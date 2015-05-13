namespace StockSharp.Oanda
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Oanda.Native;

	/// <summary>
	/// Адаптер сообщений для OANDA через протокол REST.
	/// </summary>
	public partial class OandaMessageAdapter : MessageAdapter
	{
		private OandaRestClient _restClient;
		private OandaStreamingClient _streamigClient;

		/// <summary>
		/// Создать <see cref="OandaMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public OandaMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			HeartbeatInterval = TimeSpan.FromSeconds(60);

			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new OandaOrderCondition();
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

		private void StreamingClientDispose()
		{
			_streamigClient.NewError -= SendOutError;
			_streamigClient.NewTransaction -= SessionOnNewTransaction;
			_streamigClient.NewPrice -= SessionOnNewPrice;

			_streamigClient.Dispose();
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
					_accountIds.Clear();

					if (_streamigClient != null)
					{
						try
						{
							StreamingClientDispose();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_streamigClient = null;
					}

					_restClient = null;

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_restClient != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					if (_streamigClient != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_restClient = new OandaRestClient(Server, Token);
					
					_streamigClient = new OandaStreamingClient(Server, Token, GetAccountId);
					_streamigClient.NewError += SendOutError;
					_streamigClient.NewTransaction += SessionOnNewTransaction;
					_streamigClient.NewPrice += SessionOnNewPrice;

					SendOutMessage(new ConnectMessage());

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_restClient == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					if (_streamigClient == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					StreamingClientDispose();
					_streamigClient = null;

					_restClient = null;

					SendOutMessage(new DisconnectMessage());

					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					ProcessPortfolioLookupMessage((PortfolioLookupMessage)message);
					break;
				}

				case MessageTypes.Portfolio:
				{
					ProcessPortfolioMessage((PortfolioMessage)message);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					ProcessOrderStatusMessage();
					break;
				}

				case MessageTypes.Time:
				{
					//var timeMsg = (TimeMessage)message;
					//Session.RequestHeartbeat(new HeartbeatRequest(timeMsg.TransactionId), () => { }, CreateErrorHandler("RequestHeartbeat"));
					break;
				}

				case MessageTypes.OrderRegister:
				{
					ProcessOrderRegisterMessage((OrderRegisterMessage)message);
					break;
				}

				case MessageTypes.OrderCancel:
				{
					ProcessCancelMessage((OrderCancelMessage)message);
					break;
				}

				case MessageTypes.OrderReplace:
				{
					ProcessOrderReplaceMessage((OrderReplaceMessage)message);
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					ProcessSecurityLookupMessage((SecurityLookupMessage)message);
					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;
				}
			}
		}
	}
}