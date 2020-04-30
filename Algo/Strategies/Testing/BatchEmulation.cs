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

	/// <summary>
	/// The batch emulator of strategies.
	/// </summary>
	public class BatchEmulation : BaseLogReceiver
	{
		private readonly SynchronizedDictionary<Strategy, Tuple<Portfolio, Security>> _strategyInfo = new SynchronizedDictionary<Strategy, Tuple<Portfolio, Security>>();

		private IEnumerator<IEnumerable<Strategy>> _batches;
		private Strategy[] _batch = ArrayHelper.Empty<Strategy>();
		private IMessageChannel _currChannel;
		private readonly List<HistoryEmulationConnector> _currentConnectors = new List<HistoryEmulationConnector>();
		private IMessageAdapter _histAdapter;
		private bool _cancelEmulation;
		private int _totalBatches;
		private int _currentBatch;
		private double _batchWeight;

		private readonly ISecurityProvider _securityProvider;
		private readonly IPortfolioProvider _portfolioProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="BatchEmulation"/>.
		/// </summary>
		/// <param name="securities">Instruments, the operation will be performed with.</param>
		/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		public BatchEmulation(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
			: this(new CollectionSecurityProvider(securities), new CollectionPortfolioProvider(portfolios), storageRegistry, StorageFormats.Binary, storageRegistry.DefaultDrive)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BatchEmulation"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		/// <param name="storageFormat">The format of market data. <see cref="StorageFormats.Binary"/> is used by default.</param>
		/// <param name="drive">The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.</param>
		public BatchEmulation(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IStorageRegistry storageRegistry, StorageFormats storageFormat = StorageFormats.Binary, IMarketDataDrive drive = null)
		{
			_securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
			_portfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));

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

		private EmulationStates _state = EmulationStates.Stopped;
		
		/// <summary>
		/// The emulator state.
		/// </summary>
		public EmulationStates State
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
		/// Current tested strategies.
		/// </summary>
		public IEnumerable<Strategy> BatchStrategies => _batch;

		/// <summary>
		/// The event on change of paper trade state.
		/// </summary>
		public event Action<EmulationStates, EmulationStates> StateChanged;

		/// <summary>
		/// The event of paper trade progress change.
		/// </summary>
		public event Action<Connector, int, int> ProgressChanged;

		/// <summary>
		/// Server time changed <see cref="Connector.CurrentTime"/>. It passed the time difference since the last call of the event. The first time the event passes the value <see cref="TimeSpan.Zero"/>.
		/// </summary>
		public event Action<Connector, TimeSpan> MarketTimeChanged;

		/// <summary>
		/// Start emulation.
		/// </summary>
		/// <param name="strategies">The strategies.</param>
		/// <param name="iterationCount">Iteration count.</param>
		public void Start(IEnumerable<Strategy> strategies, int iterationCount)
		{
			if (strategies == null)
				throw new ArgumentNullException(nameof(strategies));

			_cancelEmulation = false;
			_totalBatches = (int)((decimal)iterationCount / EmulationSettings.BatchSize).Ceiling();
			_currentBatch = -1;
			_batchWeight = 100.0 / _totalBatches;

			State = EmulationStates.Starting;

			_batches = strategies.Batch(EmulationSettings.BatchSize).GetEnumerator();

			TryStartNextBatch();
		}

		private void TryStartNextBatch()
		{
			if (_cancelEmulation || !_batches.MoveNext())
			{
				IsFinished = !_cancelEmulation;

				State = EmulationStates.Stopping;
				State = EmulationStates.Stopped;

				if (_currChannel != null)
				{
					_currChannel.Dispose();
					_currChannel = null;
				}

				if (_histAdapter != null)
				{
					_histAdapter.Dispose();
					_histAdapter = null;
				}

				return;
			}

			_batch = _batches.Current.ToArray();
			_currentBatch++;

			if (_currentBatch == 0)
			{
				State = EmulationStates.Starting;
				State = EmulationStates.Started;
			}

			InitAdapters();
		}

		private void InitAdapters()
		{
			_histAdapter = new SubscriptionOnlineMessageAdapter(new HistoryMessageAdapter(new IncrementalIdGenerator(), _securityProvider)
			{
				StorageRegistry = StorageSettings.StorageRegistry,
				Drive = StorageSettings.Drive,
				StorageFormat = StorageSettings.Format,
				StartDate = EmulationSettings.StartTime,
				StopDate = EmulationSettings.StopTime,
				Parent = this,
			});

			_currChannel = new InMemoryMessageChannel(new MessageByLocalTimeQueue(), "Emulator in", _histAdapter.AddErrorLog);

			var progress = new SynchronizedDictionary<HistoryEmulationConnector, int>();
			var left = _batch.Length;

			_currentConnectors.Clear();

			foreach (var strategy in _batch)
			{
				_strategyInfo[strategy] = Tuple.Create(strategy.Portfolio, strategy.Security);

				var connector = new HistoryEmulationConnector(_histAdapter, false, _currChannel, _securityProvider, _portfolioProvider)
				{
					Parent = this,
				};
				connector.EmulationAdapter.Settings.Load(EmulationSettings.Save());
				
				strategy.Connector = connector;

				strategy.Reset();
				strategy.Start();

				connector.Connect();

				progress.Add(connector, 0);

				connector.ProgressChanged += step =>
				{
					var avgStep = 0.0;

					lock (progress.SyncRoot)
					{
						progress[connector] = step;
						avgStep = progress.Values.Average();
					}

					ProgressChanged?.Invoke(connector, step, (int)(_currentBatch * _batchWeight + ((avgStep * _batchWeight) / 100)));
				};

				connector.MarketTimeChanged += diff => MarketTimeChanged?.Invoke(connector, diff);

				connector.StateChanged += () =>
				{
					if (connector.State == EmulationStates.Stopped)
					{
						left--;

						if (left == 0)
							TryStartNextBatch();
					}
				};

				_currentConnectors.Add(connector);
			}

			_histAdapter.SendInMessage(new ConnectMessage());
			_histAdapter.SendInMessage(new EmulationStateMessage { State = EmulationStates.Starting });
		}

		/// <summary>
		/// To suspend the emulation.
		/// </summary>
		public void Suspend()
		{
			State = EmulationStates.Suspending;

			var channel = _currChannel;

			if (channel == null)
				throw new InvalidOperationException();

			channel.Suspend();

			State = EmulationStates.Suspended;
		}

		/// <summary>
		/// To resume the emulation.
		/// </summary>
		public void Resume()
		{
			State = EmulationStates.Starting;

			var channel = _currChannel;

			if (channel == null)
				throw new InvalidOperationException();

			channel.Resume();

			State = EmulationStates.Started;
		}

		/// <summary>
		/// To stop paper trading.
		/// </summary>
		public void Stop()
		{
			var channel = _currChannel;

			if (channel == null)
				throw new InvalidOperationException();

			channel.Clear();
			channel.Resume();

			State = EmulationStates.Stopping;

			_cancelEmulation = true;
			
			_histAdapter.SendInMessage(new EmulationStateMessage { State = EmulationStates.Stopping });
			_histAdapter.SendInMessage(new DisconnectMessage());

			foreach (var connector in _currentConnectors)
			{
				if (connector.ConnectionState == ConnectionStates.Connected)
					connector.Disconnect();
			}
		}
	}
}