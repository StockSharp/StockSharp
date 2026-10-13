namespace StockSharp.Algo.Strategies.Optimization;

using Nito.AsyncEx;

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
		private readonly SynchronizedDictionary<string, Portfolio> _copies = new(StringComparer.OrdinalIgnoreCase);

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
	private readonly AsyncLock _nextLock = new();

	// Pause, Resume and an iteration that has just come up take their turn here, so each of them
	// finds the connectors in the state the previous one left them in.
	private readonly AsyncLock _pauseLock = new();
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
		using (await _pauseLock.LockAsync())
		{
			// CompareExchange returns the previous value; if it was already set we are already paused.
			// The gate blocks new iteration starts; the running backtests are suspended below.
			if (Interlocked.CompareExchange(ref _pauseTcs, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously), null) is not null)
				return;

			// A "soft" pause that only blocks new starts would still let the whole in-flight batch run
			// to completion - and each iteration can take seconds - so suspend the running connectors too.
			await SetConnectorsSuspendedAsync(true);
		}
	}

	/// <summary>
	/// Resume paused optimization.
	/// </summary>
	/// <returns><see cref="Task"/></returns>
	public async Task Resume()
	{
		using (await _pauseLock.LockAsync())
		{
			// Resume the suspended backtests, then release the gate that blocks new iteration starts.
			await SetConnectorsSuspendedAsync(false);
			UnblockPauseWaiters();
		}
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
					await SuspendConnectorAsync(connector);
				else if (connector.State is ChannelStates.Suspended or ChannelStates.Suspending)
					await connector.StartAsync();
			}
			catch (Exception ex)
			{
				this.AddErrorLog(ex);
			}
		}

		await Task.WhenAll(connectors.Select(ApplyAsync));
	}

	// Asks a replaying connector to suspend and waits until it has. The request travels through the
	// connector's own queue, and whoever comes next - the resume, an iteration checking the pause for
	// itself - goes by the state the connector is in.
	private static async Task SuspendConnectorAsync(HistoryEmulationConnector connector)
	{
		var suspended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		void OnStateChanged(ChannelStates state)
		{
			if (state is ChannelStates.Suspended or ChannelStates.Stopping or ChannelStates.Stopped)
				suspended.TrySetResult();
		}

		connector.StateChanged2 += OnStateChanged;

		try
		{
			if (connector.State != ChannelStates.Started)
				return;

			await connector.SuspendAsync();
			await suspended.Task;
		}
		finally
		{
			connector.StateChanged2 -= OnStateChanged;
		}
	}

	// A pause that arrived while an iteration was coming up found no replay to suspend, so the
	// iteration looks at the pause for itself once its connector is up.
	private async ValueTask SuspendIfPausedAsync(HistoryEmulationConnector connector)
	{
		using (await _pauseLock.LockAsync())
		{
			if (!IsPaused)
				return;

			try
			{
				await SuspendConnectorAsync(connector);
			}
			catch (Exception ex)
			{
				this.AddErrorLog(ex);
			}
		}
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

			// Disconnect outside the lock. A connector raises its state change on the calling thread,
			// and the handler needs this same lock to drop the finished connector, so disconnecting
			// while holding it deadlocks the cancellation against the completion it is waiting for.
			HistoryEmulationConnector[] connectors;

			using (_sync.EnterScope())
				connectors = [.. _startedConnectors];

			foreach (var connector in connectors)
				StopStartedConnector(connector);
		});
	}

	/// <summary>
	/// Disconnect a connector that is up, so its iteration reaches <see cref="ChannelStates.Stopped"/> and
	/// signals the worker waiting on it. One that is already stopping gets there on its own.
	/// </summary>
	private void StopStartedConnector(HistoryEmulationConnector connector)
	{
		try
		{
			if (connector.State is
				ChannelStates.Started or
				ChannelStates.Starting or
				ChannelStates.Suspended or
				ChannelStates.Suspending)
				connector.Disconnect();
		}
		catch (Exception ex)
		{
			this.AddErrorLog(ex);
		}
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

	/// <summary>
	/// Cancel the current optimization run and let its producers drain.
	/// </summary>
	protected void CancelRun()
		=> _linkedCts?.Cancel();

	/// <summary>
	/// Cancellation token for the current optimization run, including optimizer disposal and consumer cancellation.
	/// </summary>
	protected CancellationToken RunToken
		=> _linkedCts?.Token ?? CancellationToken.None;

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
		Func<IPortfolioProvider, CancellationToken, ValueTask<(Strategy strategy, IStrategyParam[] parameters)?>> tryGetNext,
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

		// The next strategy is awaited, and a lock cannot be held across that, so the workers take
		// their turn here and the state is guarded only while it is read and written.
		using (await _nextLock.LockAsync(cancellationToken))
		{
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
			}

			// Try to get next strategy
			var pfProvider = new CopyPortfolioProvider(PortfolioProvider);
			var next = await tryGetNext(pfProvider, cancellationToken);

			using (_sync.EnterScope())
			{
				if (next is null)
				{
					_allIterationsStarted = true;
					CheckFinished();
					return false;
				}

				if (_cancelEmulation)
				{
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
		}

		await connector.EmulationSettings.LoadAsync(await EmulationSettings.SaveAsync(cancellationToken), cancellationToken);

		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		SetupIteration(connector, strategy, parameters, iterationId, tcs, started);
		await StartIterationAsync(connector, strategy, parameters, cancellationToken);

		// The replay is started by a message, so the connector comes up some time after the call that
		// asked for it. A cancellation or a pause that looked at it before that found it not started.
		await started.Task;

		// Cancellation disconnects what is running when it fires, and it fires once. A connector that was
		// still starting then is either missed by that pass or takes the disconnect while it is coming up
		// and comes up anyway - and nothing else would ever stop it, leaving the wait below with no end.
		if (cancellationToken.IsCancellationRequested)
			StopStartedConnector(connector);
		else
			await SuspendIfPausedAsync(connector);

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

		return connector;
	}

	private void SetupIteration(
		HistoryEmulationConnector connector,
		Strategy strategy,
		IStrategyParam[] parameters,
		Guid iterationId,
		TaskCompletionSource<bool> tcs,
		TaskCompletionSource started)
	{
		var lastStep = 0;

		connector.ProgressChanged += step => SingleProgressChanged?.Invoke(strategy, parameters, lastStep = step);

		connector.StateChanged2 += state =>
		{
			// up, or over before it got there
			if (state is ChannelStates.Started or ChannelStates.Stopping or ChannelStates.Stopped)
				started.TrySetResult();

			if (state != ChannelStates.Stopped)
				return;

			OnIterationCompletedAsync(connector, strategy, parameters, iterationId, lastStep, tcs)
				.AsTask()
				.ContinueWith(task =>
				{
					if (task.IsFaulted && task.Exception is not null)
						tcs.TrySetException(task.Exception.InnerExceptions);
				}, TaskScheduler.Default);
		};
	}

	private async ValueTask OnIterationCompletedAsync(
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
			await strategy.StopAsync();
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

		await strategy.StartAsync(cancellationToken);

		connector.Connect();
		await connector.StartAsync(cancellationToken);
	}
}
