namespace StockSharp.PrizmBit;

partial class PrizmBitMessageAdapter
{
	private HttpClient _httpClient;
	private PusherClient _pusherClient;
	private Authenticator _authenticator;

	/// <summary>
	/// Initializes a new instance of the <see cref="PrizmBitMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public PrizmBitMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		HeartbeatInterval = TimeSpan.FromSeconds(1);

		this.AddMarketDataSupport();
		this.AddTransactionalSupport();

		this.AddSupportedMarketDataType(DataType.Ticks);
		this.AddSupportedMarketDataType(DataType.OrderLog);
		//this.AddSupportedMarketDataType(DataType.Level1);
		this.AddSupportedCandleTimeFrames(AllTimeFrames);
	}

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || dataType == DataType.Transactions || dataType == DataType.PositionChanges || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	public override bool IsReplaceCommandEditCurrent => true;

	/// <inheritdoc />
	public override string[] AssociatedBoards => [BoardCodes.PrizmBit];

	private void SubscribePusherClient()
	{
		_pusherClient.StateChanged += SendOutConnectionStateAsync;
		_pusherClient.Error += SendOutErrorAsync;
		_pusherClient.MarketPriceChanged += SessionOnMarketPriceChanged;
		_pusherClient.OrderBookChanged += SessionOnOrderBookChanged;
		_pusherClient.NewTrade += SessionOnNewTrade;
		_pusherClient.OrderCanceled += SessionOnOrderCanceled;
		_pusherClient.NewUserTrade += SessionOnNewUserTrade;
		_pusherClient.UserOrderCanceled += SessionOnUserOrderCanceled;
		_pusherClient.BalanceChanged += SessionOnBalanceChanged;
	}

	private void UnsubscribePusherClient()
	{
		_pusherClient.StateChanged -= SendOutConnectionStateAsync;
		_pusherClient.Error -= SendOutErrorAsync;
		_pusherClient.MarketPriceChanged -= SessionOnMarketPriceChanged;
		_pusherClient.OrderBookChanged -= SessionOnOrderBookChanged;
		_pusherClient.NewTrade -= SessionOnNewTrade;
		_pusherClient.OrderCanceled -= SessionOnOrderCanceled;
		_pusherClient.NewUserTrade -= SessionOnNewUserTrade;
		_pusherClient.UserOrderCanceled -= SessionOnUserOrderCanceled;
		_pusherClient.BalanceChanged -= SessionOnBalanceChanged;
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

		_currencies.Clear();
		_markets.Clear();

		_pfNames.Clear();

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

		if (_authenticator != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		_authenticator = new Authenticator(this.IsTransactional(), Key, Secret);

		_httpClient = new HttpClient(_authenticator, IsDemo) { Parent = this };

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

		if (_authenticator == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		_httpClient.Dispose();
		_httpClient = null;

		_pusherClient.Disconnect();

		return default;
	}
}
