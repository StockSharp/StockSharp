namespace StockSharp.Coinbase
{
#if !NO_LICENSE
	using StockSharp.Licensing;
#endif

	[OrderCondition(typeof(CoinbaseOrderCondition))]
	public partial class CoinbaseMessageAdapter : MessageAdapter
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

		/// <inheritdoc />
		public override TimeSpan GetHistoryStepSize(SecurityId securityId, DataType dataType, out TimeSpan iterationInterval)
		{
			if (dataType.IsCandles)
			{
				iterationInterval = TimeSpan.FromSeconds(1);

				var tf = (TimeSpan)dataType.Arg;

				if (tf.TotalMinutes <= 1)
					return TimeSpan.FromHours(1);
				else if (tf.TotalDays < 1)
					return TimeSpan.FromDays(2);
				else
					return TimeSpan.FromTicks(tf.Ticks * 100);
			}

			return base.GetHistoryStepSize(securityId, dataType, out iterationInterval);
		}

#if !NO_LICENSE
		/// <inheritdoc />
		public override string FeatureName => nameof(Coinbase);
#endif

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
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
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

#if !NO_LICENSE
					var msg = "Crypto".ValidateLicense(component: GetType());
					if (!msg.IsEmpty())
					{
						msg = nameof(Coinbase).ValidateLicense(component: GetType());
						
						if (!msg.IsEmpty())
							throw new InvalidOperationException(msg);
					}
#endif

					_authenticator = new Authenticator(this.IsTransactional(), Key, Secret, Passphrase);

					if (_httpClient != null)
						throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

					if (_pusherClient != null)
						throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

					_httpClient = new HttpClient(_authenticator) { Parent = this };

					_pusherClient = new PusherClient(_authenticator) { Parent = this };
					SubscribePusherClient();
					_pusherClient.Connect();

					_pusherClient.SubscribeStatus();

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

					_pusherClient.UnSubscribeStatus();
					_pusherClient.Disconnect();

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
					if (_orderInfo.Count > 0)
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