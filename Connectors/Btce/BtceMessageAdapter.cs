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
			IsTransactionEnabled = true;
			IsMarketDataEnabled = true;
		}

		/// <summary>
		/// Нужно ли отправлять в адаптер сообщение типа <see cref="TimeMessage"/>.
		/// </summary>
		protected override bool CanSendTimeMessage
		{
			get { return true; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="OrderStatusMessage"/> для получения списка заявок и собственных сделок.
		/// </summary>
		public override bool OrderStatusRequired
		{
			get { return true; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		public override bool SecurityLookupRequired
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
				case MessageTypes.Connect:
				{
					if (_client != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_subscribedLevel1.Clear();
					_subscribedDepths.Clear();
					_subscribedTicks.Clear();

					_lastTickId = 0;
					_lastMyTradeId = 0;

					_orderInfo.Clear();

					_hasActiveOrders = true;
					_hasMyTrades = true;
					_requestOrderFirst = true;

					_client = new BtceClient(Key, Secret);

					var reply = _client.GetInfo();

					SendOutMessage(new ConnectMessage());

					SendOutMessage(new PortfolioMessage
					{
						PortfolioName = GetPortfolioName(),
						State = reply.State.Rights.CanTrade ? PortfolioStates.Active : PortfolioStates.Blocked
					});

					ProcessFunds(reply.State.Funds);

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
					ProcessFunds(_client.GetInfo().State.Funds);
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					ProcessSecurityLookup((SecurityLookupMessage)message);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					ProcessOrderStatusMessage();
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
						SendInMessage(new OrderStatusMessage { TransactionId = TransactionIdGenerator.GetNextId() });
						SendInMessage(new PortfolioLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });
					}

					ProcessSubscriptions();

					break;
				}
			}
		}
	}
}