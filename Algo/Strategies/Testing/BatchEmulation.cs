namespace StockSharp.Algo.Strategies.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// The batch emulator of strategies.
	/// </summary>
	public class BatchEmulation : BaseLogReceiver
	{
		private readonly List<HistoryEmulationConnector> _currentConnectors = new List<HistoryEmulationConnector>();
		private IMessageAdapter _histAdapter;
		private bool _cancelEmulation;

		private readonly SyncObject _sync = new SyncObject();

		private readonly ISecurityProvider _securityProvider;
		private readonly IPortfolioProvider _portfolioProvider;
		private readonly IExchangeInfoProvider _exchangeInfoProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="BatchEmulation"/>.
		/// </summary>
		/// <param name="securities">Instruments, the operation will be performed with.</param>
		/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		public BatchEmulation(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
			: this(new CollectionSecurityProvider(securities), new CollectionPortfolioProvider(portfolios), new InMemoryExchangeInfoProvider(), storageRegistry, StorageFormats.Binary, storageRegistry.DefaultDrive)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BatchEmulation"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		/// <param name="storageFormat">The format of market data. <see cref="StorageFormats.Binary"/> is used by default.</param>
		/// <param name="drive">The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.</param>
		public BatchEmulation(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, IStorageRegistry storageRegistry, StorageFormats storageFormat = StorageFormats.Binary, IMarketDataDrive drive = null)
		{
			_securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
			_portfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
			_exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));

			EmulationSettings = new EmulationSettings();

			StorageSettings = new StorageCoreSettings
			{
				StorageRegistry = storageRegistry,
				Drive = drive,
				Format = storageFormat,
			};
		}

		/// <summary>
		/// Storage settings.
		/// </summary>
		public StorageCoreSettings StorageSettings { get; }

		/// <summary>
		/// Emulation settings.
		/// </summary>
		public EmulationSettings EmulationSettings { get; }

		/// <summary>
		/// Has the emulator ended its operation due to end of data, or it was interrupted through the <see cref="BatchEmulation.Stop"/>method.
		/// </summary>
		public bool IsFinished { get; private set; }

		private ChannelStates _state = ChannelStates.Stopped;
		
		/// <summary>
		/// The emulator state.
		/// </summary>
		public ChannelStates State
		{
			get => _state;
			private set
			{
				if (_state == value)
					return;

				var oldState = _state;
				_state = value;
				StateChanged?.Invoke(oldState, _state);
			}
		}

		/// <summary>
		/// The event on change of paper trade state.
		/// </summary>
		public event Action<ChannelStates, ChannelStates> StateChanged;

		/// <summary>
		/// The event of total progress change.
		/// </summary>
		public event Action<IEnumerable<Strategy>, int> TotalProgressChanged;

		/// <summary>
		/// The event of single progress change.
		/// </summary>
		public event Action<Strategy, int> SingleProgressChanged;

		/// <summary>
		/// Server time changed <see cref="ILogSource.CurrentTime"/>. It passed the time difference since the last call of the event. The first time the event passes the value <see cref="TimeSpan.Zero"/>.
		/// </summary>
		public event Action<Connector, TimeSpan> MarketTimeChanged;

		/// <summary>
		/// Start emulation.
		/// </summary>
		/// <param name="strategies">The strategies.</param>
		/// <param name="iterationCount">Iteration count.</param>
		public void Start(IEnumerable<Strategy> strategies, int iterationCount)
		{
			if (strategies is null)
				throw new ArgumentNullException(nameof(strategies));

			if (iterationCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(iterationCount), iterationCount, LocalizedStrings.Str1219);

			_cancelEmulation = false;
			
			State = ChannelStates.Starting;

			var totalBatches = (int)((decimal)iterationCount / EmulationSettings.BatchSize).Ceiling();

			if (totalBatches == 0)
				throw new ArgumentOutOfRangeException("totalBatches == 0");

			var batchWeight = 100.0 / totalBatches;

			TryStartNextBatch(strategies.Batch(EmulationSettings.BatchSize).GetEnumerator(), -1, totalBatches, batchWeight);
		}

		private void TryStartNextBatch(IEnumerator<IEnumerable<Strategy>> batches,
			int currentBatch, int totalBatches, double batchWeight)
		{
			if (batches is null)
				throw new ArgumentNullException(nameof(batches));

			lock (_sync)
			{
				if (_cancelEmulation || !batches.MoveNext())
				{
					IsFinished = !_cancelEmulation;

					State = ChannelStates.Stopping;
					State = ChannelStates.Stopped;

					if (_histAdapter != null)
					{
						_histAdapter.Dispose();
						_histAdapter = null;
					}

					return;
				}

				var batch = batches.Current.ToArray();
				currentBatch++;

				if (currentBatch == 0)
				{
					State = ChannelStates.Starting;
					State = ChannelStates.Started;
				}

				InitAdapters(batches, batch, currentBatch, totalBatches, batchWeight);
			}
		}

		private void InitAdapters(IEnumerator<IEnumerable<Strategy>> batches, Strategy[] batch,
			int currentBatch, int totalBatches, double batchWeight)
		{
			if (batch is null)
				throw new ArgumentNullException(nameof(batch));

			if (batch.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(batch));

			_histAdapter = new SubscriptionOnlineMessageAdapter(new HistoryMessageAdapter(new IncrementalIdGenerator(), _securityProvider)
			{
				StorageRegistry = StorageSettings.StorageRegistry,
				Drive = StorageSettings.Drive,
				StorageFormat = StorageSettings.Format,
				StartDate = EmulationSettings.StartTime,
				StopDate = EmulationSettings.StopTime,
				Parent = this,
			});

			var progress = new SynchronizedDictionary<HistoryEmulationConnector, int>();
			var left = batch.Length;

			var nextProgress = 1;

			_currentConnectors.Clear();

			foreach (var strategy in batch)
			{
				var inChannel = new InMemoryMessageChannel(new MessageByLocalTimeQueue(), "Emulator in", _histAdapter.AddErrorLog) { SuspendMaxCount = int.MaxValue };

				var connector = new HistoryEmulationConnector(_histAdapter, false, inChannel, _securityProvider, _portfolioProvider, _exchangeInfoProvider)
				{
					Parent = this,
				};
				connector.EmulationAdapter.Settings.Load(EmulationSettings.Save());
				
				strategy.Connector = connector;

				strategy.Reset();
				strategy.Start();

				progress.Add(connector, 0);

				connector.ProgressChanged += step =>
				{
					SingleProgressChanged?.Invoke(strategy, step);

					var avgStep = 0;

					lock (progress.SyncRoot)
					{
						progress[connector] = step;
						avgStep = (int)progress.Values.Average();
					}

					if (avgStep < nextProgress)
						return;

					nextProgress++;

					TotalProgressChanged?.Invoke(batch, (int)(currentBatch * batchWeight + ((avgStep * batchWeight) / 100)));
				};

				connector.MarketTimeChanged += diff => MarketTimeChanged?.Invoke(connector, diff);

				connector.StateChanged += () =>
				{
					if (connector.State == ChannelStates.Stopped)
					{
						left--;

						if (left == 0)
							TryStartNextBatch(batches, currentBatch, totalBatches, batchWeight);
					}
				};

				_currentConnectors.Add(connector);
			}

			foreach (var connectors in _currentConnectors)
				connectors.Connect();

			_histAdapter.SendInMessage(new ConnectMessage());
			_histAdapter.SendInMessage(new EmulationStateMessage { State = ChannelStates.Starting });
		}

		/// <summary>
		/// To suspend the emulation.
		/// </summary>
		public void Suspend()
		{
			lock (_sync)
			{
				if (State != ChannelStates.Started)
					return;

				State = ChannelStates.Suspending;

				foreach (var connector in _currentConnectors)
				{
					if (connector.State == ChannelStates.Started)
						connector.Suspend();
				}

				State = ChannelStates.Suspended;
			}
		}

		/// <summary>
		/// To resume the emulation.
		/// </summary>
		public void Resume()
		{
			lock (_sync)
			{
				if (State != ChannelStates.Suspended)
					return;

				State = ChannelStates.Starting;

				foreach (var connector in _currentConnectors)
				{
					if (connector.State == ChannelStates.Suspended)
						connector.Start();
				}

				State = ChannelStates.Started;
			}
		}

		/// <summary>
		/// To stop paper trading.
		/// </summary>
		public void Stop()
		{
			lock (_sync)
			{
				if (!(State == ChannelStates.Started || State == ChannelStates.Suspended))
					return;

				State = ChannelStates.Stopping;

				_cancelEmulation = true;

				foreach (var connector in _currentConnectors)
				{
					if (connector.State == ChannelStates.Started ||
						connector.State == ChannelStates.Starting ||
						connector.State == ChannelStates.Suspended ||
						connector.State == ChannelStates.Suspending)
						connector.Disconnect();
				}
			
				_histAdapter.SendInMessage(new EmulationStateMessage { State = ChannelStates.Stopping });
				_histAdapter.SendInMessage(new DisconnectMessage());
			}
		}
	}
}