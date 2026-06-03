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
	private bool _cancelEmulation;
	private bool _allIterationsStarted;

	private volatile TaskCompletionSource _pauseTcs;

	private readonly OptimizationBatchManager _batchManager = new();

	private Channel<(Strategy strategy, IStrategyParam[] parameters)> _resultsChannel;
	private CancellationTokenSource _linkedCts;

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
	/// <see cref="HistoryEmulationConnector.StopOnSubscriptionError"/>
	/// </summary>
	public bool StopOnSubscriptionError { get; set; }

	/// <summary>
	/// Whether optimization is currently paused.
	/// </summary>
	public bool IsPaused => _pauseTcs is not null;

	/// <summary>
	/// Pause optimization. New iterations won't start until <see cref="Resume"/> is called, and the
	/// backtests that are already running are suspended so progress halts promptly.
	/// </summary>
	/// <returns><see cref="Task"/></returns>
	public async Task Pause()
	{
		// CompareExchange returns the previous value; if it was already set we are already paused.
		// Setting the gate is synchronous, so new iteration starts are blocked immediately; the running
		// backtests are then suspended below.
		if (Interlocked.CompareExchange(ref _pauseTcs, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously), null) is not null)
			return;

		// A "soft" pause that only blocks new starts would still let the whole in-flight batch run
		// to completion - and each iteration can take seconds - so suspend the running connectors too.
		await SetConnectorsSuspendedAsync(true);
	}

	/// <summary>
	/// Resume paused optimization.
	/// </summary>
	/// <returns><see cref="Task"/></returns>
	public async Task Resume()
	{
		// Resume the suspended backtests, then release the gate that blocks new iteration starts.
		await SetConnectorsSuspendedAsync(false);
		UnblockPauseWaiters();
	}

	// Releases the gate that parks new iteration starts in TryNextRunAsync without touching the running
	// connectors. Used by the teardown paths (cancellation/dispose), which run synchronously and only
	// need the paused waiters to wake up so they can observe cancellation.
	private void UnblockPauseWaiters()
		=> Interlocked.Exchange(ref _pauseTcs, null)?.TrySetResult();

	private async Task SetConnectorsSuspendedAsync(bool suspend)
	{
		HistoryEmulationConnector[] connectors;

		using (_sync.EnterScope())
			connectors = [.. _startedConnectors];

		if (connectors.Length == 0)
			return;

		// Suspend/resume the running backtests by awaiting each connector's own SuspendAsync/StartAsync -
		// the same mechanism a single backtest uses. It is driven through the async call chain (the UI
		// button handler is async too) instead of a fire-and-forget Task.Run: that previous approach put
		// the suspend on the thread pool which - already saturated by the BatchSize (CPU*2) in-flight
		// backtests - queued it behind them, so the whole batch ran to completion before the pause took
		// effect. All connectors are handled concurrently so the batch halts at once.
		async Task ApplyAsync(HistoryEmulationConnector connector)
		{
			try
			{
				if (suspend)
				{
					if (connector.State == ChannelStates.Started)
						await connector.SuspendAsync();
				}
				else
				{
					if (connector.State is ChannelStates.Suspended or ChannelStates.Suspending)
						await connector.StartAsync();
				}
			}
			catch (Exception ex)
			{
				this.AddErrorLog(ex);
			}
		}

		await Task.WhenAll(connectors.Select(ApplyAsync));
	}

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
	/// Initialize channel, batch manager, and linked CTS for RunAsync.
	/// </summary>
	/// <param name="totalIterations">Total number of iterations (or int.MaxValue if unknown).</param>
	/// <param name="cancellationToken">External cancellation token.</param>
	protected void InitializeRunAsync(int totalIterations, CancellationToken cancellationToken)
	{
		_cancelEmulation = false;
		_allIterationsStarted = false;

		// Reset pause state (no running connectors yet at init, so just clear the start gate).
		UnblockPauseWaiters();

		_batchManager.Reset(EmulationSettings.BatchSize, totalIterations);

		_resultsChannel = Channel.CreateUnbounded<(Strategy, IStrategyParam[])>(new UnboundedChannelOptions
		{
			SingleReader = true,
		});

		_linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		_linkedCts.Token.Register(() =>
		{
			_cancelEmulation = true;

			// Unblock paused waiters so they can see cancellation (the connectors are disconnected just
			// below, so there is no need to resume their replay).
			UnblockPauseWaiters();

			using (_sync.EnterScope())
			{
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
		});
	}

	/// <summary>
	/// Yield results from channel reader.
	/// </summary>
	protected async IAsyncEnumerable<(Strategy Strategy, IStrategyParam[] Parameters)> ReadResultsAsync(
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await foreach (var result in _resultsChannel.Reader.ReadAllAsync(cancellationToken))
		{
			yield return result;
		}
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		UnblockPauseWaiters();
		_linkedCts?.Cancel();
		base.DisposeManaged();
	}

	/// <summary>
	/// Complete the channel so RunAsync enumeration ends.
	/// </summary>
	protected void CompleteChannel()
	{
		_resultsChannel?.Writer.TryComplete();
	}

	/// <summary>
	/// Try start next iteration. Returns <see langword="true"/> if iteration was started and completed,
	/// <see langword="false"/> if no more iterations available.
	/// </summary>
	/// <param name="startTime">Date in history for starting the paper trading.</param>
	/// <param name="stopTime">Date in history to stop the paper trading (date is included).</param>
	/// <param name="tryGetNext">Handler to try to get next strategy object.</param>
	/// <param name="adapterCache"><see cref="HistoryMessageAdapter.AdapterCache"/></param>
	/// <param name="storageCache"><see cref="HistoryMessageAdapter.StorageCache"/></param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected internal async ValueTask<bool> TryNextRunAsync(DateTime startTime, DateTime stopTime,
		Func<IPortfolioProvider, (Strategy strategy, IStrategyParam[] parameters)?> tryGetNext,
		MarketDataStorageCache adapterCache, MarketDataStorageCache storageCache,
		CancellationToken cancellationToken = default)
	{
		if (tryGetNext is null)
			throw new ArgumentNullException(nameof(tryGetNext));

		// Wait if paused
		var pauseTcs = _pauseTcs;
		if (pauseTcs is not null)
			await pauseTcs.Task.WaitAsync(cancellationToken);

		Strategy strategy;
		IStrategyParam[] parameters;
		HistoryEmulationConnector connector;
		Guid iterationId;

		using (_sync.EnterScope())
		{
			if (_cancelEmulation || _allIterationsStarted)
			{
				CheckFinished();
				return false;
			}

			if (!_batchManager.CanStartNext)
			{
				_allIterationsStarted = true;
				CheckFinished();
				return false;
			}

			// Try to get next strategy
			var pfProvider = new CopyPortfolioProvider(PortfolioProvider);
			var next = tryGetNext(pfProvider);

			if (next is null)
			{
				_allIterationsStarted = true;
				CheckFinished();
				return false;
			}

			(strategy, parameters) = next.Value;

			// Reserve slot in batch
			if (!_batchManager.TryReserveSlot(out iterationId))
				return false;

			strategy.Parent ??= this;

			connector = CreateConnector(pfProvider, adapterCache, storageCache, startTime, stopTime);
			_startedConnectors.Add(connector);
		}

		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		SetupIteration(connector, strategy, parameters, iterationId, tcs);
		await StartIterationAsync(connector, strategy, parameters, cancellationToken);
		return await tcs.Task;
	}

	private void CheckFinished()
	{
		if (_batchManager.IsFinished)
			CompleteChannel();
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
		TaskCompletionSource<bool> tcs)
	{
		var lastStep = 0;

		connector.ProgressChanged += step => SingleProgressChanged?.Invoke(strategy, parameters, lastStep = step);

		connector.StateChanged2 += state =>
		{
			if (state != ChannelStates.Stopped)
				return;

			OnIterationCompleted(connector, strategy, parameters, iterationId, lastStep, tcs);
		};
	}

	private void OnIterationCompleted(
		HistoryEmulationConnector connector,
		Strategy strategy,
		IStrategyParam[] parameters,
		Guid iterationId,
		int lastStep,
		TaskCompletionSource<bool> tcs)
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
			_batchManager.CompleteIteration(iterationId);
			isFinished = _allIterationsStarted && _batchManager.IsFinished;
		}

		// Write result to channel (for RunAsync consumers)
		_resultsChannel?.Writer.TryWrite((strategy, parameters));

		// Signal iteration completed
		tcs.TrySetResult(true);

		// Check if we should complete
		if (isFinished || (_cancelEmulation && _batchManager.RunningCount == 0))
			CompleteChannel();
	}

	private async ValueTask StartIterationAsync(HistoryEmulationConnector connector, Strategy strategy, IStrategyParam[] parameters, CancellationToken cancellationToken)
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
		await connector.StartAsync(cancellationToken);
	}
}
