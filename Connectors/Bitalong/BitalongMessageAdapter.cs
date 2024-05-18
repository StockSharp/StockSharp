namespace StockSharp.Bitalong
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Bitalong.Native;

	[OrderCondition(typeof(BitalongOrderCondition))]
	public partial class BitalongMessageAdapter : MessageAdapter
	{
		private HttpClient _httpClient;
		private DateTimeOffset? _lastTimeBalanceCheck;

		/// <summary>
		/// Initializes a new instance of the <see cref="BitalongMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public BitalongMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			HeartbeatInterval = TimeSpan.FromSeconds(1);

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
		public override string AssociatedBoard => BoardCodes.Bitalong;

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

					_orderInfo.Clear();
					_lastTimeBalanceCheck = null;

					_orderBookSubscriptions.Clear();
					_tradesSubscriptions.Clear();
					_level1Subscriptions.Clear();

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

					_httpClient = new HttpClient(Address, Key, Secret) { Parent = this };

					SendOutMessage(new ConnectMessage());
					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_httpClient == null)
						throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

					_httpClient.Dispose();
					_httpClient = null;

					SendOutMessage(new DisconnectMessage());
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

					ProcessSubscriptions();

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
	}
}