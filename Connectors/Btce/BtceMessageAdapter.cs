namespace StockSharp.Btce
{
	using System;

	using StockSharp.Btce.Native;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для BTC-e.
	/// </summary>
	public partial class BtceMessageAdapter : MessageAdapter<BtceSessionHolder>
	{
		private const string _boardCode = "BTCE";
		private bool _isSessionOwner;
		
		/// <summary>
		/// Создать <see cref="BtceMessageAdapter"/>.
		/// </summary>
		/// <param name="type">Тип адаптера.</param>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public BtceMessageAdapter(MessageAdapterTypes type, BtceSessionHolder sessionHolder)
			: base(type, sessionHolder)
		{
		}

		private BtceClient Session
		{
			get
			{
				var session = SessionHolder.Session;
				
				if (session == null)
					throw new InvalidOperationException(LocalizedStrings.Str2153);

				return session;
			}
		}

		/// <summary>
		/// Нужно ли отправлять в адаптер сообщение типа <see cref="TimeMessage"/>.
		/// </summary>
		protected override bool CanSendTimeMessage
		{
			get { return true; }
		}

		/// <summary>
		/// Добавить <see cref="Messages.Message"/> в выходную очередь <see cref="IMessageAdapter"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public override void SendOutMessage(Message message)
		{
			base.SendOutMessage(message);

			var connectMsg = message as ConnectMessage;

			if (connectMsg != null && connectMsg.Error == null)
			{
				switch (Type)
				{
					case MessageAdapterTypes.Transaction:
						SendInMessage(new OrderStatusMessage { TransactionId = TransactionIdGenerator.GetNextId() });
						break;
					case MessageAdapterTypes.MarketData:
						SendInMessage(new SecurityLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
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
					_subscribedLevel1.Clear();
					_subscribedDepths.Clear();
					_subscribedTicks.Clear();

					if (SessionHolder.Session == null)
					{
						_isSessionOwner = true;
						SessionHolder.Session = new BtceClient(SessionHolder.Key, SessionHolder.Secret);
					}

					if (Type == MessageAdapterTypes.Transaction)
					{
						_lastTickId = 0;
						_lastMyTradeId = 0;

						_orderInfo.Clear();

						_hasActiveOrders = true;
						_hasMyTrades = true;
						_requestOrderFirst = true;

						var reply = Session.GetInfo();

						SendOutMessage(new ConnectMessage());

						SendOutMessage(new PortfolioMessage
						{
							PortfolioName = GetPortfolioName(),
							State = reply.State.Rights.CanTrade ? PortfolioStates.Active : PortfolioStates.Blocked
						});

						ProcessFunds(reply.State.Funds);
					}
					else
						SendOutMessage(new ConnectMessage());

					break;
				}

				case MessageTypes.Disconnect:
				{
					SendOutMessage(new DisconnectMessage());

					if (_isSessionOwner)
					{
						_isSessionOwner = false;
						SessionHolder.Session.Dispose();
						SessionHolder.Session = null;
					}

					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					ProcessFunds(Session.GetInfo().State.Funds);
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
					switch (Type)
					{
						case MessageAdapterTypes.Transaction:
						{
							if (_hasActiveOrders || _hasMyTrades)
							{
								SendInMessage(new OrderStatusMessage { TransactionId = TransactionIdGenerator.GetNextId() });
								SendInMessage(new PortfolioLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });
							}
							
							break;
						}
						case MessageAdapterTypes.MarketData:
						{
							ProcessSubscriptions();
							break;
						}
						default:
							throw new ArgumentOutOfRangeException();
					}

					break;
				}
			}
		}
	}
}