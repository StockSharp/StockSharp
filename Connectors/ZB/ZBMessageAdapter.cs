namespace StockSharp.ZB;

partial class ZBMessageAdapter
{
	private Authenticator _authenticator;
	private HttpClient _httpClient;
	private PusherClient _pusherClient;

	/// <summary>
	/// Initializes a new instance of the <see cref="ZBMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public ZBMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		//HeartbeatInterval = TimeSpan.FromSeconds(1);

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
	public override string[] AssociatedBoards => [BoardCodes.ZB];

	private void SubscribePusherClient()
	{
		_pusherClient.StateChanged += SendOutConnectionStateAsync;
		_pusherClient.Error += SendOutErrorAsync;
		_pusherClient.TickerChanged += SessionOnTickerChanged;
		_pusherClient.OrderBookChanged += SessionOnOrderBookChanged;
		_pusherClient.NewTrades += SessionOnNewTrades;
		_pusherClient.NewOrders += SessionOnNewOrders;
		_pusherClient.OrderChanged += SessionOnOrderChanged;
		_pusherClient.BalancesChanged += SessionOnBalancesChanged;
	}

	private void UnsubscribePusherClient()
	{
		_pusherClient.StateChanged -= SendOutConnectionStateAsync;
		_pusherClient.Error -= SendOutErrorAsync;
		_pusherClient.TickerChanged -= SessionOnTickerChanged;
		_pusherClient.OrderBookChanged -= SessionOnOrderBookChanged;
		_pusherClient.NewTrades -= SessionOnNewTrades;
		_pusherClient.NewOrders -= SessionOnNewOrders;
		_pusherClient.OrderChanged -= SessionOnOrderChanged;
		_pusherClient.BalancesChanged -= SessionOnBalancesChanged;
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

		_httpClient = new HttpClient(_authenticator) { Parent = this };
		_pusherClient = new PusherClient(_authenticator, ReConnectionSettings.WorkingTime) { Parent = this };

		SubscribePusherClient();
		await _pusherClient.ConnectAsync(cancellationToken);
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

		await SendOutMessageAsync(new ResetMessage(), cancellationToken);
	}
}
