namespace StockSharp.BitStamp
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.BitStamp.Native;
#if !IGNORE_LICENSE
	using StockSharp.Licensing;
#endif
	using StockSharp.Messages;
	using StockSharp.Localization;

	[OrderCondition(typeof(BitStampOrderCondition))]
	public partial class BitStampMessageAdapter : MessageAdapter
	{
		private long _lastMyTradeId;
		private readonly Dictionary<long, RefPair<long, decimal>> _orderInfo = new Dictionary<long, RefPair<long, decimal>>();
		
		private HttpClient _httpClient;
		private PusherClient _pusherClient;
		private DateTimeOffset? _lastTimeBalanceCheck;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="BitStampMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public BitStampMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			HeartbeatInterval = DefaultHeartbeatInterval;

			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.RemoveSupportedMessage(MessageTypes.Portfolio);
			this.RemoveSupportedMessage(MessageTypes.OrderReplace);

			this.AddSupportedMarketDataType(DataType.Ticks);
			this.AddSupportedMarketDataType(DataType.MarketDepth);
			//this.AddSupportedMarketDataType(DataType.Level1);
			this.AddSupportedMarketDataType(DataType.OrderLog);

			this.AddSupportedResultMessage(MessageTypes.SecurityLookup);
			this.AddSupportedResultMessage(MessageTypes.PortfolioLookup);
			this.AddSupportedResultMessage(MessageTypes.OrderStatus);
		}

		/// <inheritdoc />
		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

		/// <inheritdoc />
		public override TimeSpan GetHistoryStepSize(DataType dataType, out TimeSpan iterationInterval)
		{
			var step = base.GetHistoryStepSize(dataType, out iterationInterval);
			
			if (dataType == DataType.Ticks)
				step = TimeSpan.FromDays(1);

			return step;
		}

		private void SubscribePusherClient()
		{
			_pusherClient.Connected += SessionOnPusherConnected;
			_pusherClient.Disconnected += SessionOnPusherDisconnected;
			_pusherClient.Error += SessionOnPusherError;
			_pusherClient.NewOrderBook += SessionOnNewOrderBook;
			_pusherClient.NewOrderLog += SessionOnNewOrderLog;
			_pusherClient.NewTrade += SessionOnNewTrade;
		}

		private void UnsubscribePusherClient()
		{
			_pusherClient.Connected -= SessionOnPusherConnected;
			_pusherClient.Disconnected -= SessionOnPusherDisconnected;
			_pusherClient.Error -= SessionOnPusherError;
			_pusherClient.NewOrderBook -= SessionOnNewOrderBook;
			_pusherClient.NewOrderLog -= SessionOnNewOrderLog;
			_pusherClient.NewTrade -= SessionOnNewTrade;
		}

		/// <inheritdoc />
		public override string FeatureName => nameof(BitStamp);

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
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
						if (!AuthV2 && ClientId.IsEmpty())
							throw new InvalidOperationException(LocalizedStrings.Str3835);

						if (Key.IsEmpty())
							throw new InvalidOperationException(LocalizedStrings.Str3689);
						
						if (Secret.IsEmpty())
							throw new InvalidOperationException(LocalizedStrings.Str3690);
					}

#if !IGNORE_LICENSE
					var msg = "Crypto".ValidateLicense(component: GetType());
					if (!msg.IsEmpty())
					{
						msg = nameof(BitStamp).ValidateLicense(component: GetType());
						
						if (!msg.IsEmpty())
							throw new InvalidOperationException(msg);
					}
#endif

					if (_httpClient != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					if (_pusherClient != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_httpClient = new HttpClient(ClientId, Key, Secret, AuthV2) { Parent = this };

					_pusherClient = new PusherClient { Parent = this };
					SubscribePusherClient();
					_pusherClient.Connect();

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_httpClient == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					if (_pusherClient == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

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

					_pusherClient?.ProcessPing();

					//ProcessLevel1();
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