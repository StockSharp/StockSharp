namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Состояния подключений.
	/// </summary>
	public enum ConnectionStates
	{
		/// <summary>
		/// Не активно.
		/// </summary>
		Disconnected,

		/// <summary>
		/// В процессе отключения.
		/// </summary>
		Disconnecting,

		/// <summary>
		/// В процессе подключения.
		/// </summary>
		Connecting,

		/// <summary>
		/// Подключение активно.
		/// </summary>
		Connected,

		/// <summary>
		/// Ошибка подключения.
		/// </summary>
		Failed,
	}

	/// <summary>
	/// Основной интерфейс, предоставляющий подключение с торговыми системами.
	/// </summary>
	public interface IConnector : IPersistable, ILogReceiver, IMarketDataProvider, ISecurityProvider
	{
		/// <summary>
		/// Событие появления собственных новых сделок.
		/// </summary>
		event Action<IEnumerable<MyTrade>> NewMyTrades;

		/// <summary>
		/// Событие появления всех новых сделок.
		/// </summary>
		event Action<IEnumerable<Trade>> NewTrades;

		/// <summary>
		/// Событие появления новых заявок.
		/// </summary>
		event Action<IEnumerable<Order>> NewOrders;

		/// <summary>
		/// Событие изменения состояния заявок (снята, удовлетворена).
		/// </summary>
		event Action<IEnumerable<Order>> OrdersChanged;

		/// <summary>
		/// Событие об ошибках, связанных с регистрацией заявок.
		/// </summary>
		event Action<IEnumerable<OrderFail>> OrdersRegisterFailed;

		/// <summary>
		/// Событие об ошибках, связанных со снятием заявок.
		/// </summary>
		event Action<IEnumerable<OrderFail>> OrdersCancelFailed;

		/// <summary>
		/// Событие об ошибках, связанных с регистрацией стоп-заявок.
		/// </summary>
		event Action<IEnumerable<OrderFail>> StopOrdersRegisterFailed;

		/// <summary>
		/// Событие об ошибках, связанных со снятием стоп-заявок.
		/// </summary>
		event Action<IEnumerable<OrderFail>> StopOrdersCancelFailed;

		/// <summary>
		/// Событие появления новых стоп-заявок.
		/// </summary>
		event Action<IEnumerable<Order>> NewStopOrders;

		/// <summary>
		/// Событие изменения состояния стоп-заявок.
		/// </summary>
		event Action<IEnumerable<Order>> StopOrdersChanged;

		/// <summary>
		/// Событие появления новых инструментов.
		/// </summary>
		event Action<IEnumerable<Security>> NewSecurities;

		/// <summary>
		/// Событие изменения параметров инструментов.
		/// </summary>
		event Action<IEnumerable<Security>> SecuritiesChanged;

		/// <summary>
		/// Событие появления новых портфелей.
		/// </summary>
		event Action<IEnumerable<Portfolio>> NewPortfolios;

		/// <summary>
		/// Событие изменения параметров портфелей.
		/// </summary>
		event Action<IEnumerable<Portfolio>> PortfoliosChanged;

		/// <summary>
		/// Событие появления новых позиций.
		/// </summary>
		event Action<IEnumerable<Position>> NewPositions;

		/// <summary>
		/// Событие изменения параметров позиций.
		/// </summary>
		event Action<IEnumerable<Position>> PositionsChanged;

		/// <summary>
		/// Событие появления новых стаканов с котировками.
		/// </summary>
		event Action<IEnumerable<MarketDepth>> NewMarketDepths;

		/// <summary>
		/// Событие изменения стаканов с котировками.
		/// </summary>
		event Action<IEnumerable<MarketDepth>> MarketDepthsChanged;

		/// <summary>
		/// Событие появления новых записей в логе заявок.
		/// </summary>
		event Action<IEnumerable<OrderLogItem>> NewOrderLogItems;

		/// <summary>
		/// Событие появления новости.
		/// </summary>
		event Action<News> NewNews;

		/// <summary>
		/// Событие изменения новости (например, при скачивании текста <see cref="StockSharp.BusinessEntities.News.Story"/>).
		/// </summary>
		event Action<News> NewsChanged;

		/// <summary>
		/// Событие обработки нового сообщения <see cref="Message"/>.
		/// </summary>
		event Action<Message, MessageDirections> NewMessage;

		/// <summary>
		/// Событие успешного подключения.
		/// </summary>
		event Action Connected;

		/// <summary>
		/// Событие успешного отключения.
		/// </summary>
		event Action Disconnected;

		/// <summary>
		/// Событие ошибки подключения (например, соединения было разорвано).
		/// </summary>
		event Action<Exception> ConnectionError;

		/// <summary>
		/// Событие успешного запуска экспорта.
		/// </summary>
		event Action ExportStarted;

		/// <summary>
		/// Событие успешной остановки экспорта.
		/// </summary>
		event Action ExportStopped;

		/// <summary>
		/// Событие ошибки экспорта (например, соединения было разорвано).
		/// </summary>
		event Action<Exception> ExportError;

		/// <summary>
		/// Событие, сигнализирующее об ошибке при получении или обработке новых данных с сервера.
		/// </summary>
		event Action<Exception> ProcessDataError;

		/// <summary>
		/// Событие, сигнализирующее о новых экспортируемых данных.
		/// </summary>
		event Action NewDataExported;

		/// <summary>
		/// Событие, сигнализирующее об изменении текущего времени на биржевых площадках <see cref="ExchangeBoards"/>.
		/// Передается разница во времени, прошедшее с последнего вызова события. Первый раз событие передает значение <see cref="TimeSpan.Zero"/>.
		/// </summary>
		event Action<TimeSpan> MarketTimeChanged;

		/// <summary>
		/// Событие, передающее результат поиска, запущенного через метод <see cref="LookupSecurities(StockSharp.BusinessEntities.Security)"/>.
		/// </summary>
		event Action<IEnumerable<Security>> LookupSecuritiesResult;

		/// <summary>
		/// Событие, передающее результат поиска, запущенного через метод <see cref="LookupPortfolios"/>.
		/// </summary>
		event Action<IEnumerable<Portfolio>> LookupPortfoliosResult;

		/// <summary>
		/// Событие успешной регистрации инструмента для получения маркет-данных.
		/// </summary>
		event Action<Security, MarketDataTypes> MarketDataSubscriptionSucceeded;

		/// <summary>
		/// Событие ошибки регистрации инструмента для получения маркет-данных.
		/// </summary>
		event Action<Security, MarketDataTypes, Exception> MarketDataSubscriptionFailed;

		/// <summary>
		/// Событие изменения состояния сессии для биржевой площадки.
		/// </summary>
		event Action<ExchangeBoard, SessionStates> SessionStateChanged;

		/// <summary>
		/// Получить состояние сессии для заданной площадки.
		/// </summary>
		/// <param name="board">Биржевая площадка электронных торгов.</param>
		/// <returns>Состояние сессии. Если информация о состоянии сессии отсутствует, то будет возвращено <see langword="null"/>.</returns>
		SessionStates? GetSessionState(ExchangeBoard board);

		/// <summary>
		/// Список всех биржевых площадок, для которых загружены инструменты <see cref="Securities"/>.
		/// </summary>
		IEnumerable<ExchangeBoard> ExchangeBoards { get; }

		/// <summary>
		/// Список всех загруженных инструментов.
		/// Вызывать необходимо после того, как пришло событие <see cref="NewSecurities" />. Иначе будет возвращено постое множество.
		/// </summary>
		IEnumerable<Security> Securities { get; }

		/// <summary>
		/// Получить все заявки.
		/// </summary>
		IEnumerable<Order> Orders { get; }

		/// <summary>
		/// Получить все стоп-заявки.
		/// </summary>
		IEnumerable<Order> StopOrders { get; }

		/// <summary>
		/// Получить все ошибки при регистрации заявок.
		/// </summary>
		IEnumerable<OrderFail> OrderRegisterFails { get; }

		/// <summary>
		/// Получить все ошибки при снятии заявок.
		/// </summary>
		IEnumerable<OrderFail> OrderCancelFails { get; }

		/// <summary>
		/// Получить все сделки.
		/// </summary>
		IEnumerable<Trade> Trades { get; }

		/// <summary>
		/// Получить все собственные сделки.
		/// </summary>
		IEnumerable<MyTrade> MyTrades { get; }

		/// <summary>
		/// Получить все портфели.
		/// </summary>
		IEnumerable<Portfolio> Portfolios { get; }

		/// <summary>
		/// Получить все позиции.
		/// </summary>
		IEnumerable<Position> Positions { get; }

		/// <summary>
		/// Все новости.
		/// </summary>
		IEnumerable<News> News { get; }

		/// <summary>
		/// Состояние соединения.
		/// </summary>
		ConnectionStates ConnectionState { get; }

		/// <summary>
		/// Состояние экспорта.
		/// </summary>
		ConnectionStates ExportState { get; }

		/// <summary>
		/// Поддерживается ли перерегистрация заявок через метод <see cref="ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/>
		/// в виде одной транзакции.
		/// </summary>
		bool IsSupportAtomicReRegister { get; }

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="RegisterSecurity"/>.
		/// </summary>
		IEnumerable<Security> RegisteredSecurities { get; }

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="RegisterMarketDepth"/>.
		/// </summary>
		IEnumerable<Security> RegisteredMarketDepths { get; }

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="RegisterTrades"/>.
		/// </summary>
		IEnumerable<Security> RegisteredTrades { get; }

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="RegisterOrderLog"/>.
		/// </summary>
		IEnumerable<Security> RegisteredOrderLogs { get; }

		/// <summary>
		/// Список всех портфелей, зарегистрированных через <see cref="RegisterPortfolio"/>.
		/// </summary>
		IEnumerable<Portfolio> RegisteredPortfolios { get; }

		/// <summary>
		/// Подключиться к торговой системе.
		/// </summary>
		void Connect();

		/// <summary>
		/// Отключиться от торговой системы.
		/// </summary>
		void Disconnect();

		/// <summary>
		/// Запустить экспорт данных из торговой системы в программу (получение портфелей, инструментов, заявок и т.д.).
		/// </summary>
		void StartExport();

		/// <summary>
		/// Остановить экспорт данных из торговой системы в программу, запущенный через <see cref="StartExport"/>.
		/// </summary>
		void StopExport();

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные инструменты будут переданы через событие <see cref="LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		void LookupSecurities(Security criteria);

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные инструменты будут переданы через событие <see cref="LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">Критерий, поля которого будут использоваться в качестве фильтра.</param>
		void LookupSecurities(SecurityLookupMessage criteria);

		/// <summary>
		/// Найти портфели, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные портфели будут переданы через событие <see cref="LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="criteria">Портфель, поля которого будут использоваться в качестве фильтра.</param>
		void LookupPortfolios(Portfolio criteria);

		/// <summary>
		/// Получить позицию по портфелю и инструменту.
		/// </summary>
		/// <param name="portfolio">Портфель, по которому нужно найти позицию.</param>
		/// <param name="security">Инструмент, по которому нужно найти позицию.</param>
		/// <param name="depoName">Название депозитария, где находится физически ценная бумага.
		/// По-умолчанию передается пустая строка, что означает суммарную позицию по всем депозитариям.</param>
		/// <returns>Позиция.</returns>
		Position GetPosition(Portfolio portfolio, Security security, string depoName = "");

		/// <summary>
		/// Получить отфильтрованный стакан котировок.
		/// </summary>
		/// <param name="security">Инструмент, по которому нужно получить стакан.</param>
		/// <returns>Отфильтрованный стакан котировок.</returns>
		MarketDepth GetFilteredMarketDepth(Security security);

		/// <summary>
		/// Зарегистрировать заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, содержащая информацию для регистрации.</param>
		void RegisterOrder(Order order);

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять.</param>
		/// <param name="newOrder">Новая заявка, которую нужно зарегистрировать.</param>
		void ReRegisterOrder(Order oldOrder, Order newOrder);

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять и на основе нее зарегистрировать новую.</param>
		/// <param name="price">Цена новой заявки.</param>
		/// <param name="volume">Объем новой заявки.</param>
		/// <returns>Новая заявка.</returns>
		Order ReRegisterOrder(Order oldOrder, decimal price, decimal volume);

		/// <summary>
		/// Отменить заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, которую нужно отменить.</param>
		void CancelOrder(Order order);

		/// <summary>
		/// Отменить группу заявок на бирже по фильтру.
		/// </summary>
		/// <param name="isStopOrder"><see langword="true"/>, если нужно отменить только стоп-заявки, false - если только обычный и null - если оба типа.</param>
		/// <param name="portfolio">Портфель. Если значение равно null, то портфель не попадает в фильтр снятия заявок.</param>
		/// <param name="direction">Направление заявки. Если значение равно null, то направление не попадает в фильтр снятия заявок.</param>
		/// <param name="board">Торговая площадка. Если значение равно null, то площадка не попадает в фильтр снятия заявок.</param>
		/// <param name="security">Инструмент. Если значение равно null, то инструмент не попадает в фильтр снятия заявок.</param>
		void CancelOrders(bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null);

		/// <summary>
		/// Подписаться на получение рыночных данных по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		/// <param name="type">Тип рыночных данных.</param>
		void SubscribeMarketData(Security security, MarketDataTypes type);

		/// <summary>
		/// Отписаться от получения рыночных данных по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		/// <param name="type">Тип рыночных данных.</param>
		void UnSubscribeMarketData(Security security, MarketDataTypes type);

		/// <summary>
		/// Начать получать котировки (стакан) по инструменту.
		/// Значение котировок можно получить через событие <see cref="MarketDepthsChanged"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать котировки.</param>
		void RegisterMarketDepth(Security security);

		/// <summary>
		/// Остановить получение котировок по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение котировок.</param>
		void UnRegisterMarketDepth(Security security);

		/// <summary>
		/// Начать получать отфильтрованные котировки (стакан) по инструменту.
		/// Значение котировок можно получить через метод <see cref="IConnector.GetFilteredMarketDepth"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать котировки.</param>
		void RegisterFilteredMarketDepth(Security security);

		/// <summary>
		/// Остановить получение отфильтрованных котировок по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение котировок.</param>
		void UnRegisterFilteredMarketDepth(Security security);

		/// <summary>
		/// Начать получать сделки (тиковые данные) по инструменту. Новые сделки будут приходить через
		/// событие <see cref="NewTrades"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать сделки.</param>
		void RegisterTrades(Security security);

		/// <summary>
		/// Остановить получение сделок (тиковые данные) по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение сделок.</param>
		void UnRegisterTrades(Security security);

		/// <summary>
		/// Начать получать новую информацию (например, <see cref="Security.LastTrade"/> или <see cref="Security.BestBid"/>) по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		void RegisterSecurity(Security security);

		/// <summary>
		/// Остановить получение новой информации.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение новой информации.</param>
		void UnRegisterSecurity(Security security);

		/// <summary>
		/// Начать получать лог заявок для инструмента.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать лог заявок.</param>
		void RegisterOrderLog(Security security);

		/// <summary>
		/// Остановить получение лога заявок для инструмента.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение лога заявок.</param>
		void UnRegisterOrderLog(Security security);

		/// <summary>
		/// Начать получать новую информацию по портфелю.
		/// </summary>
		/// <param name="portfolio">Портфель, по которому необходимо начать получать новую информацию.</param>
		void RegisterPortfolio(Portfolio portfolio);

		/// <summary>
		/// Остановить получение новой информации по портфелю.
		/// </summary>
		/// <param name="portfolio">Портфель, по которому необходимо остановить получение новой информации.</param>
		void UnRegisterPortfolio(Portfolio portfolio);

		/// <summary>
		/// Начать получать новости.
		/// </summary>
		void RegisterNews();

		/// <summary>
		/// Остановить получение новостей.
		/// </summary>
		void UnRegisterNews();

		/// <summary>
		/// Запросить текст новости <see cref="BusinessEntities.News.Story"/>. После получения текста будет вызвано событие <see cref="NewsChanged"/>.
		/// </summary>
		/// <param name="news">Новость.</param>
		void RequestNewsStory(News news);
	}
}