namespace StockSharp.Bittrex;

public partial class BittrexMessageAdapter
{
	private HttpClient _httpClient;
	private PusherClient _pusherClient;

	/// <summary>
	/// Initializes a new instance of the <see cref="BittrexMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public BittrexMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
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
	public override bool IsSupportOrderBookIncrements => true;

	/// <inheritdoc />
	public override string[] AssociatedBoards => [BoardCodes.Bittrex];

	private void SubscribePusherClient()
	{
		_pusherClient.Connected += SessionOnPusherConnected;
		_pusherClient.Disconnected += SessionOnPusherDisconnected;
		_pusherClient.Error += SendOutErrorAsync;
		_pusherClient.OrderBookChanged += SessionOnOrderBookChanged;
		_pusherClient.NewTrade += SessionOnNewTrade;
		_pusherClient.TickerChanged += SessionOnTickerChanged;
		_pusherClient.BalanceChanged += SessionOnBalanceChanged;
		_pusherClient.OrderChanged += SessionOnOrderChanged;
	}

	private void UnsubscribePusherClient()
	{
		_pusherClient.Connected -= SessionOnPusherConnected;
		_pusherClient.Disconnected -= SessionOnPusherDisconnected;
		_pusherClient.Error -= SendOutErrorAsync;
		_pusherClient.OrderBookChanged -= SessionOnOrderBookChanged;
		_pusherClient.NewTrade -= SessionOnNewTrade;
		_pusherClient.TickerChanged -= SessionOnTickerChanged;
		_pusherClient.BalanceChanged -= SessionOnBalanceChanged;
		_pusherClient.OrderChanged -= SessionOnOrderChanged;
	}

	/// <inheritdoc />
	protected override async ValueTask ConnectAsync(ConnectMessage msg, CancellationToken cancellationToken)
	{
		var canSign = false;

		if (this.IsTransactional())
		{
			if (Key.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.KeyNotSpecified);

			if (Secret.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.SecretNotSpecified);

			canSign = true;
		}

		if (_httpClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		if (_pusherClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		var authenticator = new Authenticator(Key, Secret);

		_httpClient = new HttpClient(authenticator) { Parent = this };

		_pusherClient = new PusherClient(authenticator, canSign) { Parent = this };
		SubscribePusherClient();
		await _pusherClient.ConnectAsync(cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask DisconnectAsync(DisconnectMessage msg, CancellationToken cancellationToken)
	{
		if (_httpClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		if (_pusherClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		_httpClient.Dispose();
		_httpClient = null;

		return _pusherClient.DisconnectAsync(cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask ResetAsync(ResetMessage msg, CancellationToken cancellationToken)
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
				await _pusherClient.DisconnectAsync(cancellationToken);
			}
			catch (Exception ex)
			{
				await SendOutErrorAsync(ex, cancellationToken);
			}

			_pusherClient = null;
		}

		_orderInfo.Clear();

		_orderBooks.Clear();

		_wsTradesSubscriptions.Clear();
		_wsBookSubscriptions.Clear();
		_wsSubscriptions.Clear();

		_summarySubscribed = false;

		await SendOutMessageAsync(new ResetMessage(), cancellationToken);
	}

	private ValueTask SessionOnPusherConnected(CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ConnectMessage(), cancellationToken);
	}

	private ValueTask SessionOnPusherDisconnected(bool expected, CancellationToken cancellationToken)
	{
		return SendOutDisconnectMessageAsync(expected, cancellationToken);
	}
}
