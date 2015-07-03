namespace StockSharp.SmartCom.Native
{
	using System;

	using Ecng.Common;
	using Ecng.Interop;

	using StockSharp.Algo;
	using StockSharp.Messages;

	using StockSharp.Localization;

	/// <summary>
	/// Базовая обертка над SmartCOM API.
	/// </summary>
	/// <typeparam name="TServer">Тип ссылки на SmartCOM API.</typeparam>
	public abstract class SmartComWrapper<TServer> : ISmartComWrapper
		where TServer : class, new()
	{
		private bool? _errorWhileDisconnect;

		/// <summary>
		/// Инициализировать <see cref="SmartComWrapper{TServer}"/>.
		/// </summary>
		protected SmartComWrapper()
		{
		}

		///// <summary>
		///// Проверить соединение.
		///// </summary>
		//public bool IsConnected { get; private set; }

		/// <summary>
		/// Поддерживается ли отмена всех заявок.
		/// </summary>
		public abstract bool IsSupportCancelAllOrders { get; }

		/// <summary>
		/// Версия обертки.
		/// </summary>
		public abstract SmartComVersions Version { get; }

		/// <summary>
		/// Ссылка на SmartCOM API.
		/// </summary>
		protected TServer Server { get; private set; }

		/// <summary>
		/// Событие о появлении инструмента.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Номер инструмента в справочнике.</description>
		/// </item>
		/// <item>
		/// <description>Всего инструментов в справочнике.</description>
		/// </item>
		/// <item>
		/// <description>Код ЦБ из таблицы котировок SmartTrade.</description>
		/// </item>
		/// <item>
		/// <description>Краткое наименование.</description>
		/// </item>
		/// <item>
		/// <description>Полное наименование.</description>
		/// </item>
		/// <item>
		/// <description>Код типа из справочника SmartTrade.</description>
		/// </item>
		/// <item>
		/// <description>Точность цены.</description>
		/// </item>
		/// <item>
		/// <description>Размер лота ценных бумаг.</description>
		/// </item>
		/// <item>
		/// <description>Цена минимального шага.</description>
		/// </item>
		/// <item>
		/// <description>Минимальный шаг цены.</description>
		/// </item>
		/// <item>
		/// <description>ISIN.</description>
		/// </item>
		/// <item>
		/// <description>Наименование площадки.</description>
		/// </item>
		/// <item>
		/// <description>Дата экспирации.</description>
		/// </item>
		/// <item>
		/// <description>Дней до экспирации.</description>
		/// </item>
		/// <item>
		/// <description>Страйк.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<int, int, string, string, string, string, int, int, decimal?, decimal?, string, string, DateTime?, decimal?, decimal?> NewSecurity;

		/// <summary>
		/// Событие об изменении инструмента.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Код ЦБ из таблицы котировок SmartTrade.</description>
		/// </item>
		/// <item>
		/// <description>Информация о последней сделке.</description>
		/// </item>
		/// <item>
		/// <description>Цена первой сделки в текущей сессии.</description>
		/// </item>
		/// <item>
		/// <description>Максимальная цена сделки в текущей сессии.</description>
		/// </item>
		/// <item>
		/// <description>Минимальная цена сделки в текущей сессии.</description>
		/// </item>
		/// <item>
		/// <description>Цена последней сделки предыдущей торговой сессии.</description>
		/// </item>
		/// <item>
		/// <description>Объём по ЦБ за текущую торговую сессию.</description>
		/// </item>
		/// <item>
		/// <description>Информация о спросе.</description>
		/// </item>
		/// <item>
		/// <description>Информация о предложении.</description>
		/// </item>
		/// <item>
		/// <description>Открытые позиции.</description>
		/// </item>
		/// <item>
		/// <description>Гарантийное обеспечение (фьючерсы).</description>
		/// </item>
		/// <item>
		/// <description>Гарантийное обеспечение продажи опционов и по синтетическим позициям (опционы).</description>
		/// </item>
		/// <item>
		/// <description>Лимит цены.</description>
		/// </item>
		/// <item>
		/// <description>Статус.</description>
		/// </item>
		/// <item>
		/// <description>Волатильность и теоретическая цена.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<string, Tuple<decimal?, decimal?, DateTime>, decimal?, decimal?, decimal?, decimal?, decimal?, QuoteChange, QuoteChange, decimal?, Tuple<decimal?, decimal?>, Tuple<decimal?, decimal?>, Tuple<decimal?, decimal?>, int, Tuple<decimal?, decimal?>> SecurityChanged;

		/// <summary>
		/// Событие о появлении портфеля.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Номер счета в списке.</description>
		/// </item>
		/// <item>
		/// <description>Всего счетов в списке.</description>
		/// </item>
		/// <item>
		/// <description>Наименование портфеля.</description>
		/// </item>
		/// <item>
		/// <description>Площадка доступная для портфеля.</description>
		/// </item>
		/// <item>
		/// <description>Статус портфеля.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<int, int, string, string, SmartPortfolioStatus> NewPortfolio;

		/// <summary>
		/// Событие об изменении портфеля.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Номер торгового счёта на торговой площадке.</description>
		/// </item>
		/// <item>
		/// <description>Сумма доступных наличных средств на счёте.</description>
		/// </item>
		/// <item>
		/// <description>Плечо для маржинальной торговли.</description>
		/// </item>
		/// <item>
		/// <description>Сумма биржевой комиссии за торговый день.</description>
		/// </item>
		/// <item>
		/// <description>Сальдо торгового дня.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<string, decimal?, decimal?, decimal?, decimal?> PortfolioChanged;

		/// <summary>
		/// Событие об изменении позиции.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Номер торгового счёта на торговой площадке.</description>
		/// </item>
		/// <item>
		/// <description>Код ЦБ из таблицы котировок SmartTrade.</description>
		/// </item>
		/// <item>
		/// <description>Средневзвешенная цена.</description>
		/// </item>
		/// <item>
		/// <description>Объём сделки, если положительный Long, отрицательный в случае Short.</description>
		/// </item>
		/// <item>
		/// <description>Количество ЦБ с учетом выставленных заявок.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<string, string, decimal?, decimal?, decimal?> PositionChanged;

		/// <summary>
		/// Событие о появлении собственной сделки.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Номер торгового счёта на торговой площадке.</description>
		/// </item>
		/// <item>
		/// <description>Код ЦБ из таблицы котировок SmartTrade.</description>
		/// </item>
		/// <item>
		/// <description>Идентификатор заявки на сервере котировок.</description>
		/// </item>
		/// <item>
		/// <description>Цена сделки.</description>
		/// </item>
		/// <item>
		/// <description>Объём сделки.</description>
		/// </item>
		/// <item>
		/// <description>Время сделки.</description>
		/// </item>
		/// <item>
		/// <description>Id сделки на рынке.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<string, string, long, decimal?, decimal?, DateTime, long> NewMyTrade;

		/// <summary>
		/// Событие о появлении тиковой сделки.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Код ЦБ из таблицы котировок SmartTrade.</description>
		/// </item>
		/// <item>
		/// <description>Время сделки.</description>
		/// </item>
		/// <item>
		/// <description>Цена сделки.</description>
		/// </item>
		/// <item>
		/// <description>Объём сделки.</description>
		/// </item>
		/// <item>
		/// <description>Id сделки на рынке.</description>
		/// </item>
		/// <item>
		/// <description>Направление сделки.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<string, DateTime, decimal?, decimal?, long, SmartOrderAction> NewTrade;

		/// <summary>
		/// Событие о появлении исторической тиковой сделки.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Номер сделки в списке.</description>
		/// </item>
		/// <item>
		/// <description>Всего сделок в списке.</description>
		/// </item>
		/// <item>
		/// <description>Код ЦБ из таблицы котировок SmartTrade.</description>
		/// </item>
		/// <item>
		/// <description>Время сделки.</description>
		/// </item>
		/// <item>
		/// <description>Цена сделки.</description>
		/// </item>
		/// <item>
		/// <description>Объём сделки.</description>
		/// </item>
		/// <item>
		/// <description>Id сделки на рынке.</description>
		/// </item>
		/// <item>
		/// <description>Направление сделки.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<int, int, string, DateTime, decimal?, decimal?, long, SmartOrderAction> NewHistoryTrade;

		/// <summary>
		/// Событие о появлении исторической временной свечи.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Номер бара в списке.</description>
		/// </item>
		/// <item>
		/// <description>Всего баров в списке.</description>
		/// </item>
		/// <item>
		/// <description>Код ЦБ из таблицы котировок SmartTrade.</description>
		/// </item>
		/// <item>
		/// <description>Интервал времени.</description>
		/// </item>
		/// <item>
		/// <description>Дата и время интервала.</description>
		/// </item>
		/// <item>
		/// <description>Цена первой сделки после открытия в интервале.</description>
		/// </item>
		/// <item>
		/// <description>Максимальная цена сделки в интервале.</description>
		/// </item>
		/// <item>
		/// <description>Минимальная цена сделки в интервале.</description>
		/// </item>
		/// <item>
		/// <description>Цена последней сделки в интервале.</description>
		/// </item>
		/// <item>
		/// <description>Объём сделок в интервале.</description>
		/// </item>
		/// <item>
		/// <description>Открытые позиции.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<int, int, string, SmartComTimeFrames, DateTime, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?> NewBar;

		/// <summary>
		/// Событие о появлении новой заявки.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Идентификатор заявки.</description>
		/// </item>
		/// <item>
		/// <description>Id заявки на сервере котировок.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<int, string> NewOrder;

		/// <summary>
		/// Событие об изменении заявки.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Номер торгового счёта на торговой площадке.</description>
		/// </item>
		/// <item>
		/// <description>Код ЦБ из таблицы котировок SmartTrade.</description>
		/// </item>
		/// <item>
		/// <description>Состояние заявки.</description>
		/// </item>
		/// <item>
		/// <description>Вид торговой операции.</description>
		/// </item>
		/// <item>
		/// <description>Тип заявки.</description>
		/// </item>
		/// <item>
		/// <description>Срок действия приказа. True - один день, false - до отмены.</description>
		/// </item>
		/// <item>
		/// <description>Цена Лимит для заявок типа Лимит и Стоп-Лимит.</description>
		/// </item>
		/// <item>
		/// <description>Объем заявки.</description>
		/// </item>
		/// <item>
		/// <description>Цена СТОП для заявок типа Стоп и Стоп-Лимит.</description>
		/// </item>
		/// <item>
		/// <description>Объем, оставшийся в заявке.</description>
		/// </item>
		/// <item>
		/// <description>Время последнего изменения заявки.</description>
		/// </item>
		/// <item>
		/// <description>Id приказа на сервере котировок.</description>
		/// </item>
		/// <item>
		/// <description>Идентификатор приказа на сервере котировок.</description>
		/// </item>
		/// <item>
		/// <description>Системный статус.</description>
		/// </item>
		/// <item>
		/// <description>Идентификатор транзакции.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<string, string, SmartOrderState, SmartOrderAction, SmartOrderType, bool, decimal?, int, decimal?, int, DateTime, string, long, int, int> OrderChanged;

		/// <summary>
		/// Событие об ошибке при регистрации заявки.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Идентификатор заявки.</description>
		/// </item>
		/// <item>
		/// <description>Id заявки на сервере котировок.</description>
		/// </item>
		/// <item>
		/// <description>Причина.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<int, string, string> OrderFailed;

		/// <summary>
		/// Событие об успешной перерегистрации заявки.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Id заявки на сервере котировок.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<string> OrderReRegistered;

		/// <summary>
		/// Событие об ошибке при перерегистрации заявки.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Id заявки на сервере котировок.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<string> OrderReRegisterFailed;

		/// <summary>
		/// Событие об успешной отмене заявки.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Id заявки на сервере котировок.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<string> OrderCancelled;

		/// <summary>
		/// Событие об ошибке при отмене заявки.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Id заявки на сервере котировок.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<string> OrderCancelFailed;

		/// <summary>
		/// Событие изменения стакана.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Код ЦБ из таблицы котировок SmartTrade.</description>
		/// </item>
		/// <item>
		/// <description>Порядковый номер строки в очереди заявок.</description>
		/// </item>
		/// <item>
		/// <description>Общее количество строк в очереди заявок.</description>
		/// </item>
		/// <item>
		/// <description>Цена на покупку.</description>
		/// </item>
		/// <item>
		/// <description>Объем ценных бумаг по цене на покупку.</description>
		/// </item>
		/// <item>
		/// <description>Цена на продажу.</description>
		/// </item>
		/// <item>
		/// <description>Объем ценных бумаг по цене на продажу.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<string, int, int, decimal?, decimal?, decimal?, decimal?> QuoteChanged;

		/// <summary>
		/// Событие об успешном подсоединении к серверу SmartCOM.
		/// </summary>
		public event Action Connected;

		/// <summary>
		/// Событие об успешном отсоединении от сервера SmartCOM или о разрыве соединения.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Причина.</description>
		/// </item>
		/// </list>
		/// </remarks>
		public event Action<Exception> Disconnected;

		/// <summary>
		/// Подключиться к SmartCOM.
		/// </summary>
		/// <param name="host">Адрес сервера.</param>
		/// <param name="port">Порт сервера.</param>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		public void Connect(string host, short port, string login, string password)
		{
			Server = new TServer();
			SubscribeEvents();

			OnConnect(host, port, login, password);
		}

		/// <summary>
		/// Подключиться к SmartCOM.
		/// </summary>
		/// <param name="host">Адрес сервера.</param>
		/// <param name="port">Порт сервера.</param>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		protected abstract void OnConnect(string host, short port, string login, string password);

		/// <summary>
		/// Отключиться от SmartCOM.
		/// </summary>
		public void Disconnect()
		{
			_errorWhileDisconnect = null;

			try
			{
				OnDisconnect();
			}
			finally
			{
				var server = Server;

				if (server != null)
				{
					UnSubscribeEvents();
					server.ReleaseComObject();
					Server = null;
					//CoFreeUnusedLibraries();	
				}
			}

			if (_errorWhileDisconnect == null)
				RaiseDisconnected((Exception)null);
		}

		//[DllImport("ole32.dll")]
		//private static extern void CoFreeUnusedLibraries();

		/// <summary>
		/// Подписаться на события.
		/// </summary>
		protected abstract void SubscribeEvents();

		/// <summary>
		/// Отписаться от событий.
		/// </summary>
		protected abstract void UnSubscribeEvents();

		/// <summary>
		/// Отключиться от SmartCOM.
		/// </summary>
		protected abstract void OnDisconnect();

		/// <summary>
		/// Запросить все доступные инструменты.
		/// </summary>
		public abstract void LookupSecurities();

		/// <summary>
		/// Запросить все доступные портфель.
		/// </summary>
		public abstract void LookupPortfolios();

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
		/// <param name="transactionId">Идентификатор транзакции.</param>
		public abstract void RegisterOrder(string portfolioName, string securityId, SmartOrderAction action, SmartOrderType type, SmartOrderValidity validity, double price, int volume, double stopPrice, int transactionId);

		/// <summary>
		/// Перерегистрировать заявку.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля.</param>
		/// <param name="newPrice">Новая цена.</param>
		/// <param name="smartId">SmartCOM идентификатор заявки.</param>
		public abstract void ReRegisterOrder(string portfolioName, double newPrice, string smartId);

		/// <summary>
		/// Отменить заявку.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="smartId">SmartCOM идентификатор заявки.</param>
		public abstract void CancelOrder(string portfolioName, string securityId, string smartId);

		/// <summary>
		/// Отменить все активные заявки.
		/// </summary>
		public abstract void CancelAllOrders();

		/// <summary>
		/// Начать получать новую информацию по инструменту.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо начать получать новую информацию.</param>
		public abstract void SubscribeSecurity(string securityId);

		/// <summary>
		/// Остановить получение новой информации.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо остановить получение новой информации.</param>
		public abstract void UnSubscribeSecurity(string securityId);

		/// <summary>
		/// Начать получать котировки (стакан) по инструменту.
		/// Значение котировок можно получить через событие <see cref="ISmartComWrapper.QuoteChanged"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо начать получать котировки.</param>
		public abstract void SubscribeMarketDepth(string securityId);

		/// <summary>
		/// Остановить получение котировок по инструменту.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо остановить получение котировок.</param>
		public abstract void UnSubscribeMarketDepth(string securityId);

		/// <summary>
		/// Начать получать сделки (тиковые данные) по инструменту. Новые сделки будут приходить через событие <see cref="ISmartComWrapper.NewTrade"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо начать получать сделки.</param>
		public abstract void SubscribeTrades(string securityId);

		/// <summary>
		/// Остановить получение сделок (тиковые данные) по инструменту.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо остановить получение сделок.</param>
		public abstract void UnSubscribeTrades(string securityId);

		/// <summary>
		/// Начать получать новую информацию по портфелю.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля, по которому необходимо начать получать новую информацию.</param>
		public abstract void SubscribePortfolio(string portfolioName);

		/// <summary>
		/// Остановить получение новой информации по портфелю.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля, по которому необходимо остановить получение новой информации.</param>
		public abstract void UnSubscribePortfolio(string portfolioName);

		/// <summary>
		/// Начать получать исторические тиковые сделки от сервера SmartCOM через событие <see cref="ISmartComWrapper.NewHistoryTrade"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо начать получать исторические сделки.</param>
		/// <param name="from">Временная точка отсчета.</param>
		/// <param name="count">Количество сделок.</param>
		public abstract void RequestHistoryTrades(string securityId, DateTime @from, int count);

		/// <summary>
		/// Начать получать исторические временные свечи от сервера SmartCOM через событие <see cref="ISmartComWrapper.NewBar"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо начать получать исторические свечи.</param>
		/// <param name="timeFrame">Тайм-фрейм.</param>
		/// <param name="from">Временная точка отсчета.</param>
		/// <param name="count">Количество свечек.</param>
		public abstract void RequestHistoryBars(string securityId, SmartComTimeFrames timeFrame, DateTime @from, int count);

		///// <summary>
		///// Время последнего обращения к SmartCOM.
		///// </summary>
		//private DateTime LastTransactionTime { get; set; }

		//private void StartCheckAliveTimer()
		//{
		//	var interval = TimeSpan.FromSeconds(10);
		//	_checkAliveTimer = ThreadingHelper.Timer(() =>
		//	{
		//		if (Server == null)
		//			return;

		//		try
		//		{
		//			if ((DateTime.Now - LastTransactionTime) > interval)
		//				Do(OnPing, false);
		//		}
		//		catch (Exception ex)
		//		{
		//			CloseCheckAliveTimer();
		//			RaiseDisconnected(new InvalidOperationException("SmartCOM не ответил на проверку живой-мертвый. Возможно, приложение SmartCOM зависло.", ex));
		//		}
		//	})
		//	.Interval(interval);
		//}

		internal TServer SafeGetServer()
		{
			var server = Server;

			if (server == null)
				throw new InvalidOperationException(LocalizedStrings.Str1875);

			return server;
		}

		internal void OnAddSecurity(int row, int rowCount, string securityId, string shortName, string longName, string type, int decimals, int lotSize,
			double stepPrice, double priceStep, string isin, string board, DateTime expiryDate, double daysBeforeExpiry, double strike)
		{
			NewSecurity.SafeInvoke(row, rowCount, securityId, shortName, longName, type, decimals, lotSize, stepPrice.ToDecimal(), priceStep.ToDecimal(),
					isin, board, DateTime.FromOADate(0) == expiryDate ? (DateTime?)null : expiryDate, daysBeforeExpiry.ToDecimal(), strike.ToDecimal());
		}

		internal void OnUpdateSecurity(string securityId, DateTime time, double open, double high, double low, double close, double lastTradePrice, double volume,
			double lastTradeVolume, double bid, double ask, double bidSize, double askSize, double openInt, double goBuy, double goSell, double goBase,
			double goBaseBacked, double highLimit, double lowLimit, int tradingStatus, double volat, double theorPrice)
		{
			SecurityChanged.SafeInvoke(securityId,
				Tuple.Create(lastTradePrice.ToDecimal(), lastTradeVolume.ToDecimal(), time),
				open.ToDecimal(), high.ToDecimal(), low.ToDecimal(), close.ToDecimal(), volume.ToDecimal(),
				new QuoteChange(Sides.Buy, bid.ToDecimal() ?? 0, bidSize.ToDecimal() ?? 0),
				new QuoteChange(Sides.Sell, ask.ToDecimal() ?? 0, askSize.ToDecimal() ?? 0),
				openInt.ToDecimal(),
				Tuple.Create(goBuy.ToDecimal(), goSell.ToDecimal()),
				Tuple.Create(goBase.ToDecimal(), goBaseBacked.ToDecimal()),
				Tuple.Create(lowLimit.ToDecimal(), highLimit.ToDecimal()),
				tradingStatus, Tuple.Create(volat.ToDecimal(), theorPrice.ToDecimal()));
		}

		internal void OnUpdateQuotes(string securityId, int row, int rowCount, double bidPrice, double bidVolume, double askPrice, double askVolume)
		{
			QuoteChanged.SafeInvoke(securityId, row, rowCount, bidPrice.ToDecimal(), bidVolume.ToDecimal(), askPrice.ToDecimal(), askVolume.ToDecimal());
		}

		internal void OnAddMyTrade(string portfolio, string securityId, string orderId, double price, double amount, DateTime time, string tradeNo)
		{
			NewMyTrade.SafeInvoke(portfolio, securityId, orderId.To<long>(), price.ToDecimal(), amount.Abs().ToDecimal(), time, tradeNo.To<long>());
		}

		internal void OnAddTrade(string securityId, DateTime time, double price, double volume, string tradeNo, SmartOrderAction action)
		{
			NewTrade.SafeInvoke(securityId, time, price.ToDecimal(), volume.ToDecimal(), tradeNo.To<long>(), action);
		}

		internal void OnAddPortfolio(int row, int rowCount, string name, string exchange, SmartPortfolioStatus status)
		{
			NewPortfolio.SafeInvoke(row, rowCount, name, exchange, status);
		}

		internal void OnUpdatePortfolio(string name, double cash, double leverage, double commission, double saldo)
		{
			PortfolioChanged.SafeInvoke(name, cash.ToDecimal(), leverage.ToDecimal(), commission.ToDecimal(), saldo.ToDecimal());
		}

		internal void OnUpdatePosition(string portfolioName, string securityId, double avPrice, double amount, double planned)
		{
			PositionChanged.SafeInvoke(portfolioName, securityId, avPrice.ToDecimal(), amount.ToDecimal(), planned.ToDecimal());
		}

		internal void OnAddTradeHistory(int row, int rowCount, string securityId, DateTime time, double price, double volume, string tradeno, SmartOrderAction action)
		{
			NewHistoryTrade.SafeInvoke(row, rowCount, securityId, time, price.ToDecimal(), volume.ToDecimal(), tradeno.To<long>(), action);
		}

		internal void OnAddBar(int row, int rowCount, string securityId, SmartBarInterval interval, DateTime time, double open, double high, double low, double close, double volume, double openInt)
		{
			NewBar.SafeInvoke(row, rowCount, securityId, SmartComTimeFrames.GetTimeFrame(interval), time, open.ToDecimal(), high.ToDecimal(), low.ToDecimal(), close.ToDecimal(), volume.ToDecimal(), openInt.ToDecimal());
		}

		internal void OnOrderSucceded(int cookie, string smartOrderId)
		{
			NewOrder.SafeInvoke(cookie, smartOrderId);
		}

		internal void OnUpdateOrder(string portfolio, string securityId, SmartOrderState state, SmartOrderAction action, SmartOrderType type,
			SmartOrderValidity validity, double price, double volume, double stop, double balance, DateTime time, string smartOrderId,
			string orderIdStr, int status, int transactionId)
		{
			// http://www.itinvest.ru/forum/index.php?showtopic=63063&st=0&p=242023&#entry242023
			OrderChanged.SafeInvoke(portfolio, securityId, state,
					action, type, validity == SmartOrderValidity.Day, price.ToDecimal(), (int)volume,
					stop.ToDecimal(), (int)balance, time, smartOrderId, orderIdStr.To<long>(), status, transactionId);
		}

		internal void OnOrderFailed(int cookie, string smartOrderId, string reason)
		{
			OrderFailed.SafeInvoke(cookie, smartOrderId, reason);
		}

		internal void OnOrderMoveSucceeded(string smartOrderId)
		{
			OrderReRegistered.SafeInvoke(smartOrderId);
		}

		internal void OnOrderMoveFailed(string smartOrderId)
		{
			OrderReRegisterFailed.SafeInvoke(smartOrderId);
		}

		internal void OnOrderCancelSucceeded(string smartOrderId)
		{
			OrderCancelled.SafeInvoke(smartOrderId);
		}

		internal void OnOrderCancelFailed(string smartOrderId)
		{
			OrderCancelFailed.SafeInvoke(smartOrderId);
		}

		internal void OnConnected()
		{
			Connected.SafeInvoke();
		}

		internal void OnDisconnected(string description)
		{
			if (!description.IsEmpty())
			{
				description = description.Trim().ToLowerInvariant();

				switch (description)
				{
					case "bad user name or password":
						description = LocalizedStrings.WrongLoginOrPassword;
						break;

					case "disconnected by user.":
					case "disconnected by user":
						description = string.Empty;
						break;

					case "timout detected. check your  internet connectivity or event handler code":
						description = LocalizedStrings.Str1876;
						break;

					case "winsock":
						description = LocalizedStrings.NetworkError;
						break;
				}
			}

			RaiseDisconnected(description);
		}

		private void RaiseDisconnected(string errorDescription)
		{
			RaiseDisconnected(errorDescription.IsEmpty() ? null : new InvalidOperationException(errorDescription));
		}

		private void RaiseDisconnected(Exception error)
		{
			_errorWhileDisconnect = error != null;
			Disconnected.SafeInvoke(error);
		}
	}
}