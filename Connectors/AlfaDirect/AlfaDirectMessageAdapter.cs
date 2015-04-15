namespace StockSharp.AlfaDirect
{
	using System;

	using Ecng.Common;
	using Ecng.Interop;

	using StockSharp.AlfaDirect.Native;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для AlfaDirect.
	/// </summary>
	public partial class AlfaDirectMessageAdapter : MessageAdapter
	{
		private AlfaWrapper _wrapper;

		/// <summary>
		/// Создать <see cref="AlfaDirectMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public AlfaDirectMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Platform = Platforms.x86;

			SecurityClassInfo.Add("FORTS", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.Forts.Code));
			SecurityClassInfo.Add("INDEX", RefTuple.Create(SecurityTypes.Index, ExchangeBoard.Micex.Code));
			SecurityClassInfo.Add("INDEX2", RefTuple.Create(SecurityTypes.Index, "INDEX"));
			SecurityClassInfo.Add("MICEX_SHR_T", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.Micex.Code));
			SecurityClassInfo.Add("RTS_STANDARD", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.Forts.Code));
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new AlfaOrderCondition();
		}

		private AlfaWrapper Wrapper
		{
			get { return _wrapper; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		public override bool SecurityLookupRequired
		{
			get { return IsMarketDataEnabled; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		public override bool PortfolioLookupRequired
		{
			get { return IsTransactionEnabled; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="OrderStatusMessage"/> для получения списка заявок и собственных сделок.
		/// </summary>
		public override bool OrderStatusRequired
		{
			get { return IsTransactionEnabled; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск портфелей.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup
		{
			get { return true; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск инструментов.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup
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
					_alfaIds.Clear();
					_localIds.Clear();

					if (_wrapper != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_wrapper = new AlfaWrapper(this);

					_wrapper.Connected += OnWrapperConnected;
					_wrapper.Disconnected += OnWrapperDisconnected;
					_wrapper.ConnectionError += OnConnectionError;
					_wrapper.Error += SendOutError;

					_wrapper.ProcessOrder += OnProcessOrders;
					_wrapper.ProcessOrderConfirmed += OnProcessOrderConfirmed;
					_wrapper.ProcessOrderFailed += OnProcessOrderFailed;
					_wrapper.ProcessPositions += OnProcessPositions;
					_wrapper.ProcessMyTrades += OnProcessMyTrades;

					_wrapper.ProcessNews += OnProcessNews;
					_wrapper.ProcessSecurities += OnProcessSecurities;
					_wrapper.ProcessLevel1 += OnProcessLevel1;
					_wrapper.ProcessQuotes += OnProcessQuotes;
					_wrapper.ProcessTrades += OnProcessTrades;
					_wrapper.ProcessCandles += OnProcessCandles;

					if (_wrapper.IsConnected)
						SendOutMessage(new ConnectMessage());
					else if (!Wrapper.IsConnecting)
						_wrapper.Connect(Login, Password.To<string>());

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_wrapper == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					_wrapper.StopExportOrders();
					_wrapper.StopExportPortfolios();
					_wrapper.StopExportMyTrades();

					_wrapper.Connected -= OnWrapperConnected;
					_wrapper.Disconnected -= OnWrapperDisconnected;
					_wrapper.ConnectionError -= OnConnectionError;

					_wrapper.ProcessOrder -= OnProcessOrders;
					_wrapper.ProcessOrderConfirmed -= OnProcessOrderConfirmed;
					_wrapper.ProcessOrderFailed -= OnProcessOrderFailed;
					_wrapper.ProcessPositions -= OnProcessPositions;
					_wrapper.ProcessMyTrades -= OnProcessMyTrades;

					_wrapper.ProcessNews -= OnProcessNews;
					_wrapper.ProcessSecurities += OnProcessSecurities;
					_wrapper.ProcessLevel1 -= OnProcessLevel1;
					_wrapper.ProcessQuotes -= OnProcessQuotes;
					_wrapper.ProcessTrades -= OnProcessTrades;
					_wrapper.ProcessCandles -= OnProcessCandles;

					_wrapper.Dispose();
					_wrapper = null;
					
					SendOutMessage(new DisconnectMessage());

					break;
				}

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					// чтобы не было дедлока, RegisterOrder должен использовать только асинхронные
					// вызовы AlfaDirect, как CreateLimitOrder(... timeout=-1)
					lock (_localIds.SyncRoot)
					{
						var alfaTransactionId = Wrapper.RegisterOrder(regMsg);
						_localIds.Add(regMsg.TransactionId, alfaTransactionId);
					}

					break;
				}

				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					if (cancelMsg.OrderId == 0)
						throw new InvalidOperationException(LocalizedStrings.Str2252Params.Put(cancelMsg.OrderTransactionId));

					Wrapper.CancelOrder(cancelMsg.OrderId);
					break;
				}

				case MessageTypes.OrderGroupCancel:
				{
					var groupMsg = (OrderGroupCancelMessage)message;
					Wrapper.CancelOrders(groupMsg.IsStop, groupMsg.PortfolioName, groupMsg.Side, groupMsg.SecurityId, groupMsg.SecurityType);
					break;
				}

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;

					if (pfMsg.IsSubscribe)
						Wrapper.StartExportPortfolios();
					else
						this.AddWarningLog("ignore portfolios unsubscribe");

					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;
					Wrapper.LookupSecurities(lookupMsg.TransactionId);
					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					var lookupMsg = (PortfolioLookupMessage)message;
					Wrapper.LookupPortfolios(lookupMsg.TransactionId);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					Wrapper.LookupOrders();
					break;
				}
			}
		}

		private void OnWrapperDisconnected()
		{
			this.AddInfoLog(LocalizedStrings.Str2254);
			SendOutMessage(new DisconnectMessage());
		}

		private void OnWrapperConnected()
		{
			this.AddInfoLog(LocalizedStrings.Str2255);
			SendOutMessage(new ConnectMessage());
		}

		private void OnConnectionError(Exception ex)
		{
			this.AddInfoLog(LocalizedStrings.Str3458Params.Put(ex.Message));
			SendOutMessage(new ConnectMessage { Error = ex });
		}
	}
}