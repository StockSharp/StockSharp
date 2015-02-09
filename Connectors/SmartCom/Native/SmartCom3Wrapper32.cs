namespace StockSharp.SmartCom.Native
{
	using System;

	using Ecng.Common;

	using SmartCOM3_32;

	/// <summary>
	/// Обертка над SmartCOM 2.X API (32 бита).
	/// </summary>
	[CLSCompliant(false)]
	public class SmartCom3Wrapper32 : SmartComWrapper<StServerClass>
	{
		/// <summary>
		/// Создать <see cref="SmartCom3Wrapper32"/>.
		/// </summary>
		public SmartCom3Wrapper32()
		{
		}

		/// <summary>
		/// Настройки клиентской части.
		/// </summary>
		public string ClientSettings { get; set; }

		/// <summary>
		/// Настройки серверной части.
		/// </summary>
		public string ServerSettings { get; set; }

		/// <summary>
		/// Поддерживается ли отмена всех заявок.
		/// </summary>
		public override bool IsSupportCancelAllOrders
		{
			get { return false; }
		}

		/// <summary>
		/// Версия обертки.
		/// </summary>
		public override SmartComVersions Version
		{
			get { return SmartComVersions.V3; }
		}

		/// <summary>
		/// Подключиться к SmartCOM.
		/// </summary>
		/// <param name="host">Адрес сервера.</param>
		/// <param name="port">Порт сервера.</param>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		protected override void OnConnect(string host, short port, string login, string password)
		{
			var server = SafeGetServer();

			if (!ClientSettings.IsEmpty())
				server.ConfigureClient(ClientSettings);

			if (!ServerSettings.IsEmpty())
				server.ConfigureServer(ServerSettings);

			server.connect(host, port, login, password);
		}

		/// <summary>
		/// Отключиться от SmartCOM.
		/// </summary>
		protected override void OnDisconnect()
		{
			SafeGetServer().disconnect();
		}

		/// <summary>
		/// Запросить все доступные инструменты.
		/// </summary>
		public override void LookupSecurities()
		{
			SafeGetServer().GetSymbols();
		}

		/// <summary>
		/// Запросить все доступные портфель.
		/// </summary>
		public override void LookupPortfolios()
		{
			SafeGetServer().GetPrortfolioList();
		}

		/// <summary>
		/// Зарегистрировать заявку.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="action">Направление действия.</param>
		/// <param name="type">Тип заявки.</param>
		/// <param name="validity">Время действия.</param>
		/// <param name="price">Цена.</param>
		/// <param name="volume">Объем.</param>
		/// <param name="stopPrice">Стоп цена (если регистрируется стоп-заявка).</param>
		/// <param name="transactionId">Уникальный номер транзакции.</param>
		public override void RegisterOrder(string portfolioName, string securityId, SmartOrderAction action, SmartOrderType type, SmartOrderValidity validity, double price, int volume, double stopPrice, int transactionId)
		{
			StOrder_Action smartAction;

			switch (action)
			{
				case SmartOrderAction.Buy:
					smartAction = StOrder_Action.StOrder_Action_Buy;
					break;
				case SmartOrderAction.Sell:
					smartAction = StOrder_Action.StOrder_Action_Sell;
					break;
				case SmartOrderAction.Short:
					smartAction = StOrder_Action.StOrder_Action_Short;
					break;
				case SmartOrderAction.Cover:
					smartAction = StOrder_Action.StOrder_Action_Cover;
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}

			StOrder_Type smartType;

			switch (type)
			{
				case SmartOrderType.Stop:
					smartType = StOrder_Type.StOrder_Type_Stop;
					break;
				case SmartOrderType.StopLimit:
					smartType = StOrder_Type.StOrder_Type_StopLimit;
					break;
				case SmartOrderType.Limit:
					smartType = StOrder_Type.StOrder_Type_Limit;
					break;
				case SmartOrderType.Market:
					smartType = StOrder_Type.StOrder_Type_Market;
					break;
				default:
					throw new ArgumentOutOfRangeException("type");
			}

			StOrder_Validity smartValidity;

			switch (validity)
			{
				case SmartOrderValidity.Day:
					smartValidity = StOrder_Validity.StOrder_Validity_Day;
					break;
				case SmartOrderValidity.Gtc:
					smartValidity = StOrder_Validity.StOrder_Validity_Gtc;
					break;
				default:
					throw new ArgumentOutOfRangeException("validity");
			}

			SafeGetServer().PlaceOrder(portfolioName, securityId, smartAction, smartType, smartValidity, price, volume, stopPrice, transactionId);
		}

		/// <summary>
		/// Перерегистрировать заявку.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля.</param>
		/// <param name="newPrice">Новая цена.</param>
		/// <param name="smartId">SmartCOM идентификатор заявки.</param>
		public override void ReRegisterOrder(string portfolioName, double newPrice, string smartId)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException("portfolioName");

			SafeGetServer().MoveOrder(portfolioName, smartId, newPrice);
		}

		/// <summary>
		/// Отменить заявку.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="smartId">SmartCOM идентификатор заявки.</param>
		public override void CancelOrder(string portfolioName, string securityId, string smartId)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException("portfolioName");

			if (securityId.IsEmpty())
				throw new ArgumentNullException("securityId");

			if (smartId.IsEmpty())
				throw new ArgumentNullException("smartId");

			SafeGetServer().CancelOrder(portfolioName, securityId, smartId);
		}

		/// <summary>
		/// Отменить все активные заявки.
		/// </summary>
		public override void CancelAllOrders()
		{
			throw new NotSupportedException();
			//SafeGetServer().CancelAllOrders());
		}

		/// <summary>
		/// Начать получать новую информацию по инструменту.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо начать получать новую информацию.</param>
		public override void SubscribeSecurity(string securityId)
		{
			if (securityId.IsEmpty())
				throw new ArgumentNullException("securityId");

			SafeGetServer().ListenQuotes(securityId);
		}

		/// <summary>
		/// Остановить получение новой информации.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо остановить получение новой информации.</param>
		public override void UnSubscribeSecurity(string securityId)
		{
			if (securityId.IsEmpty())
				throw new ArgumentNullException("securityId");

			//if (!IsConnected)
			//	return;

			SafeGetServer().CancelQuotes(securityId);
		}

		/// <summary>
		/// Начать получать котировки (стакан) по инструменту.
		/// Значение котировок можно получить через событие <see cref="ISmartComWrapper.QuoteChanged"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо начать получать котировки.</param>
		public override void SubscribeMarketDepth(string securityId)
		{
			if (securityId.IsEmpty())
				throw new ArgumentNullException("securityId");

			SafeGetServer().ListenBidAsks(securityId);
		}

		/// <summary>
		/// Остановить получение котировок по инструменту.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо остановить получение котировок.</param>
		public override void UnSubscribeMarketDepth(string securityId)
		{
			if (securityId.IsEmpty())
				throw new ArgumentNullException("securityId");

			//if (!IsConnected)
			//	return;

			SafeGetServer().CancelBidAsks(securityId);
		}

		/// <summary>
		/// Начать получать сделки (тиковые данные) по инструменту. Новые сделки будут приходить через событие <see cref="ISmartComWrapper.NewTrade"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо начать получать сделки.</param>
		public override void SubscribeTrades(string securityId)
		{
			if (securityId.IsEmpty())
				throw new ArgumentNullException("securityId");

			// http://www.itinvest.ru/forum/index.php?showtopic=63071
			//_server.GetBars(security.Id, StBarInterval.StBarInterval_Tick, DateTime.MinValue, int.MaxValue);
			SafeGetServer().ListenTicks(securityId);
		}

		/// <summary>
		/// Остановить получение сделок (тиковые данные) по инструменту.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо остановить получение сделок.</param>
		public override void UnSubscribeTrades(string securityId)
		{
			if (securityId.IsEmpty())
				throw new ArgumentNullException("securityId");

			//if (!IsConnected)
			//	return;

			SafeGetServer().CancelTicks(securityId);
		}

		/// <summary>
		/// Начать получать новую информацию по портфелю.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля, по которому необходимо начать получать новую информацию.</param>
		public override void SubscribePortfolio(string portfolioName)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException("portfolioName");

			SafeGetServer().ListenPortfolio(portfolioName);
		}

		/// <summary>
		/// Остановить получение новой информации по портфелю.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля, по которому необходимо остановить получение новой информации.</param>
		public override void UnSubscribePortfolio(string portfolioName)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException("portfolioName");

			//if (!IsConnected)
			//	return;

			SafeGetServer().CancelPortfolio(portfolioName);
		}

		/// <summary>
		/// Начать получать исторические тиковые сделки от сервера SmartCOM через событие <see cref="ISmartComWrapper.NewHistoryTrade"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо начать получать исторические сделки.</param>
		/// <param name="from">Временная точка отсчета.</param>
		/// <param name="count">Количество сделок.</param>
		public override void RequestHistoryTrades(string securityId, DateTime from, int count)
		{
			if (securityId.IsEmpty())
				throw new ArgumentNullException("securityId");

			SafeGetServer().GetTrades(securityId, from, count);
		}

		/// <summary>
		/// Начать получать исторические временные свечи от сервера SmartCOM через событие <see cref="ISmartComWrapper.NewBar"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо начать получать исторические свечи.</param>
		/// <param name="timeFrame">Тайм-фрейм.</param>
		/// <param name="from">Временная точка отсчета.</param>
		/// <param name="count">Количество свечек.</param>
		public override void RequestHistoryBars(string securityId, SmartComTimeFrames timeFrame, DateTime from, int count)
		{
			if (securityId.IsEmpty())
				throw new ArgumentNullException("securityId");

			StBarInterval interval;

			switch (timeFrame.Interval)
			{
				case SmartBarInterval.Tick:
					interval = StBarInterval.StBarInterval_Tick;
					break;
				case SmartBarInterval.Min1:
					interval = StBarInterval.StBarInterval_1Min;
					break;
				case SmartBarInterval.Min5:
					interval = StBarInterval.StBarInterval_5Min;
					break;
				case SmartBarInterval.Min10:
					interval = StBarInterval.StBarInterval_10Min;
					break;
				case SmartBarInterval.Min15:
					interval = StBarInterval.StBarInterval_15Min;
					break;
				case SmartBarInterval.Min30:
					interval = StBarInterval.StBarInterval_30Min;
					break;
				case SmartBarInterval.Min60:
					interval = StBarInterval.StBarInterval_60Min;
					break;
				case SmartBarInterval.Hour2:
					interval = StBarInterval.StBarInterval_2Hour;
					break;
				case SmartBarInterval.Hour4:
					interval = StBarInterval.StBarInterval_4Hour;
					break;
				case SmartBarInterval.Day:
					interval = StBarInterval.StBarInterval_Day;
					break;
				case SmartBarInterval.Week:
					interval = StBarInterval.StBarInterval_Week;
					break;
				case SmartBarInterval.Month:
					interval = StBarInterval.StBarInterval_Month;
					break;
				case SmartBarInterval.Quarter:
					interval = StBarInterval.StBarInterval_Quarter;
					break;
				case SmartBarInterval.Year:
					interval = StBarInterval.StBarInterval_Year;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			SafeGetServer().GetBars(securityId, interval, from, count);
		}

		/// <summary>
		/// Подписаться на события.
		/// </summary>
		protected override void SubscribeEvents()
		{
			Server.AddBar += OnAddBar;
			Server.AddSymbol += OnAddSecurity;
			Server.AddTick += OnAddTrade;
			Server.AddTickHistory += OnAddTradeHistory;
			Server.AddTrade += OnAddMyTrade;
			Server.AddPortfolio += OnAddPortfolio;
			Server.UpdateBidAsk += OnUpdateQuotes;
			Server.UpdateOrder += OnUpdateOrder;
			Server.UpdatePosition += OnUpdatePosition;
			Server.UpdateQuote += OnUpdateSecurity;
			Server.OrderCancelFailed += OnOrderCancelFailed;
			Server.OrderCancelSucceeded += OnOrderCancelSucceeded;
			Server.OrderMoveFailed += OnOrderMoveFailed;
			Server.OrderMoveSucceeded += OnOrderMoveSucceeded;
			Server.OrderFailed += OnOrderFailed;
			Server.OrderSucceeded += OnOrderSucceded;
			Server.SetPortfolio += OnUpdatePortfolio;
			Server.Connected += OnConnected;
			Server.Disconnected += OnDisconnected;
		}

		private static SmartOrderAction ToWrapper(StOrder_Action smartAction)
		{
			switch (smartAction)
			{
				case StOrder_Action.StOrder_Action_Buy:
					return SmartOrderAction.Buy;
				case StOrder_Action.StOrder_Action_Sell:
					return SmartOrderAction.Sell;
				case StOrder_Action.StOrder_Action_Short:
					return SmartOrderAction.Short;
				case StOrder_Action.StOrder_Action_Cover:
					return SmartOrderAction.Cover;
				default:
					throw new ArgumentOutOfRangeException("smartAction");
			}
		}

		private void OnUpdateOrder(string portfolio, string symbol, StOrder_State smartState, StOrder_Action smartAction, StOrder_Type smartType, StOrder_Validity smartValidity, double price, double amount, double stop, double filled, DateTime datetime, string orderid, string orderno, int statusMask, int cookie)
		{
			SmartOrderState state;

			switch (smartState)
			{
				case StOrder_State.StOrder_State_ContragentReject:
					state = SmartOrderState.ContragentReject;
					break;
				case StOrder_State.StOrder_State_Submited:
					state = SmartOrderState.Submited;
					break;
				case StOrder_State.StOrder_State_Pending:
					state = SmartOrderState.Pending;
					break;
				case StOrder_State.StOrder_State_Open:
					state = SmartOrderState.Open;
					break;
				case StOrder_State.StOrder_State_Expired:
					state = SmartOrderState.Expired;
					break;
				case StOrder_State.StOrder_State_Cancel:
					state = SmartOrderState.Cancel;
					break;
				case StOrder_State.StOrder_State_Filled:
					state = SmartOrderState.Filled;
					break;
				case StOrder_State.StOrder_State_Partial:
					state = SmartOrderState.Partial;
					break;
				case StOrder_State.StOrder_State_ContragentCancel:
					state = SmartOrderState.ContragentReject;
					break;
				case StOrder_State.StOrder_State_SystemReject:
					state = SmartOrderState.SystemReject;
					break;
				case StOrder_State.StOrder_State_SystemCancel:
					state = SmartOrderState.SystemCancel;
					break;
				default:
					throw new ArgumentOutOfRangeException("smartState");
			}

			SmartOrderType type;

			switch (smartType)
			{
				case StOrder_Type.StOrder_Type_Market:
					type = SmartOrderType.Market;
					break;
				case StOrder_Type.StOrder_Type_Limit:
					type = SmartOrderType.Limit;
					break;
				case StOrder_Type.StOrder_Type_Stop:
					type = SmartOrderType.Stop;
					break;
				case StOrder_Type.StOrder_Type_StopLimit:
					type = SmartOrderType.StopLimit;
					break;
				default:
					throw new ArgumentOutOfRangeException("smartType");
			}

			SmartOrderValidity validity;

			switch (smartValidity)
			{
				case StOrder_Validity.StOrder_Validity_Day:
					validity = SmartOrderValidity.Day;
					break;
				case StOrder_Validity.StOrder_Validity_Gtc:
					validity = SmartOrderValidity.Gtc;
					break;
				default:
					throw new ArgumentOutOfRangeException("smartValidity");
			}

			OnUpdateOrder(portfolio, symbol, state, ToWrapper(smartAction), type, validity, price, amount, stop, filled, datetime, orderid, orderno, statusMask, cookie);
		}

		private void OnAddPortfolio(int row, int nrows, string portfolioname, string portfolioexch, StPortfolioStatus smartStatus)
		{
			SmartPortfolioStatus status;

			switch (smartStatus)
			{
				case StPortfolioStatus.StPortfolioStatus_Broker:
					status = SmartPortfolioStatus.Broker;
					break;
				case StPortfolioStatus.StPortfolioStatus_TrustedManagement:
					status = SmartPortfolioStatus.TrustedManagement;
					break;
				case StPortfolioStatus.StPortfolioStatus_ReadOnly:
					status = SmartPortfolioStatus.ReadOnly;
					break;
				case StPortfolioStatus.StPortfolioStatus_Blocked:
					status = SmartPortfolioStatus.Blocked;
					break;
				case StPortfolioStatus.StPortfolioStatus_Restricted:
					status = SmartPortfolioStatus.Restricted;
					break;
				case StPortfolioStatus.StPortfolioStatus_AutoRestricted:
					status = SmartPortfolioStatus.AutoRestricted;
					break;
				case StPortfolioStatus.StPortfolioStatus_OrderNotSigned:
					status = SmartPortfolioStatus.OrderNotSigned;
					break;
				default:
					throw new ArgumentOutOfRangeException("smartStatus");
			}

			OnAddPortfolio(row, nrows, portfolioname, portfolioexch, status);
		}

		private void OnAddTradeHistory(int row, int nrows, string symbol, DateTime datetime, double price, double volume, string tradeno, StOrder_Action smartAction)
		{
			OnAddTradeHistory(row, nrows, symbol, datetime, price, volume, tradeno, ToWrapper(smartAction));
		}

		private void OnAddTrade(string symbol, DateTime datetime, double price, double volume, string tradeno, StOrder_Action smartAction)
		{
			OnAddTrade(symbol, datetime, price, volume, tradeno, ToWrapper(smartAction));
		}

		private void OnAddBar(int row, int nrows, string symbol, StBarInterval smartInterval, DateTime datetime, double open, double high, double low, double close, double volume, double openInt)
		{
			SmartBarInterval interval;

			switch (smartInterval)
			{
				case StBarInterval.StBarInterval_Tick:
					interval = SmartBarInterval.Tick;
					break;
				case StBarInterval.StBarInterval_1Min:
					interval = SmartBarInterval.Min1;
					break;
				case StBarInterval.StBarInterval_5Min:
					interval = SmartBarInterval.Min5;
					break;
				case StBarInterval.StBarInterval_10Min:
					interval = SmartBarInterval.Min10;
					break;
				case StBarInterval.StBarInterval_15Min:
					interval = SmartBarInterval.Min15;
					break;
				case StBarInterval.StBarInterval_30Min:
					interval = SmartBarInterval.Min30;
					break;
				case StBarInterval.StBarInterval_60Min:
					interval = SmartBarInterval.Min60;
					break;
				case StBarInterval.StBarInterval_2Hour:
					interval = SmartBarInterval.Hour2;
					break;
				case StBarInterval.StBarInterval_4Hour:
					interval = SmartBarInterval.Hour4;
					break;
				case StBarInterval.StBarInterval_Day:
					interval = SmartBarInterval.Day;
					break;
				case StBarInterval.StBarInterval_Week:
					interval = SmartBarInterval.Week;
					break;
				case StBarInterval.StBarInterval_Month:
					interval = SmartBarInterval.Month;
					break;
				case StBarInterval.StBarInterval_Quarter:
					interval = SmartBarInterval.Quarter;
					break;
				case StBarInterval.StBarInterval_Year:
					interval = SmartBarInterval.Year;
					break;
				default:
					throw new ArgumentOutOfRangeException("smartInterval");
			}

			OnAddBar(row, nrows, symbol, interval, datetime, open, high, low, close, volume, openInt);
		}

		/// <summary>
		/// Отписаться от событий.
		/// </summary>
		protected override void UnSubscribeEvents()
		{
			Server.AddBar -= OnAddBar;
			Server.AddSymbol -= OnAddSecurity;
			Server.AddTick -= OnAddTrade;
			Server.AddTickHistory -= OnAddTradeHistory;
			Server.AddTrade -= OnAddMyTrade;
			Server.AddPortfolio -= OnAddPortfolio;
			Server.UpdateBidAsk -= OnUpdateQuotes;
			Server.UpdateOrder -= OnUpdateOrder;
			Server.UpdatePosition -= OnUpdatePosition;
			Server.UpdateQuote -= OnUpdateSecurity;
			Server.OrderCancelFailed -= OnOrderCancelFailed;
			Server.OrderCancelSucceeded -= OnOrderCancelSucceeded;
			Server.OrderMoveFailed -= OnOrderMoveFailed;
			Server.OrderMoveSucceeded -= OnOrderMoveSucceeded;
			Server.OrderFailed -= OnOrderFailed;
			Server.OrderSucceeded -= OnOrderSucceded;
			Server.SetPortfolio -= OnUpdatePortfolio;
			Server.Connected -= OnConnected;
			Server.Disconnected -= OnDisconnected;
		}
	}
}