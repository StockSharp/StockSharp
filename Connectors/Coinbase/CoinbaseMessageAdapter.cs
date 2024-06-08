namespace StockSharp.Coinbase;

[OrderCondition(typeof(CoinbaseOrderCondition))]
public partial class CoinbaseMessageAdapter
{
	private Authenticator _authenticator;
	private HttpClient _httpClient;
	private PusherClient _pusherClient;
	private DateTimeOffset? _lastTimeBalanceCheck;

	/// <summary>
	/// Initializes a new instance of the <see cref="CoinbaseMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public CoinbaseMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		HeartbeatInterval = TimeSpan.FromSeconds(5);

		this.AddMarketDataSupport();
		this.AddTransactionalSupport();
		this.RemoveSupportedMessage(MessageTypes.Portfolio);
		this.RemoveSupportedMessage(MessageTypes.OrderReplace);

		this.AddSupportedMarketDataType(DataType.Ticks);
		this.AddSupportedMarketDataType(DataType.MarketDepth);
		this.AddSupportedMarketDataType(DataType.Level1);
		this.AddSupportedMarketDataType(DataType.OrderLog);
		this.AddSupportedMarketDataType(DataType.CandleTimeFrame);

		this.AddSupportedResultMessage(MessageTypes.SecurityLookup);
		this.AddSupportedResultMessage(MessageTypes.PortfolioLookup);
		this.AddSupportedResultMessage(MessageTypes.OrderStatus);
	}

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	protected override IEnumerable<TimeSpan> TimeFrames => AllTimeFrames;

	/// <inheritdoc />
	public override bool IsSupportOrderBookIncrements => true;

	/// <inheritdoc />
	public override string AssociatedBoard => BoardCodes.Coinbase;

	private void SubscribePusherClient()
	{
		_pusherClient.Connected += SessionOnPusherConnected;
		_pusherClient.Disconnected += SessionOnPusherDisconnected;
		_pusherClient.Error += SessionOnPusherError;
		_pusherClient.Heartbeat += SessionOnHeartbeat;
		_pusherClient.TickerChanged += SessionOnTickerChanged;
		_pusherClient.OrderBookSnapshot += SessionOnOrderBookSnapshot;
		_pusherClient.OrderBookChanged += SessionOnOrderBookChanged;
		_pusherClient.NewTrade += SessionOnNewTrade;
		_pusherClient.NewOrderLog += SessionOnNewOrderLog;
	}

	private void UnsubscribePusherClient()
	{
		_pusherClient.Connected -= SessionOnPusherConnected;
		_pusherClient.Disconnected -= SessionOnPusherDisconnected;
		_pusherClient.Error -= SessionOnPusherError;
		_pusherClient.Heartbeat -= SessionOnHeartbeat;
		_pusherClient.TickerChanged -= SessionOnTickerChanged;
		_pusherClient.OrderBookSnapshot -= SessionOnOrderBookSnapshot;
		_pusherClient.OrderBookChanged -= SessionOnOrderBookChanged;
		_pusherClient.NewTrade -= SessionOnNewTrade;
		_pusherClient.NewOrderLog -= SessionOnNewOrderLog;
	}

	/// <inheritdoc />
	public override ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		if (_httpClient != null)
		{
			try
			{
				_httpClient.Dispose();
			}
			catch (Exception ex)
			{
				SendOutError(ex);
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
				SendOutError(ex);
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
				SendOutError(ex);
			}

			_authenticator = null;
		}

		_orderInfo.Clear();
		_lastTimeBalanceCheck = null;

		SendOutMessage(new ResetMessage());
		return default;
	}

	/// <inheritdoc />
	public override async ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		if (this.IsTransactional())
		{
			if (Key.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.KeyNotSpecified);

			if (Secret.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.SecretNotSpecified);
		}

		_authenticator = new Authenticator(this.IsTransactional(), Key, Secret, Passphrase);

		if (_httpClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		if (_pusherClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		_httpClient = new HttpClient(_authenticator) { Parent = this };

		_pusherClient = new PusherClient(_authenticator) { Parent = this };
		SubscribePusherClient();

		await _pusherClient.Connect(cancellationToken);
		await _pusherClient.SubscribeStatus(cancellationToken);
	}

	/// <inheritdoc />
	public override async ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		if (_httpClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		if (_pusherClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		_httpClient.Dispose();
		_httpClient = null;

		await _pusherClient.UnSubscribeStatus(cancellationToken);
		_pusherClient.Disconnect();
	}

	/// <inheritdoc />
	public override async ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
	{
		if (_orderInfo.Count > 0)
		{
			await OrderStatusAsync(null, cancellationToken);
			await PortfolioLookupAsync(null, cancellationToken);
		}

		if (BalanceCheckInterval > TimeSpan.Zero &&
			(_lastTimeBalanceCheck == null || (CurrentTime - _lastTimeBalanceCheck) > BalanceCheckInterval))
		{
			await PortfolioLookupAsync(null, cancellationToken);
		}
	}

	private void SessionOnPusherConnected()
	{
		SendOutMessage(new ConnectMessage());
	}

	private void SessionOnPusherError(Exception exception)
	{
		SendOutError(exception);
	}

	private void SessionOnPusherDisconnected(bool expected)
	{
		SendOutDisconnectMessage(expected);
	}
}