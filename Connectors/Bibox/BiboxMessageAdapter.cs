namespace StockSharp.Bibox;

[OrderCondition(typeof(BiboxOrderCondition))]
public partial class BiboxMessageAdapter
{
	private HttpClient _httpClient;
	private PusherClient _pusherClient;
	private Authenticator _authenticator;

	/// <summary>
	/// Initializes a new instance of the <see cref="BiboxMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public BiboxMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		HeartbeatInterval = TimeSpan.FromSeconds(10);

		this.AddMarketDataSupport();
		this.AddTransactionalSupport();
		this.RemoveSupportedMessage(MessageTypes.OrderReplace);
		this.RemoveSupportedMessage(MessageTypes.OrderGroupCancel);

		this.AddSupportedMarketDataType(DataType.Ticks);
		this.AddSupportedMarketDataType(DataType.MarketDepth);
		this.AddSupportedMarketDataType(DataType.Level1);
		this.AddSupportedCandleTimeFrames(AllTimeFrames);
	}

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || dataType == DataType.Transactions || dataType == DataType.PositionChanges || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	public override string[] AssociatedBoards => [BoardCodes.Bibox];

	/// <inheritdoc />
	public override IEnumerable<int> SupportedOrderBookDepths => [5, 10, 20, 50, 100, 200, 500, 1000];

	private void SubscribePusherClient()
	{
		_pusherClient.StateChanged += SendOutConnectionStateAsync;
		_pusherClient.Error += SendOutErrorAsync;
		_pusherClient.PingReceived += SessionOnPingReceived;
		_pusherClient.TickerChanged += SessionOnTickerChanged;
		_pusherClient.NewTrades += SessionOnNewTrades;
		_pusherClient.NewCandles += SessionOnNewCandles;
		_pusherClient.OrderBookChanged += SessionOnOrderBookChanged;
		_pusherClient.OrderChanged += SessionOnOrderChanged;
		_pusherClient.BalancesChanged += SessionOnBalancesChanged;
	}

	private void UnsubscribePusherClient()
	{
		_pusherClient.StateChanged -= SendOutConnectionStateAsync;
		_pusherClient.Error -= SendOutErrorAsync;
		_pusherClient.PingReceived -= SessionOnPingReceived;
		_pusherClient.TickerChanged -= SessionOnTickerChanged;
		_pusherClient.NewTrades -= SessionOnNewTrades;
		_pusherClient.NewCandles -= SessionOnNewCandles;
		_pusherClient.OrderBookChanged -= SessionOnOrderBookChanged;
		_pusherClient.OrderChanged -= SessionOnOrderChanged;
		_pusherClient.BalancesChanged -= SessionOnBalancesChanged;
	}

	/// <inheritdoc />
	protected override async ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		if (_httpClient != null)
		{
			try
			{
				_httpClient.Dispose();
			}
			catch (Exception ex)
			{
				await SendOutErrorAsync(ex, cancellationToken);
			}

			_httpClient = null;
		}

		if (_pusherClient != null)
		{
			try
			{
				UnsubscribePusherClient();
				_pusherClient.Disconnect();
			}
			catch (Exception ex)
			{
				await SendOutErrorAsync(ex, cancellationToken);
			}

			_pusherClient = null;
		}

		if (_authenticator != null)
		{
			try
			{
				_authenticator.Dispose();
			}
			catch (Exception ex)
			{
				await SendOutErrorAsync(ex, cancellationToken);
			}

			_authenticator = null;
		}

		_candlesTransactions.Clear();

		await base.ResetAsync(resetMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		if (this.IsTransactional())
		{
			if (Key.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.KeyNotSpecified);

			if (Secret.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.SecretNotSpecified);
		}

		if (_httpClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		if (_pusherClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		_authenticator = new Authenticator(this.IsTransactional(), Key, Secret);

		_httpClient = new(_authenticator) { Parent = this };
		_pusherClient = new(_authenticator, ReConnectionSettings.WorkingTime) { Parent = this };

		SubscribePusherClient();

		await _pusherClient.Connect(cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		if (_httpClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		if (_pusherClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		_httpClient.Dispose();
		_httpClient = null;

		_pusherClient.Disconnect();

		return default;
	}

	/// <inheritdoc />
	protected override ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
	{
		if (_pusherClient is PusherClient pc)
			return pc.ProcessPing(cancellationToken);

		return default;
	}

	private ValueTask SessionOnPingReceived(string id, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new TimeMessage
		{
			TransactionId = TransactionIdGenerator.GetNextId(),
			OriginalTransactionId = id,
			BackMode = MessageBackModes.Direct,
		}, cancellationToken);
	}
}
