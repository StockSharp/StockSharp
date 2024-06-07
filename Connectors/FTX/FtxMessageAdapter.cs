namespace StockSharp.FTX;

public partial class FtxMessageAdapter : MessageAdapter
{
	private FtxRestClient _restClient;
	private FtxWebSocketClient _wsClient;
	private DateTimeOffset _lastStateUpdate;

	private readonly TimeSpan[] _timeFrames = new[]
	{
		TimeSpan.FromSeconds(15),
		TimeSpan.FromSeconds(60),
		TimeSpan.FromSeconds(300),
		TimeSpan.FromSeconds(900),
		TimeSpan.FromSeconds(3600),
		TimeSpan.FromSeconds(14400),
		TimeSpan.FromSeconds(86400),
	};

	/// <inheritdoc />
	protected override IEnumerable<TimeSpan> TimeFrames => _timeFrames;

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
		this.RemoveSupportedMessage(MessageTypes.Portfolio);
		this.RemoveSupportedMessage(MessageTypes.OrderReplace);


		this.AddSupportedMarketDataType(DataType.Ticks);
		this.AddSupportedMarketDataType(DataType.MarketDepth);
		this.AddSupportedMarketDataType(DataType.Level1);
		this.AddSupportedMarketDataType(DataType.CandleTimeFrame);

		this.AddSupportedResultMessage(MessageTypes.SecurityLookup);
		this.AddSupportedResultMessage(MessageTypes.PortfolioLookup);
	}

	/// <inheritdoc />
	public override string AssociatedBoard => BoardCodes.FTX;

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	public override TimeSpan GetHistoryStepSize(SecurityId securityId, DataType dataType, out TimeSpan iterationInterval)
	{
		var step = base.GetHistoryStepSize(securityId, dataType, out iterationInterval);

		if (dataType == DataType.Ticks)
			step = TimeSpan.FromDays(1);

		if (dataType.MessageType == typeof(TimeFrameCandleMessage))
			step = TimeSpan.FromDays(1);

		return step;
	}

	private void SubscribeWsClient()
	{
		_wsClient.Connected += SessionOnWsConnected;
		_wsClient.Disconnected += SessionOnWsDisconnected;
		_wsClient.Error += SessionOnWsError;
		_wsClient.NewLevel1 += SessionOnNewLevel1;
		_wsClient.NewOrderBook += SessionOnNewOrderBook;
		_wsClient.NewTrade += SessionOnNewTrade;
		_wsClient.NewFill += SessionOnNewFill;
		_wsClient.NewOrder += SessionOnNewOrder;
	}

	private void UnsubscribeWsClient()
	{
		_wsClient.Connected -= SessionOnWsConnected;
		_wsClient.Disconnected -= SessionOnWsDisconnected;
		_wsClient.Error -= SessionOnWsError;
		_wsClient.NewLevel1 -= SessionOnNewLevel1;
		_wsClient.NewOrderBook -= SessionOnNewOrderBook;
		_wsClient.NewTrade -= SessionOnNewTrade;
		_wsClient.NewFill -= SessionOnNewFill;
		_wsClient.NewOrder -= SessionOnNewOrder;
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_lastStateUpdate = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

				_restClient?.Dispose();
				_restClient = null;


				if (_wsClient != null)
				{
					UnsubscribeWsClient();
					_wsClient.Disconnect();

					_wsClient = null;
				}

				SendOutMessage(new ResetMessage());

				break;
			}

			case MessageTypes.Connect:
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

				_restClient = new FtxRestClient(Key, Secret)
				{
					Parent = this
				};

				_wsClient = new FtxWebSocketClient(Key, Secret, SubaccountName)
				{
					Parent = this
				};

				SubscribeWsClient();
				_wsClient.Connect();

				break;
			}

			case MessageTypes.Disconnect:
			{
				if (_restClient == null)
					throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

				if (_wsClient == null)
					throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

				_restClient.Dispose();
				_restClient = null;
				_wsClient.Disconnect();

				break;
			}

			case MessageTypes.PortfolioLookup:
			{
				ProcessPortfolioLookup((PortfolioLookupMessage)message);
				break;
			}

			case MessageTypes.OrderStatus:
			{
				ProcessOrderStatus((OrderStatusMessage)message);
				break;
			}

			case MessageTypes.OrderRegister:
			{
				ProcessOrderRegister((OrderRegisterMessage)message);
				break;
			}

			case MessageTypes.OrderCancel:
			{
				ProcessOrderCancel((OrderCancelMessage)message);
				break;
			}

			case MessageTypes.OrderGroupCancel:
			{
				ProcessOrderGroupCancel((OrderGroupCancelMessage)message);
				break;
			}

			case MessageTypes.Time:
			{
				if ((DateTime.UtcNow - _lastStateUpdate).TotalMilliseconds >= 1000)
				{
					ProcessPortfolioLookup(null);
					_lastStateUpdate = DateTime.UtcNow;
				}

				_wsClient?.ProcessPing();
				break;
			}

			case MessageTypes.SecurityLookup:
			{
				ProcessSecurityLookup((SecurityLookupMessage)message);
				break;
			}

			case MessageTypes.MarketData:
			{
				ProcessMarketData((MarketDataMessage)message);
				break;
			}

			default:
				return false;
		}

		return true;
	}

	private void SessionOnWsConnected()
	{
		SendOutMessage(new ConnectMessage());
	}

	private void SessionOnWsError(Exception exception)
	{
		SendOutError(exception);
	}

	private void SessionOnWsDisconnected(bool expected)
	{
		SendOutDisconnectMessage(expected);
	}
}