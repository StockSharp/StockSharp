namespace StockSharp.Btce
{
	using System;

	using Ecng.Common;

	using StockSharp.Btce.Native;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для BTC-e.
	/// </summary>
	public partial class BtceMessageAdapter : MessageAdapter
	{
		private const string _boardCode = "BTCE";
		private BtceClient _client;
		
		/// <summary>
		/// Создать <see cref="BtceMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public BtceMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			HeartbeatInterval = TimeSpan.FromSeconds(1);

			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск инструментов.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup
		{
			get { return true; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск портфелей.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup
		{
			get { return true; }
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_subscribedLevel1.Clear();
					_subscribedDepths.Clear();
					_subscribedTicks.Clear();

					_lastTickId = 0;
					_lastMyTradeId = 0;

					_orderInfo.Clear();

					_hasActiveOrders = false;
					_hasMyTrades = false;
					_requestOrderFirst = false;

					if (_client != null)
					{
						try
						{
							_client.Dispose();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_client = null;
					}

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_client != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_client = new BtceClient(Key, Secret);

					SendOutMessage(new ConnectMessage());
					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_client == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					SendOutMessage(new DisconnectMessage());

					_client.Dispose();
					_client = null;

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
					_hasActiveOrders = true;
					_hasMyTrades = true;
					_requestOrderFirst = true;

					ProcessOrderStatus();
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
					if (_hasActiveOrders || _hasMyTrades)
					{
						ProcessOrderStatus();
						ProcessPortfolioLookup(null);
					}

					ProcessSubscriptions();

					break;
				}
			}
		}
	}
}