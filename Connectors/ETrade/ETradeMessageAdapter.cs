namespace StockSharp.ETrade
{
	using System;

	using Ecng.Common;

	using StockSharp.ETrade.Native;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для ETrade.
	/// </summary>
	public partial class ETradeMessageAdapter : MessageAdapter
	{
		private ETradeClient _client;

		/// <summary>
		/// Создать <see cref="ETradeMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public ETradeMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			CreateAssociatedSecurity = true;

			this.AddMarketDataSupport();
			this.AddTransactionalSupport();

			this.RemoveSupportedMessage(MessageTypes.PortfolioLookup);
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		public override bool SecurityLookupRequired
		{
			get { return false; }
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new ETradeOrderCondition();
		}

		private void DisposeClient()
		{
			_client.ConnectionStateChanged -= ClientOnConnectionStateChanged;
			_client.ConnectionError -= ClientOnConnectionError;
			_client.Error -= SendOutError;

			_client.OrderRegisterResult -= ClientOnOrderRegisterResult;
			_client.OrderReRegisterResult -= ClientOnOrderRegisterResult;
			_client.OrderCancelResult -= ClientOnOrderCancelResult;
			_client.AccountsData -= ClientOnAccountsData;
			_client.PositionsData -= ClientOnPositionsData;
			_client.OrdersData -= ClientOnOrdersData;

			_client.ProductLookupResult -= ClientOnProductLookupResult;

			_client.Disconnect();
		}

		/// <summary>
		/// Отправить входящее сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					if (_client != null)
					{
						try
						{
							DisposeClient();
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

					_client = new ETradeClient
					{
						ConsumerKey = ConsumerKey,
						ConsumerSecret = ConsumerSecret,
						AccessToken = AccessToken,
						Sandbox = Sandbox,
						VerificationCode = VerificationCode,
					};

					_client.ConnectionStateChanged += ClientOnConnectionStateChanged;
					_client.ConnectionError += ClientOnConnectionError;
					_client.Error += SendOutError;

					_client.OrderRegisterResult += ClientOnOrderRegisterResult;
					_client.OrderReRegisterResult += ClientOnOrderRegisterResult;
					_client.OrderCancelResult += ClientOnOrderCancelResult;
					_client.AccountsData += ClientOnAccountsData;
					_client.PositionsData += ClientOnPositionsData;
					_client.OrdersData += ClientOnOrdersData;

					_client.ProductLookupResult += ClientOnProductLookupResult;

					_client.Parent = this;
					_client.Connect();

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_client == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					DisposeClient();
					_client = null;

					break;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;
					_client.LookupSecurities(lookupMsg.SecurityId.SecurityCode, lookupMsg.TransactionId);
					break;
				}

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					_client.RegisterOrder(
						regMsg.PortfolioName,
						regMsg.SecurityId.SecurityCode,
						regMsg.Side,
						regMsg.Price,
						regMsg.Volume,
						regMsg.TransactionId,
						regMsg.TimeInForce == TimeInForce.MatchOrCancel,
						regMsg.TillDate,
						regMsg.OrderType,
						(ETradeOrderCondition)regMsg.Condition);

					break;
				}

				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;

					if (replaceMsg.OldOrderId == null)
						throw new InvalidOperationException(LocalizedStrings.Str2252Params.Put(replaceMsg.OldTransactionId));

					SaveOrder(replaceMsg.OldTransactionId, replaceMsg.OldOrderId.Value);

					_client.ReRegisterOrder(
						replaceMsg.OldOrderId.Value,
						replaceMsg.PortfolioName,
						replaceMsg.Price,
						replaceMsg.Volume,
						replaceMsg.TransactionId,
						replaceMsg.TimeInForce == TimeInForce.MatchOrCancel,
						replaceMsg.TillDate,
						replaceMsg.OrderType,
						(ETradeOrderCondition)replaceMsg.Condition);

					break;
				}

				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					if (cancelMsg.OrderId == null)
						throw new InvalidOperationException(LocalizedStrings.Str2252Params.Put(cancelMsg.OrderTransactionId));

					_client.CancelOrder(cancelMsg.TransactionId, cancelMsg.OrderId.Value, cancelMsg.PortfolioName);
					break;
				}
			}
		}

		/// <summary>Коллбэк изменения статуса соединения.</summary>
		private void ClientOnConnectionStateChanged()
		{
			this.AddInfoLog(LocalizedStrings.Str3364Params, _client.IsConnected ? LocalizedStrings.Str3365 : LocalizedStrings.Str3366);
			SendOutMessage(_client.IsConnected ? (Message)new ConnectMessage() : new DisconnectMessage());
		}

		/// <summary>Коллбэк ошибки подключения.</summary>
		/// <param name="ex">Ошибка подключения.</param>
		private void ClientOnConnectionError(Exception ex)
		{
			this.AddInfoLog(LocalizedStrings.Str3458Params.Put(ex.Message));
			SendOutMessage(new ConnectMessage { Error = ex });
		}

		/// <summary>
		/// Установить свой метод авторизации (по-умолчанию запускается браузер).
		/// </summary>
		/// <param name="method">Метод, принимающий в качестве параметра URL, по которому происходит авторизация на сайте ETrade.</param>
		public void SetCustomAuthorizationMethod(Action<string> method)
		{
			_client.SetCustomAuthorizationMethod(method);
		}
	}
}
