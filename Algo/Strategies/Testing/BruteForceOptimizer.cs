namespace StockSharp.Algo.Strategies.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// The brute force optimizer of strategies.
	/// </summary>
	public class BruteForceOptimizer : BaseLogReceiver
	{
		private readonly HashSet<HistoryEmulationConnector> _startedConnectors = new();
		private bool _cancelEmulation;
		private int _leftIters;
		private int _lastTotalProgress;
		private DateTime _startedAt;

		private readonly SyncObject _sync = new();

		private readonly ISecurityProvider _securityProvider;
		private readonly IPortfolioProvider _portfolioProvider;
		private readonly IExchangeInfoProvider _exchangeInfoProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="BruteForceOptimizer"/>.
		/// </summary>
		/// <param name="securities">Instruments, the operation will be performed with.</param>
		/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		public BruteForceOptimizer(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
			: this(new CollectionSecurityProvider(securities), new CollectionPortfolioProvider(portfolios), storageRegistry.CheckOnNull(nameof(storageRegistry)).ExchangeInfoProvider, storageRegistry, StorageFormats.Binary, storageRegistry.DefaultDrive)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BruteForceOptimizer"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		public BruteForceOptimizer(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IStorageRegistry storageRegistry)
			: this(securityProvider, portfolioProvider, storageRegistry.CheckOnNull(nameof(storageRegistry)).ExchangeInfoProvider, storageRegistry, StorageFormats.Binary, storageRegistry.DefaultDrive)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BruteForceOptimizer"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		/// <param name="storageFormat">The format of market data. <see cref="StorageFormats.Binary"/> is used by default.</param>
		/// <param name="drive">The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.</param>
		public BruteForceOptimizer(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, IStorageRegistry storageRegistry, StorageFormats storageFormat = StorageFormats.Binary, IMarketDataDrive drive = null)
		{
			_securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
			_portfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
			_exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));

			EmulationSettings = new();

			StorageSettings = new()
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
		/// <see cref="HistoryMessageAdapter.AdapterCache"/>.
		/// </summary>
		public MarketDataStorageCache AdapterCache { get; set; }

		/// <summary>
		/// <see cref="HistoryMessageAdapter.StorageCache"/>.
		/// </summary>
		public MarketDataStorageCache StorageCache { get; set; }

		/// <summary>
		/// Has the emulator ended its operation due to end of data, or it was interrupted through the <see cref="Stop"/> method.
		/// </summary>
		public bool IsCancelled { get; private set; }

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
		public event Action<int, TimeSpan, TimeSpan> TotalProgressChanged;

		/// <summary>
		/// The event of single progress change.
		/// </summary>
		public event Action<Strategy, IStrategyParam[], int> SingleProgressChanged;

		/// <summary>
		/// Start emulation.
		/// </summary>
		/// <param name="strategies">The strategies and parameters used for optimization.</param>
		/// <param name="iterationCount">Iteration count.</param>
		public void Start(IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> strategies, int iterationCount)
		{
			if (strategies is null)
				throw new ArgumentNullException(nameof(strategies));

			if (iterationCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(iterationCount), iterationCount, LocalizedStrings.Str1219);

			_cancelEmulation = false;
			_startedAt = DateTime.UtcNow;
			_lastTotalProgress = 0;

			var maxIters = EmulationSettings.MaxIterations;
			if (maxIters > 0 && iterationCount > maxIters)
			{
				iterationCount = maxIters;
				strategies = strategies.Take(iterationCount);
			}

			State = ChannelStates.Starting;

			var batchSize = EmulationSettings.BatchSize;

			_leftIters = iterationCount;

			var enumerator = strategies.GetEnumerator();

			State = ChannelStates.Started;

			for (var i = 0; i < batchSize; i++)
			{
				TryNextRun(enumerator, AdapterCache?.Clone(), StorageCache?.Clone(), iterationCount);
			}
		}

		private void TryNextRun(IEnumerator<(Strategy, IStrategyParam[])> enumerator, MarketDataStorageCache adapterCache, MarketDataStorageCache storageCache, int iterCount)
		{
			Strategy strategy;
			IStrategyParam[] parameters;
			HistoryEmulationConnector connector;

			lock (_sync)
			{
				if (_cancelEmulation || !enumerator.MoveNext())
				{
					if (State == ChannelStates.Stopped)
						return;

					IsCancelled = _cancelEmulation;

					State = ChannelStates.Stopping;
					State = ChannelStates.Stopped;

					return;
				}

				var t = enumerator.Current;
				strategy = t.Item1;
				parameters = t.Item2;

				connector = new(_securityProvider, _portfolioProvider, _exchangeInfoProvider)
				{
					Parent = this,

					HistoryMessageAdapter =
					{
						StorageRegistry = StorageSettings.StorageRegistry,
						Drive = StorageSettings.Drive,
						StorageFormat = StorageSettings.Format,

						StartDate = EmulationSettings.StartTime,
						StopDate = EmulationSettings.StopTime,

						AdapterCache = adapterCache,
						StorageCache = storageCache,
					},
				};

				_startedConnectors.Add(connector);
			}
			
			connector.EmulationAdapter.Settings.Load(EmulationSettings.Save());

			strategy.Connector = connector;

			strategy.Reset();
			strategy.Start();

			var lastStep = 0;

			connector.ProgressChanged += step => SingleProgressChanged?.Invoke(strategy, parameters, lastStep = step);

			connector.StateChanged += () =>
			{
				if (connector.State == ChannelStates.Stopped)
				{
					if (lastStep < 100)
						SingleProgressChanged?.Invoke(strategy, parameters, 100);

					int totalProgress;
					var now = DateTime.UtcNow;
					var duration = now - _startedAt;
					var remaining = TimeSpan.Zero;
					var updateProgress = true;

					lock (_sync)
					{
						_startedConnectors.Remove(connector);

						_leftIters--;

						var doneIters = iterCount - _leftIters;

						totalProgress = (int)((doneIters * 100.0) / iterCount);

						if (_lastTotalProgress >= totalProgress)
							updateProgress = false;
						else
						{
							_lastTotalProgress = totalProgress;
							remaining = _leftIters * (duration / doneIters);
						}
					}

					if (updateProgress)
						TotalProgressChanged?.Invoke(totalProgress, duration, remaining);

					TryNextRun(enumerator, adapterCache, storageCache, iterCount);
				}
			};

			connector.Connect();
			connector.Start();
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

				foreach (var connector in _startedConnectors)
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

				foreach (var connector in _startedConnectors)
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
				if (!(State is ChannelStates.Started or ChannelStates.Suspended))
					return;

				State = ChannelStates.Stopping;

				_cancelEmulation = true;

				foreach (var connector in _startedConnectors)
				{
					if (connector.State is
						ChannelStates.Started or
						ChannelStates.Starting or
						ChannelStates.Suspended or
						ChannelStates.Suspending)
						connector.Disconnect();
				}
			}
		}

		/// <inheritdoc />
		protected override void DisposeManaged()
		{
			Stop();
			base.DisposeManaged();
		}
	}
}