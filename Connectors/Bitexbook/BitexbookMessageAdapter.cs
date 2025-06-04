namespace StockSharp.Bitexbook;

[OrderCondition(typeof(BitexbookOrderCondition))]
public partial class BitexbookMessageAdapter
{
	private HttpClient _httpClient;
	private PusherClient _pusherClient;
	private DateTimeOffset? _lastTimeBalanceCheck;

	/// <summary>
	/// Initializes a new instance of the <see cref="BitexbookMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public BitexbookMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		HeartbeatInterval = TimeSpan.FromSeconds(1);

		this.AddMarketDataSupport();
		this.AddTransactionalSupport();
		this.RemoveSupportedMessage(MessageTypes.OrderReplace);
		this.RemoveSupportedMessage(MessageTypes.OrderGroupCancel);

		this.AddSupportedMarketDataType(DataType.OrderLog);
		this.AddSupportedCandleTimeFrames(AllTimeFrames);
	}

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	public override string[] AssociatedBoards { get; } = [BoardCodes.Bitexbook];

	private void SubscribePusherClient()
	{
		_pusherClient.StateChanged += SendOutConnectionState;
		_pusherClient.Error += SessionOnPusherError;
		_pusherClient.NewSymbols += SessionOnNewSymbols;
		_pusherClient.TickerChanged += SessionOnTickerChanged;
		_pusherClient.NewTickerChange += SessionOnNewTickerChange;
		_pusherClient.TicketsActive += SessionOnTicketsActive;
		_pusherClient.TicketAdded += SessionOnTicketAdded;
		_pusherClient.TicketCanceled += SessionOnTicketCanceled;
		_pusherClient.TicketExecuted += SessionOnTicketExecuted;
	}

	private void UnsubscribePusherClient()
	{
		_pusherClient.StateChanged -= SendOutConnectionState;
		_pusherClient.Error -= SessionOnPusherError;
		_pusherClient.NewSymbols -= SessionOnNewSymbols;
		_pusherClient.TickerChanged -= SessionOnTickerChanged;
		_pusherClient.NewTickerChange -= SessionOnNewTickerChange;
		_pusherClient.TicketsActive -= SessionOnTicketsActive;
		_pusherClient.TicketAdded -= SessionOnTicketAdded;
		_pusherClient.TicketCanceled -= SessionOnTicketCanceled;
		_pusherClient.TicketExecuted -= SessionOnTicketExecuted;
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

		_secIdMapping.Clear();

		_orderInfo.Clear();
		_lastTimeBalanceCheck = null;

		SendOutMessage(new ResetMessage());

		return default;
	}

	/// <inheritdoc />
	public override ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		//if (this.IsTransactional())
		//{
		//	if (Key.IsEmpty())
		//		throw new InvalidOperationException(LocalizedStrings.KeyNotSpecified);

		//	if (Secret.IsEmpty())
		//		throw new InvalidOperationException(LocalizedStrings.SecretNotSpecified);
		//}

		if (_httpClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		if (_pusherClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		_httpClient = new(Key, Secret) { Parent = this };
		_pusherClient = new(ReConnectionSettings.AttemptCount) { Parent = this };

		SubscribePusherClient();

		return _pusherClient.Connect(cancellationToken);
	}

	/// <inheritdoc />
	public override ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
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

	private void SessionOnPusherError(Exception exception)
	{
		SendOutError(exception);
	}
}