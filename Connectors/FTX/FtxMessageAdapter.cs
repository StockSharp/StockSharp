namespace StockSharp.FTX;

public partial class FtxMessageAdapter
{
	private FtxRestClient _restClient;
	private FtxWebSocketClient _wsClient;
	private DateTimeOffset _lastStateUpdate;

	/// <summary>
	/// All possible time frames.
	/// </summary>
	public static TimeSpan[] AllTimeFrames { get; } =
	[
		TimeSpan.FromSeconds(15),
		TimeSpan.FromSeconds(60),
		TimeSpan.FromSeconds(300),
		TimeSpan.FromSeconds(900),
		TimeSpan.FromSeconds(3600),
		TimeSpan.FromSeconds(14400),
		TimeSpan.FromSeconds(86400),
	];

	/// <inheritdoc />
	public override IEnumerable<int> SupportedOrderBookDepths => new[] { 100 };

	/// <summary>
	/// Initializes a new instance of the <see cref="FtxMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public FtxMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		HeartbeatInterval = DefaultHeartbeatInterval;

		this.AddMarketDataSupport();
		this.AddTransactionalSupport();
		this.RemoveSupportedMessage(MessageTypes.OrderReplace);

		this.AddSupportedMarketDataType(DataType.Ticks);
		this.AddSupportedMarketDataType(DataType.MarketDepth);
		this.AddSupportedMarketDataType(DataType.Level1);
		this.AddSupportedCandleTimeFrames(AllTimeFrames);
	}

	/// <inheritdoc />
	public override string[] AssociatedBoards { get; } = new[] { BoardCodes.FTX };

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

	private void SubscribeWsClient()
	{
		_wsClient.StateChanged += SendOutConnectionState;
		_wsClient.Error += SessionOnWsError;
		_wsClient.NewLevel1 += SessionOnNewLevel1;
		_wsClient.NewOrderBook += SessionOnNewOrderBook;
		_wsClient.NewTrade += SessionOnNewTrade;
		_wsClient.NewFill += SessionOnNewFill;
		_wsClient.NewOrder += SessionOnNewOrder;
	}

	private void UnsubscribeWsClient()
	{
		_wsClient.StateChanged -= SendOutConnectionState;
		_wsClient.Error -= SessionOnWsError;
		_wsClient.NewLevel1 -= SessionOnNewLevel1;
		_wsClient.NewOrderBook -= SessionOnNewOrderBook;
		_wsClient.NewTrade -= SessionOnNewTrade;
		_wsClient.NewFill -= SessionOnNewFill;
		_wsClient.NewOrder -= SessionOnNewOrder;
	}

	/// <inheritdoc />
	public override ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		_lastStateUpdate = default;

		_restClient?.Dispose();
		_restClient = null;

		if (_wsClient != null)
		{
			UnsubscribeWsClient();

			_wsClient.Dispose();
			_wsClient = null;
		}

		_portfolioLookupSubMessageTransactionID = default;
		_isOrderSubscribed = default;

		SendOutMessage(new ResetMessage());
		return default;
	}

	/// <inheritdoc />
	public override ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		if (this.IsTransactional())
		{
			if (Key.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.KeyNotSpecified);

			if (Secret.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.SecretNotSpecified);
		}

		if (_restClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		if (_wsClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		_restClient = new(Key, Secret) { Parent = this };
		_wsClient = new(Key, Secret, SubaccountName, ReConnectionSettings.ReAttemptCount) { Parent = this };

		SubscribeWsClient();
		return _wsClient.Connect(cancellationToken);
	}

	/// <inheritdoc />
	public override ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		if (_restClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		if (_wsClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		_wsClient.Disconnect();

		return default;
	}

	/// <inheritdoc />
	public override async ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
	{
		if ((DateTime.UtcNow - _lastStateUpdate).TotalMilliseconds >= 1000)
		{
			await PortfolioLookupAsync(null, cancellationToken);
			_lastStateUpdate = DateTime.UtcNow;
		}

		if (_wsClient is FtxWebSocketClient sc)
			await sc.ProcessPing(cancellationToken);
	}

	private void SessionOnWsError(Exception exception)
	{
		SendOutError(exception);
	}
}