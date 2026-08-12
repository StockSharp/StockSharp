namespace StockSharp.Algo.Testing;

using System.Runtime.CompilerServices;

using Nito.AsyncEx;

using StockSharp.Algo.Testing.Generation;

/// <summary>
/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
/// </summary>
public class HistoryMessageAdapter : MessageAdapter, IEmulationMessageAdapter
{
	private sealed class ReplayRun(CancellationTokenSource cancellationSource, CancellationToken cancellationToken)
	{
		public CancellationTokenSource CancellationSource { get; } = cancellationSource;
		public CancellationToken CancellationToken { get; } = cancellationToken;

		public Task<EmulationStateMessage> ReplayTask { get; set; } = Task.FromResult<EmulationStateMessage>(null);
		public Task LifecycleTask { get; set; } = Task.CompletedTask;

		public EmulationStateMessage ExplicitStopping { get; set; }
		public EmulationStateMessage TerminalMessage { get; set; }
		public DisconnectMessage DisconnectMessage { get; set; }
		public ResetMessage ResetMessage { get; set; }

		public TaskCompletionSource<bool> StartPublicationSource { get; set; }
		public Task StartPublicationTask { get; set; } = Task.CompletedTask;

		public bool IsStopping { get; set; }
		public bool TerminalDecided { get; set; }
		public bool TerminalPublicationCompleted { get; set; }
		public bool DisconnectDecided { get; set; }
		public bool DisconnectPublicationCompleted { get; set; }
		public bool ResetDecided { get; set; }
		public bool ResetPublicationCompleted { get; set; }
	}

	private enum StartAction
	{
		New,
		Resume,
		Ignore,
	}

	private enum TerminalAction
	{
		SendNow,
		Lifecycle,
		Ignore,
	}

	private readonly IHistoryMarketDataManager _marketDataManager;
	private readonly Lock _processingSync = new();

	private ReplayRun _processingRun;
	private bool _isDisposing;
	private bool _stopRequestInProgress;
	private ResetMessage _activeReset;

	private readonly AsyncManualResetEvent _processingGate = new(true);

	/// <summary>
	/// The provider of information about instruments.
	/// </summary>
	public ISecurityProvider SecurityProvider { get; }

	/// <summary>
	/// The number of loaded events.
	/// </summary>
	public int LoadedMessageCount => _marketDataManager.LoadedMessageCount;

	/// <summary>
	/// The number of the event <see cref="ITimeProvider.CurrentTimeChanged"/> calls after end of trading.
	/// </summary>
	public int PostTradeMarketTimeChangedCount
	{
		get => _marketDataManager.PostTradeMarketTimeChangedCount;
		set => _marketDataManager.PostTradeMarketTimeChangedCount = value;
	}

	/// <summary>
	/// Market data storage.
	/// </summary>
	public IStorageRegistry StorageRegistry
	{
		get => _marketDataManager.StorageRegistry;
		set => _marketDataManager.StorageRegistry = value;
	}

	/// <summary>
	/// The storage which is used by default.
	/// </summary>
	public IMarketDataDrive Drive
	{
		get => _marketDataManager.Drive;
		set => _marketDataManager.Drive = value;
	}

	/// <summary>
	/// The format of market data.
	/// </summary>
	public StorageFormats StorageFormat
	{
		get => _marketDataManager.StorageFormat;
		set => _marketDataManager.StorageFormat = value;
	}

	/// <summary>
	/// The interval of message <see cref="TimeMessage"/> generation.
	/// </summary>
	public TimeSpan MarketTimeChangedInterval
	{
		get => _marketDataManager.MarketTimeChangedInterval;
		set => _marketDataManager.MarketTimeChangedInterval = value;
	}

	/// <summary>
	/// Date in history for starting the paper trading.
	/// </summary>
	public DateTime StartDate
	{
		get => _marketDataManager.StartDate;
		set => _marketDataManager.StartDate = value;
	}

	/// <summary>
	/// Date in history to stop the paper trading (date is included).
	/// </summary>
	public DateTime StopDate
	{
		get => _marketDataManager.StopDate;
		set => _marketDataManager.StopDate = value;
	}

	/// <summary>
	/// Check loading dates are they tradable.
	/// </summary>
	public bool CheckTradableDates
	{
		get => _marketDataManager.CheckTradableDates;
		set => _marketDataManager.CheckTradableDates = value;
	}

	/// <summary>
	/// <see cref="BasketMarketDataStorage{T}.Cache"/>.
	/// </summary>
	public MarketDataStorageCache StorageCache
	{
		get => _marketDataManager.StorageCache;
		set => _marketDataManager.StorageCache = value;
	}

	/// <summary>
	/// <see cref="MarketDataStorageCache"/>.
	/// </summary>
	public MarketDataStorageCache AdapterCache
	{
		get => _marketDataManager.AdapterCache;
		set => _marketDataManager.AdapterCache = value;
	}

	/// <summary>
	/// Order book builders.
	/// </summary>
	public IDictionary<SecurityId, IOrderLogMarketDepthBuilder> OrderLogMarketDepthBuilders { get; } = new Dictionary<SecurityId, IOrderLogMarketDepthBuilder>();

	/// <inheritdoc />
	public override IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		=> OrderLogMarketDepthBuilders[securityId];

	/// <inheritdoc />
	public override DateTime CurrentTime => _marketDataManager.CurrentTime;

	/// <inheritdoc />
	public override bool UseOutChannel => false;

	/// <inheritdoc />
	public override bool IsFullCandlesOnly => false;

	/// <inheritdoc />
	public override bool IsSupportCandlesUpdates(MarketDataMessage subscription) => true;

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || dataType == DataType.Transactions || dataType == DataType.PositionChanges || base.IsAllDownloadingSupported(dataType);

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="marketDataManager">Market data manager.</param>
	public HistoryMessageAdapter(IdGenerator transactionIdGenerator, ISecurityProvider securityProvider, IHistoryMarketDataManager marketDataManager)
		: base(transactionIdGenerator)
	{
		SecurityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
		_marketDataManager = marketDataManager ?? throw new ArgumentNullException(nameof(marketDataManager));

		this.AddMarketDataSupport();
		this.AddSupportedMessage(MessageTypes.EmulationState, null);
		this.AddSupportedMessage(HistoryMessageTypes.Generator, true);
	}

	private readonly Dictionary<SecurityId, DataType[]> _supportedMarketDataTypes = [];

	/// <inheritdoc />
	public override IAsyncEnumerable<DataType> GetSupportedMarketDataTypesAsync(SecurityId securityId, DateTime? from, DateTime? to)
	{
		return Impl();

		async IAsyncEnumerable<DataType> Impl([EnumeratorCancellation]CancellationToken cancellationToken = default)
		{
			if (!_supportedMarketDataTypes.TryGetValue(securityId, out var dataTypes))
			{
				dataTypes = await _marketDataManager.GetSupportedDataTypesAsync(securityId).ToArrayAsync(cancellationToken);
				_supportedMarketDataTypes.Add(securityId, dataTypes);
			}

			foreach (var dataType in dataTypes)
				yield return dataType;
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				var reset = new ResetMessage();
				var (run, action) = RequestReset(reset);

				if (action == TerminalAction.SendNow)
					await SendResetAsync(run, reset, cancellationToken);

				break;
			}

			case MessageTypes.Connect:
			{
				if (IsProcessing())
					throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

				await SendOutMessageAsync(new ConnectMessage { LocalTime = StartDate }, cancellationToken);
				break;
			}

			case MessageTypes.Disconnect:
			{
				var disconnect = new DisconnectMessage { LocalTime = StopDate };
				var (run, action) = RequestDisconnect(disconnect);

				switch (action)
				{
					case TerminalAction.SendNow:
						await SendDisconnectAsync(run, disconnect, cancellationToken);
						break;
				}

				break;
			}

			case MessageTypes.SecurityLookup:
			{
				var lookupMsg = (SecurityLookupMessage)message;

				var securities = lookupMsg.SecurityId == default
					? SecurityProvider.LookupAll()
					: SecurityProvider.Lookup(lookupMsg);

				var processedBoards = new HashSet<ExchangeBoard>();

				foreach (var security in securities)
				{
					if (security.Board != null && processedBoards.Add(security.Board))
						await SendOutMessageAsync(security.Board.ToMessage(), cancellationToken);

					await SendOutMessageAsync(security.ToMessage(originalTransactionId: lookupMsg.TransactionId), cancellationToken);
				}

				await SendSubscriptionResultAsync(lookupMsg, cancellationToken);
				break;
			}

			case MessageTypes.MarketData:
				await ProcessMarketDataMessageAsync((MarketDataMessage)message, cancellationToken);
				break;

			case MessageTypes.EmulationState:
			{
				var stateMsg = (EmulationStateMessage)message;

				switch (stateMsg.State)
				{
					case ChannelStates.Starting:
					{
						var (run, action, publication) = PrepareStart(
								stateMsg.StartDate == default ? StartDate : stateMsg.StartDate,
								stateMsg.StopDate == default ? StopDate : stateMsg.StopDate,
								cancellationToken);

						if (action == StartAction.Ignore)
							return;

						var published = false;

						try
						{
							await SendOutMessageAsync(stateMsg, cancellationToken);
							published = true;
						}
						finally
						{
							CompleteStart(run, action, publication, published);
						}

						return;
					}

					case ChannelStates.Suspending:
					{
						// Pause the replay loop so no further historical data is fed out while suspended.
						_processingGate.Reset();
						break;
					}

					case ChannelStates.Stopping:
					{
						var (run, action) = RequestStopping(stateMsg);

						if (action == TerminalAction.SendNow)
							await SendTerminalAsync(run, stateMsg, cancellationToken);

						return;
					}
				}

				await SendOutMessageAsync(stateMsg, cancellationToken);
				break;
			}

			case HistoryMessageTypes.Generator:
			{
				var generatorMsg = (GeneratorMessage)message;

				if (generatorMsg.IsSubscribe)
				{
					_marketDataManager.RegisterGenerator(
						generatorMsg.SecurityId,
						generatorMsg.DataType2,
						generatorMsg.Generator,
						generatorMsg.TransactionId);
				}
				else
				{
					_marketDataManager.UnregisterGenerator(generatorMsg.OriginalTransactionId);
				}

				// Registering/unregistering a generator changes a security's supported data types, so drop the
				// cached query results — otherwise GetSupportedMarketDataTypesAsync keeps returning the stale set.
				_supportedMarketDataTypes.Clear();

				break;
			}
		}
	}

	private async ValueTask ProcessMarketDataMessageAsync(MarketDataMessage message, CancellationToken cancellationToken)
	{
		var isSubscribe = message.IsSubscribe;
		var transId = message.TransactionId;
		var originId = message.OriginalTransactionId;

		Exception error = null;

		if (isSubscribe)
		{
			error = await _marketDataManager.SubscribeAsync(message, cancellationToken);
		}
		else
		{
			_marketDataManager.Unsubscribe(originId);
		}

		await SendSubscriptionReplyAsync(transId, cancellationToken, error);

		if (isSubscribe && error == null)
			await SendSubscriptionResultAsync(message, cancellationToken);
	}

	private BoardMessage[] GetBoards()
		=> [.. SecurityProvider
			.LookupAll()
			.Select(s => s.Board)
			.Distinct()
			.Select(b => b.ToMessage())];

	private bool IsProcessing()
	{
		using (_processingSync.EnterScope())
		{
			if (_stopRequestInProgress || _activeReset is not null)
				return true;

			if (_processingRun is not { } run)
				return false;

			return !run.LifecycleTask.IsCompleted ||
				(run.TerminalMessage is not null && !run.TerminalPublicationCompleted) ||
				(run.DisconnectMessage is not null && !run.DisconnectPublicationCompleted) ||
				(run.ResetMessage is not null && !run.ResetPublicationCompleted);
		}
	}

	private (ReplayRun run, StartAction action, TaskCompletionSource<bool> publication) PrepareStart(
		DateTime startDate, DateTime stopDate, CancellationToken cancellationToken)
	{
		using (_processingSync.EnterScope())
		{
			if (_isDisposing || _stopRequestInProgress || _activeReset is not null)
				return (null, StartAction.Ignore, null);

			if (_processingRun is { } currentRun)
			{
				var publicationPending =
					(currentRun.TerminalMessage is not null && !currentRun.TerminalPublicationCompleted) ||
					(currentRun.DisconnectMessage is not null && !currentRun.DisconnectPublicationCompleted) ||
					(currentRun.ResetMessage is not null && !currentRun.ResetPublicationCompleted);

				if (!currentRun.LifecycleTask.IsCompleted || publicationPending)
				{
					if (currentRun.IsStopping || currentRun.ReplayTask.IsCompleted || publicationPending ||
						!currentRun.StartPublicationTask.IsCompleted)
						return (currentRun, StartAction.Ignore, null);

					var resumePublication = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
					currentRun.StartPublicationSource = resumePublication;
					currentRun.StartPublicationTask = resumePublication.Task;
					return (currentRun, StartAction.Resume, resumePublication);
				}
			}

			_marketDataManager.StartDate = startDate;
			_marketDataManager.StopDate = stopDate;

			var (cts, token) = cancellationToken.CreateChildToken();
			var run = new ReplayRun(cts, token);
			var boards = CheckTradableDates ? GetBoards() : [];
			var startPublication = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			run.StartPublicationSource = startPublication;
			run.StartPublicationTask = startPublication.Task;

			_processingRun = run;
			run.ReplayTask = Task.Run(
				() => RunReplayAsync(run, boards, stopDate, startPublication.Task),
				CancellationToken.None);
			run.LifecycleTask = Task.Run(() => CompleteLifecycleAsync(run), CancellationToken.None);

			return (run, StartAction.New, startPublication);
		}
	}

	private void CompleteStart(
		ReplayRun run,
		StartAction action,
		TaskCompletionSource<bool> publication,
		bool published)
	{
		var shouldStop = false;
		var shouldResume = false;
		CancellationTokenSource cancellationSource = null;

		using (_processingSync.EnterScope())
		{
			if (run is null || publication is null ||
				!ReferenceEquals(run.StartPublicationSource, publication))
			{
				publication?.TrySetResult(false);
				return;
			}

			var canRun = published && !_isDisposing && !run.IsStopping;

			if (!published && !_isDisposing && !run.IsStopping)
			{
				run.IsStopping = true;
				cancellationSource = run.CancellationSource;

				if (!_stopRequestInProgress)
				{
					_stopRequestInProgress = true;
					shouldStop = true;
				}
			}

			shouldResume = canRun && action == StartAction.Resume;
			publication.TrySetResult(canRun);
		}

		if (shouldResume)
			_processingGate.Set();

		if (shouldStop)
			StopManager(cancellationSource);
	}

	private async Task<EmulationStateMessage> RunReplayAsync(
		ReplayRun run,
		BoardMessage[] boards,
		DateTime stopDate,
		Task<bool> startPublication)
	{
		var token = run.CancellationToken;

		try
		{
			if (!await startPublication.WaitAsync(token).ConfigureAwait(false))
				return null;

			token.ThrowIfCancellationRequested();

			EmulationStateMessage terminalState = null;

			await foreach (var message in _marketDataManager
				.StartAsync(boards)
				.WithCancellation(token)
				.ConfigureAwait(false))
			{
				await _processingGate.WaitAsync(token).ConfigureAwait(false);

				if (message is EmulationStateMessage { State: ChannelStates.Stopping } stopping)
				{
					terminalState = stopping;
					break;
				}

				await SendOutMessageAsync(message, token).ConfigureAwait(false);
			}

			return terminalState;
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
			return null;
		}
		catch (Exception ex)
		{
			return new EmulationStateMessage
			{
				LocalTime = stopDate,
				State = ChannelStates.Stopping,
				Error = ex,
			};
		}
	}

	private (ReplayRun run, TerminalAction action) RequestStopping(EmulationStateMessage terminalMessage)
	{
		ReplayRun run;
		TerminalAction action;
		var shouldStop = false;
		CancellationTokenSource cancellationSource = null;

		using (_processingSync.EnterScope())
		{
			run = _processingRun;

			if (_isDisposing)
				return (run, TerminalAction.Ignore);

			if (run is null)
			{
				action = TerminalAction.SendNow;
			}
			else if (run.ExplicitStopping is not null || run.TerminalMessage is not null ||
				run.ResetMessage is not null ||
				(run.DisconnectMessage is not null && (run.TerminalDecided || run.DisconnectDecided)))
			{
				action = TerminalAction.Ignore;
			}
			else if (run.LifecycleTask.IsCompleted)
			{
				run.ExplicitStopping = terminalMessage;
				run.TerminalMessage = terminalMessage;
				run.IsStopping = true;
				action = TerminalAction.SendNow;
			}
			else
			{
				run.IsStopping = true;
				run.ExplicitStopping = terminalMessage;
				cancellationSource = run.CancellationSource;

				if (run.TerminalDecided)
				{
					// Replay callbacks are complete, so a late explicit stop can publish here.
					run.TerminalMessage = terminalMessage;
					action = TerminalAction.SendNow;
				}
				else
				{
					action = TerminalAction.Lifecycle;
				}
			}

			if (!_stopRequestInProgress)
			{
				_stopRequestInProgress = true;
				shouldStop = true;
			}
		}

		if (shouldStop)
			StopManager(cancellationSource);

		return (run, action);
	}

	private (ReplayRun run, TerminalAction action) RequestDisconnect(DisconnectMessage disconnectMessage)
	{
		ReplayRun run;
		TerminalAction action;
		var shouldStop = false;
		CancellationTokenSource cancellationSource = null;

		using (_processingSync.EnterScope())
		{
			run = _processingRun;

			if (_isDisposing || run?.DisconnectMessage is not null)
				return (run, TerminalAction.Ignore);

			if (run is null)
			{
				action = TerminalAction.SendNow;
			}
			else
			{
				run.DisconnectMessage = disconnectMessage;
				run.IsStopping = true;
				action = run.LifecycleTask.IsCompleted || run.DisconnectDecided
					? TerminalAction.SendNow
					: TerminalAction.Lifecycle;

				if (!run.LifecycleTask.IsCompleted)
					cancellationSource = run.CancellationSource;
			}

			if (!_stopRequestInProgress)
			{
				_stopRequestInProgress = true;
				shouldStop = true;
			}
		}

		if (shouldStop)
			StopManager(cancellationSource);

		return (run, action);
	}

	private (ReplayRun run, TerminalAction action) RequestReset(ResetMessage resetMessage)
	{
		ReplayRun run;
		TerminalAction action;
		var shouldStop = false;
		CancellationTokenSource cancellationSource = null;

		using (_processingSync.EnterScope())
		{
			run = _processingRun;

			if (_isDisposing || _activeReset is not null || run?.ResetMessage is not null)
				return (run, TerminalAction.Ignore);

			_activeReset = resetMessage;

			if (run is null)
			{
				action = TerminalAction.SendNow;
			}
			else
			{
				run.ResetMessage = resetMessage;
				run.IsStopping = true;
				action = run.LifecycleTask.IsCompleted || run.ResetDecided
					? TerminalAction.SendNow
					: TerminalAction.Lifecycle;

				if (!run.LifecycleTask.IsCompleted)
					cancellationSource = run.CancellationSource;
			}

			if (run is not null && !run.LifecycleTask.IsCompleted && !_stopRequestInProgress)
			{
				_stopRequestInProgress = true;
				shouldStop = true;
			}
		}

		if (shouldStop)
			StopManager(cancellationSource);

		return (run, action);
	}

	private void StopManager(CancellationTokenSource cancellationSource)
	{
		try
		{
			_marketDataManager.Stop();
		}
		finally
		{
			try
			{
				Cancel(cancellationSource);
			}
			finally
			{
				try
				{
					ReleaseProcessingGate();
				}
				finally
				{
					using (_processingSync.EnterScope())
						_stopRequestInProgress = false;
				}
			}
		}
	}

	private void ReleaseProcessingGate()
	{
		try
		{
			_processingGate.Set();
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private static void Cancel(CancellationTokenSource cancellationSource)
	{
		try
		{
			cancellationSource?.Cancel();
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private async Task CompleteLifecycleAsync(ReplayRun run)
	{
		var replayTerminal = await run.ReplayTask.ConfigureAwait(false);
		Task startPublication;

		using (_processingSync.EnterScope())
			startPublication = run.StartPublicationTask;

		await startPublication.ConfigureAwait(false);

		EmulationStateMessage terminalMessage;

		using (_processingSync.EnterScope())
		{
			terminalMessage = replayTerminal?.Error is not null
				? replayTerminal
				: run.ExplicitStopping ?? replayTerminal;

			run.TerminalMessage = terminalMessage;
			run.TerminalDecided = true;

			if (terminalMessage is not null)
				run.IsStopping = true;
		}

		try
		{
			if (replayTerminal?.Error is { } error)
				await SendLifecycleErrorAsync(error);

			if (terminalMessage is not null)
				await SendTerminalAsync(run, terminalMessage, CancellationToken.None, true);

			DisconnectMessage disconnectMessage;

			using (_processingSync.EnterScope())
			{
				run.DisconnectDecided = true;
				disconnectMessage = run.DisconnectMessage;
			}

			if (disconnectMessage is not null)
				await SendDisconnectAsync(run, disconnectMessage, CancellationToken.None, true);

			ResetMessage resetMessage;

			using (_processingSync.EnterScope())
			{
				run.ResetDecided = true;
				resetMessage = run.ResetMessage;
			}

			if (resetMessage is not null)
				await SendResetAsync(run, resetMessage, CancellationToken.None, true);
		}
		finally
		{
			run.CancellationSource.Dispose();
		}
	}

	private bool TryReservePublication()
	{
		using (_processingSync.EnterScope())
			return !_isDisposing;
	}

	private async ValueTask SendLifecycleErrorAsync(Exception error)
	{
		if (!TryReservePublication())
			return;

		try
		{
			await SendOutMessageAsync(error.ToErrorMessage(), CancellationToken.None);
		}
		catch (Exception ex)
		{
			this.AddErrorLog(ex);
		}
	}

	private async ValueTask SendTerminalAsync(
		ReplayRun run,
		EmulationStateMessage terminalMessage,
		CancellationToken cancellationToken,
		bool logError = false)
	{
		if (!TryReservePublication())
		{
			MarkTerminalPublicationCompleted(run);
			return;
		}

		try
		{
			await SendOutMessageAsync(terminalMessage, cancellationToken);
		}
		catch (Exception ex) when (logError)
		{
			this.AddErrorLog(ex);
		}
		finally
		{
			MarkTerminalPublicationCompleted(run);
		}
	}

	private void MarkTerminalPublicationCompleted(ReplayRun run)
	{
		if (run is null)
			return;

		using (_processingSync.EnterScope())
			run.TerminalPublicationCompleted = true;
	}

	private async ValueTask SendDisconnectAsync(
		ReplayRun run,
		DisconnectMessage disconnectMessage,
		CancellationToken cancellationToken,
		bool logError = false)
	{
		if (!TryReservePublication())
		{
			MarkDisconnectPublicationCompleted(run);
			return;
		}

		try
		{
			await SendOutMessageAsync(disconnectMessage, cancellationToken);
		}
		catch (Exception ex) when (logError)
		{
			this.AddErrorLog(ex);
		}
		finally
		{
			MarkDisconnectPublicationCompleted(run);
		}
	}

	private void MarkDisconnectPublicationCompleted(ReplayRun run)
	{
		if (run is null)
			return;

		using (_processingSync.EnterScope())
			run.DisconnectPublicationCompleted = true;
	}

	private async ValueTask SendResetAsync(
		ReplayRun run,
		ResetMessage resetMessage,
		CancellationToken cancellationToken,
		bool logError = false)
	{
		if (!TryReservePublication())
		{
			CompleteResetPublication(run, resetMessage);
			return;
		}

		try
		{
			_marketDataManager.Reset();
			_supportedMarketDataTypes.Clear();
			_processingGate.Set();

			using (_processingSync.EnterScope())
			{
				if (run is not null && ReferenceEquals(_processingRun, run))
					_processingRun = null;

				if (ReferenceEquals(_activeReset, resetMessage))
					_activeReset = null;
			}

			await SendOutMessageAsync(resetMessage, cancellationToken);
		}
		catch (Exception ex) when (logError)
		{
			this.AddErrorLog(ex);
		}
		finally
		{
			CompleteResetPublication(run, resetMessage);
		}
	}

	private void CompleteResetPublication(ReplayRun run, ResetMessage resetMessage)
	{
		using (_processingSync.EnterScope())
		{
			if (ReferenceEquals(_activeReset, resetMessage))
				_activeReset = null;

			if (run is not null)
				run.ResetPublicationCompleted = true;
		}
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		ReplayRun run;
		bool shouldStopManager;

		using (_processingSync.EnterScope())
		{
			_isDisposing = true;
			run = _processingRun;
			shouldStopManager = !_stopRequestInProgress;

			if (shouldStopManager)
				_stopRequestInProgress = true;

			if (run is not null && !run.LifecycleTask.IsCompleted)
			{
				run.IsStopping = true;
				run.StartPublicationSource?.TrySetResult(false);
			}

		}

		try
		{
			if (shouldStopManager)
				_marketDataManager.Stop();
		}
		finally
		{
			try
			{
				if (run is not null && !run.LifecycleTask.IsCompleted)
					Cancel(run.CancellationSource);

				ReleaseProcessingGate();
			}
			finally
			{
				if (shouldStopManager)
				{
					using (_processingSync.EnterScope())
						_stopRequestInProgress = false;
				}
			}
		}

		base.DisposeManaged();
	}

	/// <inheritdoc />
	public override string ToString()
		=> $"Hist: {StartDate}-{StopDate}";
}
