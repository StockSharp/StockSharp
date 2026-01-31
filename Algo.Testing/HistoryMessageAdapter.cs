namespace StockSharp.Algo.Testing;

using System.Runtime.CompilerServices;

using StockSharp.Algo.Testing.Generation;

/// <summary>
/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
/// </summary>
public class HistoryMessageAdapter : MessageAdapter, IEmulationMessageAdapter
{
	private readonly IHistoryMarketDataManager _marketDataManager;

	private CancellationTokenSource _cancellationTokenSource;

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
	public override DateTime CurrentTimeUtc => _marketDataManager.CurrentTime;

	/// <inheritdoc />
	public override bool UseOutChannel => false;

	/// <inheritdoc />
	public override bool IsFullCandlesOnly => false;

	/// <inheritdoc />
	public override bool IsSupportCandlesUpdates(MarketDataMessage subscription) => true;

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

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
				_marketDataManager.Reset();
				_supportedMarketDataTypes.Clear();

				if (!_marketDataManager.IsStarted)
					await SendOutMessageAsync(new ResetMessage(), cancellationToken);

				break;
			}

			case MessageTypes.Connect:
			{
				if (_marketDataManager.IsStarted)
					throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

				await SendOutMessageAsync(new ConnectMessage { LocalTime = StartDate }, cancellationToken);
				break;
			}

			case MessageTypes.Disconnect:
			{
				_marketDataManager.Stop();
				await SendOutMessageAsync(new DisconnectMessage { LocalTime = StopDate }, cancellationToken);
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
						if (!_marketDataManager.IsStarted)
						{
							_ = StartProcessing(
								stateMsg.StartDate == default ? StartDate : stateMsg.StartDate,
								stateMsg.StopDate == default ? StopDate : stateMsg.StopDate,
								cancellationToken);
						}

						break;
					}

					case ChannelStates.Stopping:
					{
						StopProcessing();
						break;
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

	private Task StartProcessing(DateTime startDate, DateTime stopDate, CancellationToken cancellationToken)
	{
		_marketDataManager.StartDate = startDate;
		_marketDataManager.StopDate = stopDate;

		var (cts, t) = cancellationToken.CreateChildToken();
		_cancellationTokenSource = cts;
		var token = t;

		var boards = CheckTradableDates ? GetBoards() : [];

		return Task.Run(async () =>
		{
			await Task.Yield();

			try
			{
				await foreach (var message in _marketDataManager.StartAsync(boards).WithCancellation(token))
				{
					await SendOutMessageAsync(message, token);
				}
			}
			catch (Exception ex)
			{
				if (!token.IsCancellationRequested)
					await SendOutMessageAsync(ex.ToErrorMessage(), token);

				await SendOutMessageAsync(new EmulationStateMessage
				{
					LocalTime = stopDate,
					State = ChannelStates.Stopping,
					Error = ex,
				}, token);
			}
		}, token);
	}

	private void StopProcessing()
	{
		_marketDataManager.Stop();
		_cancellationTokenSource?.Cancel();
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		StopProcessing();
		base.DisposeManaged();
	}

	/// <inheritdoc />
	public override string ToString()
		=> $"Hist: {StartDate}-{StopDate}";
}