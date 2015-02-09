namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Синхронизованное подключение. Оборачивает объект <see cref="IConnector"/> обычного подключения для того, чтобы все события приходили в GUI потоке.
	/// </summary>
	/// <typeparam name="TUnderlyingConnector">Тип подключения, которое необходимо синхронизовать.</typeparam>
	public class GuiConnector<TUnderlyingConnector> : BaseLogReceiver, IConnector
		where TUnderlyingConnector : IConnector
	{
		/// <summary>
		/// Создать синхронизованное подключение.
		/// </summary>
		/// <param name="connector">Подключение, которое необходимо обернуть в <see cref="GuiConnector{T}"/>.</param>
		public GuiConnector(TUnderlyingConnector connector)
		{
			Connector = connector;
		}

		private TUnderlyingConnector _connector;

		/// <summary>
		/// Несинхронизованный объект подключения.
		/// </summary>
		public TUnderlyingConnector Connector
		{
			get { return _connector; }
			private set
			{
				if (value.IsNull())
					throw new ArgumentNullException();

				_connector = value;

				Connector.NewPortfolios += NewPortfoliosHandler;
				Connector.PortfoliosChanged += PortfoliosChangedHandler;
				Connector.NewPositions += NewPositionsHandler;
				Connector.PositionsChanged += PositionsChangedHandler;
				Connector.NewSecurities += NewSecuritiesHandler;
				Connector.SecuritiesChanged += SecuritiesChangedHandler;
				Connector.NewTrades += NewTradesHandler;
				Connector.NewMyTrades += NewMyTradesHandler;
				Connector.NewOrders += NewOrdersHandler;
				Connector.OrdersChanged += OrdersChangedHandler;
				Connector.OrdersRegisterFailed += OrdersRegisterFailedHandler;
				Connector.OrdersCancelFailed += OrdersCancelFailedHandler;
				Connector.NewStopOrders += NewStopOrdersHandler;
				Connector.StopOrdersChanged += StopOrdersChangedHandler;
				Connector.StopOrdersRegisterFailed += StopOrdersRegisterFailedHandler;
				Connector.StopOrdersCancelFailed += StopOrdersCancelFailedHandler;
				Connector.NewMarketDepths += NewMarketDepthsHandler;
				Connector.MarketDepthsChanged += MarketDepthsChangedHandler;
				Connector.NewOrderLogItems += NewOrderLogItemsHandler;
				Connector.NewNews += NewNewsHandler;
				Connector.NewsChanged += NewsChangedHandler;
				Connector.NewMessage += NewMessageHandler;
				Connector.Connected += ConnectedHandler;
				Connector.Disconnected += DisconnectedHandler;
				Connector.ConnectionError += ConnectionErrorHandler;
				Connector.ExportStarted += ExportStartedHandler;
				Connector.ExportStopped += ExportStoppedHandler;
				Connector.ExportError += ExportErrorHandler;
				Connector.NewDataExported += NewDataExportedHandler;
				Connector.ProcessDataError += ProcessDataErrorHandler;
				Connector.MarketTimeChanged += MarketTimeChangedHandler;
				Connector.LookupSecuritiesResult += LookupSecuritiesResultHandler;
				Connector.LookupPortfoliosResult += LookupPortfoliosResultHandler;
				Connector.MarketDataSubscriptionSucceeded += MarketDataSubscriptionSucceededHandler;
				Connector.MarketDataSubscriptionFailed += MarketDataSubscriptionFailedHandler;
				Connector.SessionStateChanged += SessionStateChangedHandler;
				Connector.ValuesChanged += ValuesChangedHandler;
			}
		}

		#region NewPortfolios

		/// <summary>
		/// Событие появления новых портфелей.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> NewPortfolios;

		private void NewPortfoliosHandler(IEnumerable<Portfolio> portfolios)
		{
			AddGuiAction(() => NewPortfolios.SafeInvoke(portfolios));
		}

		#endregion

		#region PortfoliosChanged

		/// <summary>
		/// Событие изменения параметров портфелей.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> PortfoliosChanged;

		private void PortfoliosChangedHandler(IEnumerable<Portfolio> portfolios)
		{
			AddGuiAction(() => PortfoliosChanged.SafeInvoke(portfolios));
		}

		#endregion

		#region NewPositions

		/// <summary>
		/// Событие появления новых позиций.
		/// </summary>
		public event Action<IEnumerable<Position>> NewPositions;

		private void NewPositionsHandler(IEnumerable<Position> positions)
		{
			AddGuiAction(() => NewPositions.SafeInvoke(positions));
		}

		#endregion

		#region PositionsChanged

		/// <summary>
		/// Событие изменения параметров позиций.
		/// </summary>
		public event Action<IEnumerable<Position>> PositionsChanged;

		private void PositionsChangedHandler(IEnumerable<Position> positions)
		{
			AddGuiAction(() => PositionsChanged.SafeInvoke(positions));
		}

		#endregion

		#region NewSecurities

		/// <summary>
		/// Событие появления новых инструментов.
		/// </summary>
		public event Action<IEnumerable<Security>> NewSecurities;

		private void NewSecuritiesHandler(IEnumerable<Security> securities)
		{
			AddGuiAction(() => NewSecurities.SafeInvoke(securities));
		}

		#endregion

		#region SecuritiesChanged

		/// <summary>
		/// Событие изменения параметров инструментов.
		/// </summary>
		public event Action<IEnumerable<Security>> SecuritiesChanged;

		private void SecuritiesChangedHandler(IEnumerable<Security> securities)
		{
			AddGuiAction(() => SecuritiesChanged.SafeInvoke(securities));
		}

		#endregion

		#region NewTrades

		/// <summary>
		/// Событие появления всех новых сделок.
		/// </summary>
		public event Action<IEnumerable<Trade>> NewTrades;

		private void NewTradesHandler(IEnumerable<Trade> trades)
		{
			AddGuiAction(() => NewTrades.SafeInvoke(trades));
		}

		#endregion

		#region NewMyTrades

		/// <summary>
		/// Событие появления собственных новых сделок.
		/// </summary>
		public event Action<IEnumerable<MyTrade>> NewMyTrades;

		private void NewMyTradesHandler(IEnumerable<MyTrade> trades)
		{
			AddGuiAction(() => NewMyTrades.SafeInvoke(trades));
		}

		#endregion

		#region NewOrders

		/// <summary>
		/// Событие появления новых заявок.
		/// </summary>
		public event Action<IEnumerable<Order>> NewOrders;

		private void NewOrdersHandler(IEnumerable<Order> orders)
		{
			AddGuiAction(() => NewOrders.SafeInvoke(orders));
		}

		#endregion

		#region OrdersChanged

		/// <summary>
		/// Событие изменения состояния заявок (снята, удовлетворена).
		/// </summary>
		public event Action<IEnumerable<Order>> OrdersChanged;

		private void OrdersChangedHandler(IEnumerable<Order> orders)
		{
			AddGuiAction(() => OrdersChanged.SafeInvoke(orders));
		}

		#endregion

		#region OrdersRegisterFailed

		/// <summary>
		/// Событие об ошибках, связанных с регистрацией заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> OrdersRegisterFailed;

		private void OrdersRegisterFailedHandler(IEnumerable<OrderFail> fails)
		{
			AddGuiAction(() => OrdersRegisterFailed.SafeInvoke(fails));
		}

		#endregion

		#region OrdersCancelFailed

		/// <summary>
		/// Событие об ошибках, связанных со снятием заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> OrdersCancelFailed;

		private void OrdersCancelFailedHandler(IEnumerable<OrderFail> fails)
		{
			AddGuiAction(() => OrdersCancelFailed.SafeInvoke(fails));
		}

		#endregion

		#region NewStopOrders

		/// <summary>
		/// Событие появления новых стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<Order>> NewStopOrders;

		private void NewStopOrdersHandler(IEnumerable<Order> orders)
		{
			AddGuiAction(() => NewStopOrders.SafeInvoke(orders));
		}

		#endregion

		#region StopOrdersChanged

		/// <summary>
		/// Событие появления новых стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<Order>> StopOrdersChanged;

		private void StopOrdersChangedHandler(IEnumerable<Order> orders)
		{
			AddGuiAction(() => StopOrdersChanged.SafeInvoke(orders));
		}

		#endregion

		#region StopOrdersRegisterFailed

		/// <summary>
		/// Событие об ошибках, связанных с регистрацией стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> StopOrdersRegisterFailed;

		private void StopOrdersRegisterFailedHandler(IEnumerable<OrderFail> fails)
		{
			AddGuiAction(() => StopOrdersRegisterFailed.SafeInvoke(fails));
		}

		#endregion

		#region StopOrdersCancelFailed

		/// <summary>
		/// Событие об ошибках, связанных со снятием стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> StopOrdersCancelFailed;

		private void StopOrdersCancelFailedHandler(IEnumerable<OrderFail> fails)
		{
			AddGuiAction(() => StopOrdersCancelFailed.SafeInvoke(fails));
		}

		#endregion

		#region NewMarketDepths

		/// <summary>
		/// Событие появления новых стаканов с котировками.
		/// </summary>
		public event Action<IEnumerable<MarketDepth>> NewMarketDepths;

		private void NewMarketDepthsHandler(IEnumerable<MarketDepth> marketDepths)
		{
			AddGuiAction(() => NewMarketDepths.SafeInvoke(marketDepths));
		}

		#endregion

		#region MarketDepthsChanged

		/// <summary>
		/// Событие изменения стаканов с котировками.
		/// </summary>
		public event Action<IEnumerable<MarketDepth>> MarketDepthsChanged;

		private void MarketDepthsChangedHandler(IEnumerable<MarketDepth> marketDepths)
		{
			AddGuiAction(() => MarketDepthsChanged.SafeInvoke(marketDepths));
		}

		#endregion

		#region NewOrderLogItems

		/// <summary>
		/// Событие появления новых записей в логе заявок.
		/// </summary>
		public event Action<IEnumerable<OrderLogItem>> NewOrderLogItems;

		private void NewOrderLogItemsHandler(IEnumerable<OrderLogItem> items)
		{
			AddGuiAction(() => NewOrderLogItems.SafeInvoke(items));
		}

		#endregion

		#region NewNews

		/// <summary>
		/// Событие появления новости.
		/// </summary>
		public event Action<News> NewNews;

		private void NewNewsHandler(News news)
		{
			AddGuiAction(() => NewNews.SafeInvoke(news));
		}

		#endregion

		#region NewsChanged

		/// <summary>
		/// Событие изменения новости (например, при скачивании текста <see cref="StockSharp.BusinessEntities.News.Story"/>).
		/// </summary>
		public event Action<News> NewsChanged;

		private void NewsChangedHandler(News news)
		{
			AddGuiAction(() => NewsChanged.SafeInvoke(news));
		}

		#endregion

		#region NewMessage

		/// <summary>
		/// Событие обработки нового сообщения <see cref="Message"/>.
		/// </summary>
		public event Action<Message, MessageDirections> NewMessage;

		private void NewMessageHandler(Message message, MessageDirections direction)
		{
			AddGuiAction(() => NewMessage.SafeInvoke(message, direction));
		}

		#endregion

		#region Connected

		/// <summary>
		/// Событие успешного подключения.
		/// </summary>
		public event Action Connected;

		private void ConnectedHandler()
		{
			AddGuiAction(() => Connected.SafeInvoke());
		}

		#endregion

		#region Disconnected

		/// <summary>
		/// Событие успешного отключения.
		/// </summary>
		public event Action Disconnected;

		private void DisconnectedHandler()
		{
			AddGuiAction(() => Disconnected.SafeInvoke());
		}

		#endregion

		#region ConnectionError

		/// <summary>
		/// Событие ошибки подключения (например, соединения было разорвано).
		/// </summary>
		public event Action<Exception> ConnectionError;

		private void ConnectionErrorHandler(Exception exception)
		{
			AddGuiAction(() => ConnectionError.SafeInvoke(exception));
		}

		#endregion

		#region NewDataExported

		/// <summary>
		/// Событие, сигнализирующее о новых экспортируемых данных.
		/// </summary>
		public event Action NewDataExported;

		private void NewDataExportedHandler()
		{
			AddGuiAction(() => NewDataExported.SafeInvoke());
		}

		#endregion

		#region ProcessDataError

		/// <summary>
		/// Событие, сигнализирующее об ошибке при получении или обработке новых данных с сервера.
		/// </summary>
		public event Action<Exception> ProcessDataError;

		private void ProcessDataErrorHandler(Exception exception)
		{
			AddGuiAction(() => ProcessDataError.SafeInvoke(exception));
		}

		#endregion

		#region MarketTimeChanged

		/// <summary>
		/// Событие, сигнализирующее об изменении текущего времени на биржевых площадках <see cref="IConnector.ExchangeBoards"/>.
		/// Передается разница во времени, прошедшее с последнего вызова события. Первый раз событие передает значение <see cref="TimeSpan.Zero"/>.
		/// </summary>
		public event Action<TimeSpan> MarketTimeChanged;

		private void MarketTimeChangedHandler(TimeSpan diff)
		{
			AddGuiAction(() => MarketTimeChanged.SafeInvoke(diff));
		}

		#endregion

		#region LookupSecuritiesResult

		/// <summary>
		/// Событие, передающее результат поиска, запущенного через метод <see cref="LookupSecurities(StockSharp.BusinessEntities.Security)"/>.
		/// </summary>
		public event Action<IEnumerable<Security>> LookupSecuritiesResult;

		private void LookupSecuritiesResultHandler(IEnumerable<Security> securities)
		{
			AddGuiAction(() => LookupSecuritiesResult.SafeInvoke(securities));
		}

		#endregion

		#region LookupPortfoliosResult

		/// <summary>
		/// Событие, передающее результат поиска, запущенного через метод <see cref="LookupPortfolios"/>.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> LookupPortfoliosResult;

		private void LookupPortfoliosResultHandler(IEnumerable<Portfolio> portfolios)
		{
			AddGuiAction(() => LookupPortfoliosResult.SafeInvoke(portfolios));
		}

		#endregion

		#region MarketDataSubscriptionSucceeded

		/// <summary>
		/// Событие успешной регистрации инструмента для получения маркет-данных.
		/// </summary>
		public event Action<Security, MarketDataTypes> MarketDataSubscriptionSucceeded;

		private void MarketDataSubscriptionSucceededHandler(Security security, MarketDataTypes type)
		{
			AddGuiAction(() => MarketDataSubscriptionSucceeded.SafeInvoke(security, type));
		}

		#endregion

		#region MarketDataSubscriptionFailed

		/// <summary>
		/// Событие ошибки регистрации инструмента для получения маркет-данных.
		/// </summary>
		public event Action<Security, MarketDataTypes, Exception> MarketDataSubscriptionFailed;

		private void MarketDataSubscriptionFailedHandler(Security security, MarketDataTypes type, Exception error)
		{
			AddGuiAction(() => MarketDataSubscriptionFailed.SafeInvoke(security, type, error));
		}

		#endregion

		#region ExportStarted

		/// <summary>
		/// Событие успешного запуска экспорта.
		/// </summary>
		public event Action ExportStarted;

		private void ExportStartedHandler()
		{
			AddGuiAction(() => ExportStarted.SafeInvoke());
		}

		#endregion

		#region ExportStopped

		/// <summary>
		/// Событие успешной остановки экспорта.
		/// </summary>
		public event Action ExportStopped;

		private void ExportStoppedHandler()
		{
			AddGuiAction(() => ExportStopped.SafeInvoke());
		}

		#endregion

		#region ExportError

		/// <summary>
		/// Событие ошибки экспорта (например, соединения было разорвано).
		/// </summary>
		public event Action<Exception> ExportError;

		private void ExportErrorHandler(Exception exception)
		{
			AddGuiAction(() => ExportError.SafeInvoke(exception));
		}

		#endregion

		#region SessionStateChanged

		/// <summary>
		/// Событие изменения состояния сессии для биржевой площадки.
		/// </summary>
		public event Action<ExchangeBoard, SessionStates> SessionStateChanged;

		private void SessionStateChangedHandler(ExchangeBoard board, SessionStates state)
		{
			AddGuiAction(() => SessionStateChanged.SafeInvoke(board, state));
		}

		#endregion

		private static void AddGuiAction(Action action)
		{
			GuiDispatcher.GlobalDispatcher.AddAction(action);
		}

		/// <summary>
		/// Получить состояние сессии для заданной площадки.
		/// </summary>
		/// <param name="board">Биржевая площадка электронных торгов.</param>
		/// <returns>Состояние сессии. Если информация о состоянии сессии отсутствует, то будет возвращено <see langword="null"/>.</returns>
		public SessionStates? GetSessionState(ExchangeBoard board)
		{
			return Connector.GetSessionState(board);
		}

		/// <summary>
		/// Список всех биржевых площадок, для которых загружены инструменты <see cref="IConnector.Securities"/>.
		/// </summary>
		public IEnumerable<ExchangeBoard> ExchangeBoards
		{
			get { return Connector.ExchangeBoards; }
		}

		/// <summary>
		/// Список всех загруженных инструментов.
		/// Вызывать необходимо после того, как пришло событие <see cref="IConnector.NewSecurities" />. Иначе будет возвращено постое множество.
		/// </summary>
		public IEnumerable<Security> Securities
		{
			get { return Connector.Securities; }
		}

		/// <summary>
		/// Получить все заявки.
		/// </summary>
		public IEnumerable<Order> Orders
		{
			get { return Connector.Orders; }
		}

		/// <summary>
		/// Получить все стоп-заявки.
		/// </summary>
		public IEnumerable<Order> StopOrders
		{
			get { return Connector.StopOrders; }
		}

		/// <summary>
		/// Получить все ошибки при регистрации заявок.
		/// </summary>
		public IEnumerable<OrderFail> OrderRegisterFails
		{
			get { return Connector.OrderRegisterFails; }
		}

		/// <summary>
		/// Получить все ошибки при снятии заявок.
		/// </summary>
		public IEnumerable<OrderFail> OrderCancelFails
		{
			get { return Connector.OrderCancelFails; }
		}

		/// <summary>
		/// Получить все сделки.
		/// </summary>
		public IEnumerable<Trade> Trades
		{
			get { return Connector.Trades; }
		}

		/// <summary>
		/// Получить все собственные сделки.
		/// </summary>
		public IEnumerable<MyTrade> MyTrades
		{
			get { return Connector.MyTrades; }
		}

		/// <summary>
		/// Получить все портфели.
		/// </summary>
		public IEnumerable<Portfolio> Portfolios
		{
			get { return Connector.Portfolios; }
		}

		/// <summary>
		/// Получить все позиции.
		/// </summary>
		public IEnumerable<Position> Positions
		{
			get { return Connector.Positions; }
		}

		/// <summary>
		/// Все новости.
		/// </summary>
		public IEnumerable<News> News
		{
			get { return Connector.News; }
		}

		/// <summary>
		/// Состояние соединения.
		/// </summary>
		public ConnectionStates ConnectionState
		{
			get { return Connector.ConnectionState; }
		}

		/// <summary>
		/// Состояние экспорта.
		/// </summary>
		public ConnectionStates ExportState
		{
			get { return Connector.ExportState; }
		}

		/// <summary>
		/// Поддерживается ли перерегистрация заявок через метод <see cref="IConnector.ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/>
		/// в виде одной транзакции.
		/// </summary>
		public bool IsSupportAtomicReRegister
		{
			get { return Connector.IsSupportAtomicReRegister; }
		}

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="IConnector.RegisterSecurity"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredSecurities
		{
			get { return Connector.RegisteredSecurities; }
		}

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="IConnector.RegisterMarketDepth"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredMarketDepths
		{
			get { return Connector.RegisteredMarketDepths; }
		}

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="IConnector.RegisterTrades"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredTrades
		{
			get { return Connector.RegisteredTrades; }
		}

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="IConnector.RegisterOrderLog"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredOrderLogs
		{
			get { return Connector.RegisteredOrderLogs; }
		}

		/// <summary>
		/// Список всех портфелей, зарегистрированных через <see cref="IConnector.RegisterPortfolio"/>.
		/// </summary>
		public IEnumerable<Portfolio> RegisteredPortfolios
		{
			get { return Connector.RegisteredPortfolios; }
		}

		/// <summary>
		/// Подключиться к торговой системе.
		/// </summary>
		public void Connect()
		{
			Connector.Connect();
		}

		/// <summary>
		/// Отключиться от торговой системы.
		/// </summary>
		public void Disconnect()
		{
			Connector.Disconnect();
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные инструменты будут переданы через событие <see cref="LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		public void LookupSecurities(Security criteria)
		{
			Connector.LookupSecurities(criteria);
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные инструменты будут переданы через событие <see cref="IConnector.LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">Критерий, поля которого будут использоваться в качестве фильтра.</param>
		public void LookupSecurities(SecurityLookupMessage criteria)
		{
			Connector.LookupSecurities(criteria);
		}

		/// <summary>
		/// Найти портфели, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные портфели будут переданы через событие <see cref="LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="criteria">Портфель, поля которого будут использоваться в качестве фильтра.</param>
		public void LookupPortfolios(Portfolio criteria)
		{
			Connector.LookupPortfolios(criteria);
		}

		/// <summary>
		/// Получить позицию по портфелю и инструменту.
		/// </summary>
		/// <param name="portfolio">Портфель, по которому нужно найти позицию.</param>
		/// <param name="security">Инструмент, по которому нужно найти позицию.</param>
		/// <param name="depoName">Название депозитария, где находится физически ценная бумага.
		/// По-умолчанию передается пустая строка, что означает суммарную позицию по всем депозитариям.</param>
		/// <returns>Позиция.</returns>
		public Position GetPosition(Portfolio portfolio, Security security, string depoName = "")
		{
			return Connector.GetPosition(portfolio, security, depoName);
		}

		/// <summary>
		/// Получить стакан котировок.
		/// </summary>
		/// <param name="security">Инструмент, по которому нужно получить стакан.</param>
		/// <returns>Стакан котировок.</returns>
		public MarketDepth GetMarketDepth(Security security)
		{
			return Connector.GetMarketDepth(security);
		}

		/// <summary>
		/// Получить отфильтрованный стакан котировок.
		/// </summary>
		/// <param name="security">Инструмент, по которому нужно получить стакан.</param>
		/// <returns>Отфильтрованный стакан котировок.</returns>
		public MarketDepth GetFilteredMarketDepth(Security security)
		{
			return Connector.GetFilteredMarketDepth(security);
		}

		/// <summary>
		/// Подписаться на получение рыночных данных по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		/// <param name="type">Тип рыночных данных.</param>
		public void SubscribeMarketData(Security security, MarketDataTypes type)
		{
			Connector.SubscribeMarketData(security, type);
		}

		/// <summary>
		/// Отписаться от получения рыночных данных по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		/// <param name="type">Тип рыночных данных.</param>
		public void UnSubscribeMarketData(Security security, MarketDataTypes type)
		{
			Connector.UnSubscribeMarketData(security, type);
		}

		/// <summary>
		/// Начать получать котировки (стакан) по инструменту.
		/// Значение котировок можно получить через событие <see cref="MarketDepthsChanged"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать котировки.</param>
		public void RegisterMarketDepth(Security security)
		{
			Connector.RegisterMarketDepth(security);
		}

		/// <summary>
		/// Остановить получение котировок по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение котировок.</param>
		public void UnRegisterMarketDepth(Security security)
		{
			Connector.UnRegisterMarketDepth(security);
		}

		/// <summary>
		/// Начать получать отфильтрованные котировки (стакан) по инструменту.
		/// Значение котировок можно получить через метод <see cref="IConnector.GetFilteredMarketDepth"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать котировки.</param>
		public void RegisterFilteredMarketDepth(Security security)
		{
			Connector.RegisterFilteredMarketDepth(security);
		}

		/// <summary>
		/// Остановить получение отфильтрованных котировок по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение котировок.</param>
		public void UnRegisterFilteredMarketDepth(Security security)
		{
			Connector.RegisterFilteredMarketDepth(security);
		}

		/// <summary>
		/// Начать получать сделки (тиковые данные) по инструменту. Новые сделки будут приходить через
		/// событие <see cref="IConnector.NewTrades"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать сделки.</param>
		public void RegisterTrades(Security security)
		{
			Connector.RegisterTrades(security);
		}

		/// <summary>
		/// Остановить получение сделок (тиковые данные) по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение сделок.</param>
		public void UnRegisterTrades(Security security)
		{
			Connector.UnRegisterTrades(security);
		}

		/// <summary>
		/// Начать получать новую информацию по портфелю.
		/// </summary>
		/// <param name="portfolio">Портфель, по которому необходимо начать получать новую информацию.</param>
		public void RegisterPortfolio(Portfolio portfolio)
		{
			Connector.RegisterPortfolio(portfolio);
		}

		/// <summary>
		/// Остановить получение новой информации по портфелю.
		/// </summary>
		/// <param name="portfolio">Портфель, по которому необходимо остановить получение новой информации.</param>
		public void UnRegisterPortfolio(Portfolio portfolio)
		{
			Connector.UnRegisterPortfolio(portfolio);
		}

		/// <summary>
		/// Начать получать новости.
		/// </summary>
		public void RegisterNews()
		{
			Connector.RegisterNews();
		}

		/// <summary>
		/// Остановить получение новостей.
		/// </summary>
		public void UnRegisterNews()
		{
			Connector.UnRegisterNews();
		}

		/// <summary>
		/// Запросить текст новости <see cref="BusinessEntities.News.Story"/>. После получения текста будет вызвано событие <see cref="IConnector.NewsChanged"/>.
		/// </summary>
		/// <param name="news">Новость.</param>
		public void RequestNewsStory(News news)
		{
			Connector.RequestNewsStory(news);
		}

		/// <summary>
		/// Начать получать лог заявок для инструмента.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать лог заявок.</param>
		public void RegisterOrderLog(Security security)
		{
			Connector.RegisterOrderLog(security);
		}

		/// <summary>
		/// Остановить получение лога заявок для инструмента.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение лога заявок.</param>
		public void UnRegisterOrderLog(Security security)
		{
			Connector.UnRegisterOrderLog(security);
		}

		/// <summary>
		/// Начать получать новую информацию (например, <see cref="Security.LastTrade"/> или <see cref="Security.BestBid"/>) по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		public void RegisterSecurity(Security security)
		{
			Connector.RegisterSecurity(security);
		}

		/// <summary>
		/// Остановить получение новой информации.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение новой информации.</param>
		public void UnRegisterSecurity(Security security)
		{
			Connector.UnRegisterSecurity(security);
		}

		/// <summary>
		/// Зарегистрировать заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, содержащая информацию для регистрации.</param>
		public void RegisterOrder(Order order)
		{
			Connector.RegisterOrder(order);
		}

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять.</param>
		/// <param name="newOrder">Новая заявка, которую нужно зарегистрировать.</param>
		public void ReRegisterOrder(Order oldOrder, Order newOrder)
		{
			Connector.ReRegisterOrder(oldOrder, newOrder);
		}

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять и на основе нее зарегистрировать новую.</param>
		/// <param name="price">Цена новой заявки.</param>
		/// <param name="volume">Объем новой заявки.</param>
		/// <returns>Новая заявка.</returns>
		public Order ReRegisterOrder(Order oldOrder, decimal price, decimal volume)
		{
			return Connector.ReRegisterOrder(oldOrder, price, volume);
		}

		/// <summary>
		/// Отменить заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, которую нужно отменять.</param>
		public void CancelOrder(Order order)
		{
			Connector.CancelOrder(order);
		}

		/// <summary>
		/// Отменить группу заявок на бирже по фильтру.
		/// </summary>
		/// <param name="isStopOrder"><see langword="true"/>, если нужно отменить только стоп-заявки, false - если только обычный и null - если оба типа.</param>
		/// <param name="portfolio">Портфель. Если значение равно null, то портфель не попадает в фильтр снятия заявок.</param>
		/// <param name="direction">Направление заявки. Если значение равно null, то направление не попадает в фильтр снятия заявок.</param>
		/// <param name="board">Торговая площадка. Если значение равно null, то площадка не попадает в фильтр снятия заявок.</param>
		/// <param name="security">Инструмент. Если значение равно null, то инструмент не попадает в фильтр снятия заявок.</param>
		public void CancelOrders(bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			Connector.CancelOrders(isStopOrder, portfolio, direction, board, security);
		}

		/// <summary>
		/// Запустить экспорт данных из торговой системы в программу (получение портфелей, инструментов, заявок и т.д.).
		/// </summary>
		public void StartExport()
		{
			Connector.StartExport();
		}

		/// <summary>
		/// Остановить экспорт данных из торговой системы в программу, запущенный через <see cref="IConnector.StartExport"/>.
		/// </summary>
		public void StopExport()
		{
			Connector.StopExport();
		}

		/// <summary>
		/// Событие изменения инструмента.
		/// </summary>
		public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTime> ValuesChanged;

		private void ValuesChangedHandler(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTime localTime)
		{
			AddGuiAction(() => ValuesChanged.SafeInvoke(security, changes, serverTime, localTime));
		}

		/// <summary>
		/// Получить значение маркет-данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="field">Поле маркет-данных.</param>
		/// <returns>Значение поля. Если данных нет, то будет возвращено <see langword="null"/>.</returns>
		public object GetSecurityValue(Security security, Level1Fields field)
		{
			return Connector.GetSecurityValue(security, field);
		}

		/// <summary>
		/// Получить набор доступных полей <see cref="Level1Fields"/>, для которых есть маркет-данные для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Набор доступных полей.</returns>
		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		{
			return Connector.GetLevel1Fields(security);
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Найденные инструменты.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			return Connector.Lookup(criteria);
		}

		/// <summary>
		/// Получить внутренний идентификатор торговой системы.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Внутренний идентификатор торговой системы.</returns>
		public object GetNativeId(Security security)
		{
			return Connector.GetNativeId(security);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			Connector.NewSecurities -= NewSecuritiesHandler;
			Connector.SecuritiesChanged -= SecuritiesChangedHandler;
			Connector.NewPortfolios -= NewPortfoliosHandler;
			Connector.PortfoliosChanged -= PortfoliosChangedHandler;
			Connector.NewPositions -= NewPositionsHandler;
			Connector.PositionsChanged -= PositionsChangedHandler;
			Connector.NewTrades -= NewTradesHandler;
			Connector.NewMyTrades -= NewMyTradesHandler;
			Connector.NewOrders -= NewOrdersHandler;
			Connector.OrdersChanged -= OrdersChangedHandler;
			Connector.OrdersRegisterFailed -= OrdersRegisterFailedHandler;
			Connector.OrdersCancelFailed -= OrdersCancelFailedHandler;
			Connector.NewStopOrders -= NewStopOrdersHandler;
			Connector.StopOrdersChanged -= StopOrdersChangedHandler;
			Connector.StopOrdersRegisterFailed -= StopOrdersRegisterFailedHandler;
			Connector.StopOrdersCancelFailed -= StopOrdersCancelFailedHandler;
			Connector.NewMarketDepths -= NewMarketDepthsHandler;
			Connector.MarketDepthsChanged -= MarketDepthsChangedHandler;
			Connector.NewOrderLogItems -= NewOrderLogItemsHandler;
			Connector.NewNews -= NewNewsHandler;
			Connector.NewsChanged -= NewsChangedHandler;
			Connector.Connected -= ConnectedHandler;
			Connector.Disconnected -= DisconnectedHandler;
			Connector.ConnectionError -= ConnectionErrorHandler;
			Connector.ExportStarted -= ExportStartedHandler;
			Connector.ExportStopped -= ExportStoppedHandler;
			Connector.ExportError -= ExportErrorHandler;
			Connector.NewDataExported -= NewDataExportedHandler;
			Connector.ProcessDataError -= ProcessDataErrorHandler;
			Connector.MarketTimeChanged -= MarketTimeChangedHandler;
			Connector.LookupSecuritiesResult -= LookupSecuritiesResultHandler;
			Connector.LookupPortfoliosResult -= LookupPortfoliosResultHandler;
			Connector.MarketDataSubscriptionSucceeded -= MarketDataSubscriptionSucceededHandler;
			Connector.MarketDataSubscriptionFailed -= MarketDataSubscriptionFailedHandler;
			Connector.SessionStateChanged -= SessionStateChangedHandler;
			Connector.ValuesChanged -= ValuesChangedHandler;

			base.DisposeManaged();
		}
	}
}