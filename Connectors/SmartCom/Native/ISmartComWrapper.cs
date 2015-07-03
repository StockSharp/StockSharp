namespace StockSharp.SmartCom.Native
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// Статусы портфелей.
	/// </summary>
	public enum SmartPortfolioStatus
	{
		/// <summary>
		/// Брокерский.
		/// </summary>
		Broker,
		
		/// <summary>
		/// Доверительное управление.
		/// </summary>
		TrustedManagement,
		
		/// <summary>
		/// Просмотровый.
		/// </summary>
		ReadOnly,
		
		/// <summary>
		/// Заблокирован.
		/// </summary>
		Blocked,
		
		/// <summary>
		/// Ограничен.
		/// </summary>
		Restricted,
		
		/// <summary>
		/// Автоматически ограничен.
		/// </summary>
		AutoRestricted,
		
		/// <summary>
		/// Заявка не подписана.
		/// </summary>
		OrderNotSigned
	}

	/// <summary>
	/// Интервалы свечек.
	/// </summary>
	public enum SmartBarInterval
	{
		/// <summary>
		/// Тиковый.
		/// </summary>
		Tick,
		
		/// <summary>
		/// Минутный тайм-фрейм.
		/// </summary>
		Min1,
		
		/// <summary>
		/// Пяти минутный тайм-фрейм.
		/// </summary>
		Min5,
		
		/// <summary>
		/// Десяти минутный тайм-фрейм.
		/// </summary>
		Min10,
		
		/// <summary>
		/// Пятнадцати минутный тайм-фрейм.
		/// </summary>
		Min15,
		
		/// <summary>
		/// Тридцати минутный тайм-фрейм.
		/// </summary>
		Min30,
		
		/// <summary>
		/// Часовой тайм-фрейм.
		/// </summary>
		Min60,
		
		/// <summary>
		/// Двух часовой тайм-фрейм.
		/// </summary>
		Hour2,
		
		/// <summary>
		/// Четырех часовой тайм-фрейм.
		/// </summary>
		Hour4,
		
		/// <summary>
		/// Дневной тайм-фрейм.
		/// </summary>
		Day,
		
		/// <summary>
		/// Недельный тайм-фрейм.
		/// </summary>
		Week,
		
		/// <summary>
		/// Месячный тайм-фрейм.
		/// </summary>
		Month,

		/// <summary>
		/// Квартальный тайм-фрейм.
		/// </summary>
		Quarter,

		/// <summary>
		/// Годовой тайм-фрейм.
		/// </summary>
		Year
	}

	/// <summary>
	/// Действия заявки.
	/// </summary>
	public enum SmartOrderAction
	{
		/// <summary>
		/// Покупка.
		/// </summary>
		Buy = 1,
		
		/// <summary>
		/// Продажа.
		/// </summary>
		Sell = 2,
		
		/// <summary>
		/// Короткая продажа.
		/// </summary>
		Short = 3,
		
		/// <summary>
		/// Покрытие.
		/// </summary>
		Cover = 4,
	}

	/// <summary>
	/// Типы заявок.
	/// </summary>
	public enum SmartOrderType
	{
		/// <summary>
		/// Стоп.
		/// </summary>
		Stop,
		
		/// <summary>
		/// Стоп-лимит.
		/// </summary>
		StopLimit,
		
		/// <summary>
		/// Лимитная.
		/// </summary>
		Limit,
		
		/// <summary>
		/// Рыночная.
		/// </summary>
		Market
	}

	/// <summary>
	/// Статусы заявок.
	/// </summary>
	public enum SmartOrderState
	{
		/// <summary>
		/// Отправлена.
		/// </summary>
		Submited,
		
		/// <summary>
		/// В ожидании получения статуса.
		/// </summary>
		Pending,
		
		/// <summary>
		/// Активна.
		/// </summary>
		Open,
		
		/// <summary>
		/// Ислекло время действия.
		/// </summary>
		Expired,
		
		/// <summary>
		/// Отменена.
		/// </summary>
		Cancel,
		
		/// <summary>
		/// Исполнена.
		/// </summary>
		Filled,
		
		/// <summary>
		/// Частично исполнена.
		/// </summary>
		Partial,

		/// <summary>
		/// Отклонена брокером.
		/// </summary>
		ContragentReject,
		
		/// <summary>
		/// Отклонена брокером.
		/// </summary>
		ContragentCancel,
		
		/// <summary>
		/// Отклонена биржей.
		/// </summary>
		SystemReject,
		
		/// <summary>
		/// Отменена биржей.
		/// </summary>
		SystemCancel
	}

	/// <summary>
	/// Время жизни заявки.
	/// </summary>
	public enum SmartOrderValidity
	{
		/// <summary>
		/// Текущая сессия.
		/// </summary>
		Day,

		/// <summary>
		/// До отмены.
		/// </summary>
		Gtc
	}

	/// <summary>
	/// Версии SmartCOM API.
	/// </summary>
	public enum SmartComVersions
	{
		/// <summary>
		/// Вторая версия.
		/// </summary>
		V2 = 2,

		/// <summary>
		/// Третья версия.
		/// </summary>
		V3 = 3,
	}

	/// <summary>
	/// Интерфейс обертки над SmartCOM API.
	/// </summary>
	public interface ISmartComWrapper
	{
		///// <summary>
		///// Проверить соединение.
		///// </summary>
		//bool IsConnected { get; }

		/// <summary>
		/// Поддерживается ли отмена всех заявок.
		/// </summary>
		bool IsSupportCancelAllOrders { get; }

		/// <summary>
		/// Версия обертки.
		/// </summary>
		SmartComVersions Version { get; }

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
		event Action<int, int, string, string, string, string, int, int, decimal?, decimal?, string, string, DateTime?, decimal?, decimal?> NewSecurity;

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
		event Action<string, Tuple<decimal?, decimal?, DateTime>, decimal?, decimal?, decimal?, decimal?, decimal?, QuoteChange, QuoteChange, decimal?, Tuple<decimal?, decimal?>, Tuple<decimal?, decimal?>, Tuple<decimal?, decimal?>, int, Tuple<decimal?, decimal?>> SecurityChanged;

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
		event Action<int, int, string, string, SmartPortfolioStatus> NewPortfolio;

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
		event Action<string, decimal?, decimal?, decimal?, decimal?> PortfolioChanged;

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
		event Action<string, string, decimal?, decimal?, decimal?> PositionChanged;

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
		event Action<string, string, long, decimal?, decimal?, DateTime, long> NewMyTrade;

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
		event Action<string, DateTime, decimal?, decimal?, long, SmartOrderAction> NewTrade;

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
		event Action<int, int, string, DateTime, decimal?, decimal?, long, SmartOrderAction> NewHistoryTrade;

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
		event Action<int, int, string, SmartComTimeFrames, DateTime, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?> NewBar;

		/// <summary>
		/// Событие о появлении новой заявки.
		/// </summary>
		/// <remarks>
		/// Передаваемые параметры:
		/// <list type="number">
		/// <item>
		/// <description>Уникальный номер заявки.</description>
		/// </item>
		/// <item>
		/// <description>Id заявки на сервере котировок.</description>
		/// </item>
		/// </list>
		/// </remarks>
		event Action<int, string> NewOrder;

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
		event Action<string, string, SmartOrderState, SmartOrderAction, SmartOrderType, bool, decimal?, int, decimal?, int, DateTime, string, long, int, int> OrderChanged;

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
		event Action<int, string, string> OrderFailed;

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
		event Action<string> OrderReRegistered;

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
		event Action<string> OrderReRegisterFailed;

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
		event Action<string> OrderCancelled;

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
		event Action<string> OrderCancelFailed;

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
		event Action<string, int, int, decimal?, decimal?, decimal?, decimal?> QuoteChanged;

		/// <summary>
		/// Событие об успешном подсоединении к серверу SmartCOM.
		/// </summary>
		event Action Connected;

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
		event Action<Exception> Disconnected;

		/// <summary>
		/// Подключиться к SmartCOM.
		/// </summary>
		/// <param name="host">Адрес сервера.</param>
		/// <param name="port">Порт сервера.</param>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		void Connect(string host, short port, string login, string password);

		/// <summary>
		/// Отключиться от SmartCOM.
		/// </summary>
		void Disconnect();

		/// <summary>
		/// Запросить все доступные инструменты.
		/// </summary>
		void LookupSecurities();

		/// <summary>
		/// Запросить все доступные портфель.
		/// </summary>
		void LookupPortfolios();

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
		void RegisterOrder(string portfolioName, string securityId, SmartOrderAction action, SmartOrderType type, SmartOrderValidity validity, double price, int volume, double stopPrice, int transactionId);

		/// <summary>
		/// Перерегистрировать заявку.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля.</param>
		/// <param name="newPrice">Новая цена.</param>
		/// <param name="smartId">SmartCOM идентификатор заявки.</param>
		void ReRegisterOrder(string portfolioName, double newPrice, string smartId);

		/// <summary>
		/// Отменить заявку.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="smartId">SmartCOM идентификатор заявки.</param>
		void CancelOrder(string portfolioName, string securityId, string smartId);

		/// <summary>
		/// Отменить все активные заявки.
		/// </summary>
		void CancelAllOrders();

		/// <summary>
		/// Начать получать новую информацию по инструменту.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо начать получать новую информацию.</param>
		void SubscribeSecurity(string securityId);

		/// <summary>
		/// Остановить получение новой информации.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо остановить получение новой информации.</param>
		void UnSubscribeSecurity(string securityId);

		/// <summary>
		/// Начать получать котировки (стакан) по инструменту.
		/// Значение котировок можно получить через событие <see cref="QuoteChanged"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо начать получать котировки.</param>
		void SubscribeMarketDepth(string securityId);

		/// <summary>
		/// Остановить получение котировок по инструменту.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо остановить получение котировок.</param>
		void UnSubscribeMarketDepth(string securityId);

		/// <summary>
		/// Начать получать сделки (тиковые данные) по инструменту. Новые сделки будут приходить через событие <see cref="NewTrade"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо начать получать сделки.</param>
		void SubscribeTrades(string securityId);

		/// <summary>
		/// Остановить получение сделок (тиковые данные) по инструменту.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, по которому необходимо остановить получение сделок.</param>
		void UnSubscribeTrades(string securityId);

		/// <summary>
		/// Начать получать новую информацию по портфелю.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля, по которому необходимо начать получать новую информацию.</param>
		void SubscribePortfolio(string portfolioName);

		/// <summary>
		/// Остановить получение новой информации по портфелю.
		/// </summary>
		/// <param name="portfolioName">Номер портфеля, по которому необходимо остановить получение новой информации.</param>
		void UnSubscribePortfolio(string portfolioName);

		/// <summary>
		/// Начать получать исторические тиковые сделки от сервера SmartCOM через событие <see cref="NewHistoryTrade"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо начать получать исторические сделки.</param>
		/// <param name="from">Временная точка отсчета.</param>
		/// <param name="count">Количество сделок.</param>
		void RequestHistoryTrades(string securityId, DateTime from, int count);

		/// <summary>
		/// Начать получать исторические временные свечи от сервера SmartCOM через событие <see cref="NewBar"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо начать получать исторические свечи.</param>
		/// <param name="timeFrame">Тайм-фрейм.</param>
		/// <param name="from">Временная точка отсчета.</param>
		/// <param name="count">Количество свечек.</param>
		void RequestHistoryBars(string securityId, SmartComTimeFrames timeFrame, DateTime from, int count);
	}
}