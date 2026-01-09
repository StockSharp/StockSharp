namespace StockSharp.Algo.Strategies.Optimization;

using StockSharp.Algo.Testing;

/// <summary>
/// The base optimizer of strategies.
/// </summary>
public abstract class BaseOptimizer : BaseLogReceiver
{
	private class CacheAllocator(MarketDataStorageCache original)
	{
		private readonly MarketDataStorageCache _original = original ?? throw new ArgumentNullException(nameof(original));

		public MarketDataStorageCache Allocate() => _original;

		public void Free(MarketDataStorageCache cache) { }
	}

	private class CopyPortfolioProvider : IPortfolioProvider
	{
		private readonly IPortfolioProvider _provider;
		private readonly SynchronizedDictionary<string, Portfolio> _copies = new(StringComparer.InvariantCultureIgnoreCase);

		public CopyPortfolioProvider(IPortfolioProvider provider)
		{
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));

			_provider.NewPortfolio += OnNewPortfolio;
			_provider.PortfolioChanged += OnPortfolioChanged;
		}

		private void OnNewPortfolio(Portfolio portfolio)
			=> NewPortfolio?.Invoke(GetCopy(portfolio));

		private void OnPortfolioChanged(Portfolio portfolio)
			=> PortfolioChanged?.Invoke(GetCopy(portfolio));

		private Portfolio GetCopy(Portfolio portfolio)
			=> LookupByPortfolioName(portfolio.CheckOnNull(nameof(portfolio)).Name);

		public Portfolio LookupByPortfolioName(string name)
			=> _copies.SafeAdd(name, key => (Portfolio)_provider.LookupByPortfolioName(key)?.Clone() ?? new Portfolio { Name = key });

		public IEnumerable<Portfolio> Portfolios => _provider.Portfolios.Select(GetCopy);

		public event Action<Portfolio> NewPortfolio;
		public event Action<Portfolio> PortfolioChanged;
	}

	private readonly HashSet<HistoryEmulationConnector> _startedConnectors = [];

	private MarketDataStorageCache _adapterCache;
	private MarketDataStorageCache _storageCache;

	private CacheAllocator _adapterCacheAllocator;
	private CacheAllocator _storageCacheAllocator;

	private readonly Lock _sync = new();
	private ChannelStates _state = ChannelStates.Stopped;
	private bool _cancelEmulation;
	private bool _allIterationsStarted;

	private IOptimizationBatchManager _batchManager = new OptimizationBatchManager();
	private IOptimizationProgressTracker _progressTracker = new OptimizationProgressTracker();

	/// <summary>
	/// Batch manager for concurrent iteration control.
	/// </summary>
	public IOptimizationBatchManager BatchManager
	{
		get => _batchManager;
		set => _batchManager = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Progress tracker.
	/// </summary>
	public IOptimizationProgressTracker ProgressTracker
	{
		get => _progressTracker;
		set => _progressTracker = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BaseOptimizer"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	/// <param name="storageFormat">The format of market data. <see cref="StorageFormats.Binary"/> is used by default.</param>
	/// <param name="drive">The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.</param>
	protected BaseOptimizer(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, IStorageRegistry storageRegistry, StorageFormats storageFormat, IMarketDataDrive drive)
	{
		SecurityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
		PortfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
		ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));

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
	public OptimizerSettings EmulationSettings { get; }

	/// <summary>
	/// <see cref="HistoryMessageAdapter.AdapterCache"/>.
	/// </summary>
	public MarketDataStorageCache AdapterCache
	{
		get => _adapterCache;
		set
		{
			_adapterCache = value;
			_adapterCacheAllocator = value is null ? null : new(value);
		}
	}

	/// <summary>
	/// <see cref="HistoryMessageAdapter.StorageCache"/>.
	/// </summary>
	public MarketDataStorageCache StorageCache
	{
		get => _storageCache;
		set
		{
			_storageCache = value;
			_storageCacheAllocator = value is null ? null : new(value);
		}
	}

	/// <summary>
	/// Allocate <see cref="AdapterCache"/>.
	/// </summary>
	/// <returns><see cref="AdapterCache"/></returns>
	protected internal MarketDataStorageCache AllocateAdapterCache()
		=> _adapterCacheAllocator?.Allocate();

	/// <summary>
	/// Allocate <see cref="StorageCache"/>.
	/// </summary>
	/// <returns><see cref="StorageCache"/></returns>
	protected internal MarketDataStorageCache AllocateStorageCache()
		=> _storageCacheAllocator?.Allocate();

	/// <summary>
	/// Free <see cref="AdapterCache"/>.
	/// </summary>
	/// <param name="cache"><see cref="AdapterCache"/></param>
	protected internal void FreeAdapterCache(MarketDataStorageCache cache)
		=> _adapterCacheAllocator?.Free(cache);

	/// <summary>
	/// Free <see cref="StorageCache"/>.
	/// </summary>
	/// <param name="cache"><see cref="StorageCache"/></param>
	protected internal void FreeStorageCache(MarketDataStorageCache cache)
		=> _storageCacheAllocator?.Free(cache);

	/// <summary>
	/// <see cref="ISecurityProvider"/>
	/// </summary>
	public ISecurityProvider SecurityProvider { get; }

	/// <summary>
	/// <see cref="IPortfolioProvider"/>
	/// </summary>
	public IPortfolioProvider PortfolioProvider { get; }

	/// <summary>
	/// <see cref="IExchangeInfoProvider"/>
	/// </summary>
	public IExchangeInfoProvider ExchangeInfoProvider { get; }

	/// <summary>
	/// Has the emulator ended its operation due to end of data, or it was interrupted through the <see cref="Stop"/> method.
	/// </summary>
	public bool IsCancelled { get; private set; }

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
	/// <see cref="HistoryEmulationConnector.StopOnSubscriptionError"/>
	/// </summary>
	public bool StopOnSubscriptionError { get; set; }

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
	/// Strategy initialized event.
	/// </summary>
	public event Action<Strategy, IStrategyParam[]> StrategyInitialized;

	/// <summary>
	/// Init <see cref="Connector"/>. Called before <see cref="Connector.Connect"/>.
	/// </summary>
	public event Action<Connector> ConnectorInitialized;

	/// <summary>
	/// Start optimization.
	/// </summary>
	/// <param name="totalIterations">Total number of iterations.</param>
	protected void OnStart(int totalIterations)
	{
		var maxIters = EmulationSettings.MaxIterations;
		if (maxIters > 0 && totalIterations > maxIters)
			totalIterations = maxIters;

		_cancelEmulation = false;
		_allIterationsStarted = false;
		IsCancelled = false;

		ProgressTracker.Reset(totalIterations);
		BatchManager.Reset(EmulationSettings.BatchSize, totalIterations);

		State = ChannelStates.Starting;
		State = ChannelStates.Started;
	}

	/// <summary>
	/// To suspend the optimization.
	/// </summary>
	public virtual void Suspend()
	{
		using (_sync.EnterScope())
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
	/// To resume the optimization.
	/// </summary>
	public virtual void Resume()
	{
		using (_sync.EnterScope())
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
	/// To stop optimization.
	/// </summary>
	public virtual void Stop()
	{
		using (_sync.EnterScope())
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

			if (BatchManager.RunningCount == 0)
				RaiseStopped();
		}
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		Stop();
		base.DisposeManaged();
	}

	/// <summary>
	/// Set <see cref="State"/> to <see cref="ChannelStates.Stopped"/>.
	/// </summary>
	protected void RaiseStopped()
	{
		if (State == ChannelStates.Stopped)
			return;

		if (State != ChannelStates.Stopping)
			State = ChannelStates.Stopping;

		if (ProgressTracker.TotalProgress < 100 && !_cancelEmulation)
			TotalProgressChanged?.Invoke(100, ProgressTracker.Elapsed, default);

		IsCancelled = _cancelEmulation;
		State = ChannelStates.Stopped;
	}

	/// <summary>
	/// Try start next iteration.
	/// </summary>
	/// <param name="startTime">Date in history for starting the paper trading.</param>
	/// <param name="stopTime">Date in history to stop the paper trading (date is included).</param>
	/// <param name="tryGetNext">Handler to try to get next strategy object.</param>
	/// <param name="adapterCache"><see cref="HistoryMessageAdapter.AdapterCache"/></param>
	/// <param name="storageCache"><see cref="HistoryMessageAdapter.StorageCache"/></param>
	/// <param name="iterationFinished">Callback to notify the iteration was finished.</param>
	protected internal void TryNextRun(DateTime startTime, DateTime stopTime,
		Func<IPortfolioProvider, (Strategy strategy, IStrategyParam[] parameters)?> tryGetNext,
		MarketDataStorageCache adapterCache, MarketDataStorageCache storageCache,
		Action iterationFinished)
	{
		if (tryGetNext is null)
			throw new ArgumentNullException(nameof(tryGetNext));

		if (iterationFinished is null)
			throw new ArgumentNullException(nameof(iterationFinished));

		Strategy strategy;
		IStrategyParam[] parameters;
		HistoryEmulationConnector connector;
		Guid iterationId;

		using (_sync.EnterScope())
		{
			// Check if we can start a new iteration
			if (State is ChannelStates.Suspended or ChannelStates.Suspending or ChannelStates.Stopping or ChannelStates.Stopped)
				return;

			if (_cancelEmulation || _allIterationsStarted)
			{
				CheckFinished();
				return;
			}

			if (!BatchManager.CanStartNext)
			{
				_allIterationsStarted = true;
				CheckFinished();
				return;
			}

			// Try to get next strategy
			var pfProvider = new CopyPortfolioProvider(PortfolioProvider);
			var next = tryGetNext(pfProvider);

			if (next is null)
			{
				_allIterationsStarted = true;
				CheckFinished();
				return;
			}

			(strategy, parameters) = next.Value;

			// Reserve slot in batch
			if (!BatchManager.TryReserveSlot(out iterationId))
				return;

			ProgressTracker.IterationStarted();

			strategy.Parent ??= this;

			connector = CreateConnector(pfProvider, adapterCache, storageCache, startTime, stopTime);
			_startedConnectors.Add(connector);
		}

		SetupIteration(connector, strategy, parameters, iterationId, iterationFinished);
		StartIteration(connector, strategy, parameters);
	}

	private void CheckFinished()
	{
		if (BatchManager.IsFinished)
			RaiseStopped();
	}

	private HistoryEmulationConnector CreateConnector(
		IPortfolioProvider pfProvider,
		MarketDataStorageCache adapterCache,
		MarketDataStorageCache storageCache,
		DateTime startTime,
		DateTime stopTime)
	{
		var connector = new HistoryEmulationConnector(SecurityProvider, pfProvider, ExchangeInfoProvider, StorageSettings.StorageRegistry)
		{
			Parent = this,
			StopOnSubscriptionError = StopOnSubscriptionError,

			HistoryMessageAdapter =
			{
				Drive = StorageSettings.Drive,
				StorageFormat = StorageSettings.Format,

				StartDate = startTime,
				StopDate = stopTime,

				AdapterCache = adapterCache,
				StorageCache = storageCache,
			},

			MaxMessageCount = EmulationSettings.MaxMessageCount,
		};

		connector.EmulationSettings.Load(EmulationSettings.Save());

		return connector;
	}

	private void SetupIteration(
		HistoryEmulationConnector connector,
		Strategy strategy,
		IStrategyParam[] parameters,
		Guid iterationId,
		Action iterationFinished)
	{
		var lastStep = 0;

		connector.ProgressChanged += step => SingleProgressChanged?.Invoke(strategy, parameters, lastStep = step);

		connector.StateChanged2 += state =>
		{
			if (state != ChannelStates.Stopped)
				return;

			OnIterationCompleted(connector, strategy, parameters, iterationId, lastStep, iterationFinished);
		};
	}

	private void OnIterationCompleted(
		HistoryEmulationConnector connector,
		Strategy strategy,
		IStrategyParam[] parameters,
		Guid iterationId,
		int lastStep,
		Action iterationFinished)
	{
		if (lastStep < 100)
		{
			SingleProgressChanged?.Invoke(strategy, parameters, 100);
			strategy.Stop();
		}

		bool isFinished;

		using (_sync.EnterScope())
		{
			_startedConnectors.Remove(connector);
			BatchManager.CompleteIteration(iterationId);
			isFinished = _allIterationsStarted && BatchManager.IsFinished;
		}

		ProgressTracker.IterationCompleted();

		// Report total progress
		var progress = ProgressTracker.TotalProgress;
		TotalProgressChanged?.Invoke(progress, ProgressTracker.Elapsed, ProgressTracker.Remaining);

		// Trigger next iteration
		iterationFinished();

		// Check if we should stop
		if (isFinished || (_cancelEmulation && BatchManager.RunningCount == 0))
			RaiseStopped();
	}

	private void StartIteration(HistoryEmulationConnector connector, Strategy strategy, IStrategyParam[] parameters)
	{
		strategy.Connector = connector;
		strategy.WaitRulesOnStop = false;
		strategy.Reset();

		StrategyInitialized?.Invoke(strategy, parameters);
		ConnectorInitialized?.Invoke(connector);

		if (StopOnSubscriptionError)
		{
			strategy.ProcessStateChanged += s =>
			{
				if (s == strategy && s.ProcessState == ProcessStates.Started && !((ISubscriptionProvider)s).Subscriptions.Any(sub => sub.DataType.IsMarketData))
				{
					s.LogError("No any market data subscription.");
					connector.Disconnect();
				}
			};
		}

		strategy.Start();

		connector.Connect();
		connector.Start();
	}
}
