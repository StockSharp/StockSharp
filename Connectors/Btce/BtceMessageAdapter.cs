namespace StockSharp.Btce
{
	[OrderCondition(typeof(BtceOrderCondition))]
	public partial class BtceMessageAdapter : MessageAdapter
	{
		private HttpClient _httpClient;
		private PusherClient _pusherClient;
		private DateTimeOffset? _lastTimeBalanceCheck;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="BtceMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public BtceMessageAdapter(IdGenerator transactionIdGenerator)
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

			this.AddSupportedResultMessage(MessageTypes.SecurityLookup);
			this.AddSupportedResultMessage(MessageTypes.PortfolioLookup);
			this.AddSupportedResultMessage(MessageTypes.OrderStatus);
		}

		/// <inheritdoc />
		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

		/// <inheritdoc />
		public override bool IsSupportOrderBookIncrements => true;

		/// <inheritdoc />
		public override string AssociatedBoard => BoardCodes.Btce;

		/// <inheritdoc />
		public override TimeSpan GetHistoryStepSize(SecurityId securityId, DataType dataType, out TimeSpan iterationInterval)
		{
			var step = base.GetHistoryStepSize(securityId, dataType, out iterationInterval);
			
			if (dataType == DataType.Ticks)
				step = TimeSpan.FromDays(1);

			return step;
		}

		private void SubscribePusherClient()
		{
			_pusherClient.Connected += SessionOnPusherConnected;
			_pusherClient.Disconnected += SessionOnPusherDisconnected;
			_pusherClient.Error += SessionOnPusherError;
			_pusherClient.OrderBookChanged += SessionOnOrderBookChanged;
			_pusherClient.NewTrades += SessionOnNewTrades;
		}

		private void UnsubscribePusherClient()
		{
			_pusherClient.Connected -= SessionOnPusherConnected;
			_pusherClient.Disconnected -= SessionOnPusherDisconnected;
			_pusherClient.Error -= SessionOnPusherError;
			_pusherClient.OrderBookChanged -= SessionOnOrderBookChanged;
			_pusherClient.NewTrades -= SessionOnNewTrades;
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_orderBooks.Clear();

					_orderInfo.Clear();
					//_unkOrds.Clear();
					_positions.Clear();

					_lastMyTradeId = 0;
					_lastTimeBalanceCheck = null;

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

					if (_httpClient != null)
						throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

					if (_pusherClient != null)
						throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

					_httpClient = new HttpClient(Address, Key, Secret);

					_pusherClient = new PusherClient { Parent = this };
					SubscribePusherClient();
					_pusherClient.Connect();

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_httpClient == null)
						throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

					if (_pusherClient == null)
						throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

					_httpClient.Dispose();
					_httpClient = null;

					_pusherClient.Disconnect();

					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					ProcessPortfolioLookup((PortfolioLookupMessage)message);
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					ProcessSecurityLookup((SecurityLookupMessage)message);
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

				case MessageTypes.MarketData:
				{
					ProcessMarketData((MarketDataMessage)message);
					break;
				}

				case MessageTypes.Time:
				{
					if (_orderInfo.Count > 0/* || _unkOrds.Count > 0*/)
					{
						ProcessOrderStatus(null);
						ProcessPortfolioLookup(null);
					}

					if (BalanceCheckInterval > TimeSpan.Zero &&
					    (_lastTimeBalanceCheck == null || (CurrentTime - _lastTimeBalanceCheck) > BalanceCheckInterval))
					{
						ProcessPortfolioLookup(null);
					}

					break;
				}
			
				default:
					return false;
			}

			return true;
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
}