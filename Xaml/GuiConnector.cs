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
	/// The synchronized connection. It wraps an object <see cref="IConnector"/> of the usual connection to all the events are arisen in the GUI thread.
	/// </summary>
	/// <typeparam name="TUnderlyingConnector">The type of connection that should be synchronized.</typeparam>
	public class GuiConnector<TUnderlyingConnector> : BaseLogReceiver, IConnector
		where TUnderlyingConnector : IConnector
	{
		/// <summary>
		/// To create the synchronized connection.
		/// </summary>
		/// <param name="connector">The connection that should be wrapped in <see cref="GuiConnector{T}"/>.</param>
		public GuiConnector(TUnderlyingConnector connector)
		{
			Connector = connector;
		}

		private TUnderlyingConnector _connector;

		/// <summary>
		/// Unsynchronized connection object.
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
				Connector.Error += ErrorHandler;
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
		/// Portfolios received.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> NewPortfolios;

		private void NewPortfoliosHandler(IEnumerable<Portfolio> portfolios)
		{
			AddGuiAction(() => NewPortfolios.SafeInvoke(portfolios));
		}

		#endregion

		#region PortfoliosChanged

		/// <summary>
		/// Portfolios changed.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> PortfoliosChanged;

		private void PortfoliosChangedHandler(IEnumerable<Portfolio> portfolios)
		{
			AddGuiAction(() => PortfoliosChanged.SafeInvoke(portfolios));
		}

		#endregion

		#region NewPositions

		/// <summary>
		/// Positions received.
		/// </summary>
		public event Action<IEnumerable<Position>> NewPositions;

		private void NewPositionsHandler(IEnumerable<Position> positions)
		{
			AddGuiAction(() => NewPositions.SafeInvoke(positions));
		}

		#endregion

		#region PositionsChanged

		/// <summary>
		/// Positions changed.
		/// </summary>
		public event Action<IEnumerable<Position>> PositionsChanged;

		private void PositionsChangedHandler(IEnumerable<Position> positions)
		{
			AddGuiAction(() => PositionsChanged.SafeInvoke(positions));
		}

		#endregion

		#region NewSecurities

		/// <summary>
		/// Securities received.
		/// </summary>
		public event Action<IEnumerable<Security>> NewSecurities;

		private void NewSecuritiesHandler(IEnumerable<Security> securities)
		{
			AddGuiAction(() => NewSecurities.SafeInvoke(securities));
		}

		#endregion

		#region SecuritiesChanged

		/// <summary>
		/// Securities changed.
		/// </summary>
		public event Action<IEnumerable<Security>> SecuritiesChanged;

		private void SecuritiesChangedHandler(IEnumerable<Security> securities)
		{
			AddGuiAction(() => SecuritiesChanged.SafeInvoke(securities));
		}

		#endregion

		#region NewTrades

		/// <summary>
		/// Tick tades received.
		/// </summary>
		public event Action<IEnumerable<Trade>> NewTrades;

		private void NewTradesHandler(IEnumerable<Trade> trades)
		{
			AddGuiAction(() => NewTrades.SafeInvoke(trades));
		}

		#endregion

		#region NewMyTrades

		/// <summary>
		/// Own trades received.
		/// </summary>
		public event Action<IEnumerable<MyTrade>> NewMyTrades;

		private void NewMyTradesHandler(IEnumerable<MyTrade> trades)
		{
			AddGuiAction(() => NewMyTrades.SafeInvoke(trades));
		}

		#endregion

		#region NewOrders

		/// <summary>
		/// Orders received.
		/// </summary>
		public event Action<IEnumerable<Order>> NewOrders;

		private void NewOrdersHandler(IEnumerable<Order> orders)
		{
			AddGuiAction(() => NewOrders.SafeInvoke(orders));
		}

		#endregion

		#region OrdersChanged

		/// <summary>
		/// Orders changed (cancelled, matched).
		/// </summary>
		public event Action<IEnumerable<Order>> OrdersChanged;

		private void OrdersChangedHandler(IEnumerable<Order> orders)
		{
			AddGuiAction(() => OrdersChanged.SafeInvoke(orders));
		}

		#endregion

		#region OrdersRegisterFailed

		/// <summary>
		/// Order registration errors event.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> OrdersRegisterFailed;

		private void OrdersRegisterFailedHandler(IEnumerable<OrderFail> fails)
		{
			AddGuiAction(() => OrdersRegisterFailed.SafeInvoke(fails));
		}

		#endregion

		#region OrdersCancelFailed

		/// <summary>
		/// Order cancellation errors event.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> OrdersCancelFailed;

		private void OrdersCancelFailedHandler(IEnumerable<OrderFail> fails)
		{
			AddGuiAction(() => OrdersCancelFailed.SafeInvoke(fails));
		}

		#endregion

		#region NewStopOrders

		/// <summary>
		/// Stop-orders received.
		/// </summary>
		public event Action<IEnumerable<Order>> NewStopOrders;

		private void NewStopOrdersHandler(IEnumerable<Order> orders)
		{
			AddGuiAction(() => NewStopOrders.SafeInvoke(orders));
		}

		#endregion

		#region StopOrdersChanged

		/// <summary>
		/// Stop-orders received.
		/// </summary>
		public event Action<IEnumerable<Order>> StopOrdersChanged;

		private void StopOrdersChangedHandler(IEnumerable<Order> orders)
		{
			AddGuiAction(() => StopOrdersChanged.SafeInvoke(orders));
		}

		#endregion

		#region StopOrdersRegisterFailed

		/// <summary>
		/// Stop-order registration errors event.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> StopOrdersRegisterFailed;

		private void StopOrdersRegisterFailedHandler(IEnumerable<OrderFail> fails)
		{
			AddGuiAction(() => StopOrdersRegisterFailed.SafeInvoke(fails));
		}

		#endregion

		#region StopOrdersCancelFailed

		/// <summary>
		/// Stop-order cancellation errors event.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> StopOrdersCancelFailed;

		private void StopOrdersCancelFailedHandler(IEnumerable<OrderFail> fails)
		{
			AddGuiAction(() => StopOrdersCancelFailed.SafeInvoke(fails));
		}

		#endregion

		#region NewMarketDepths

		/// <summary>
		/// Order books received.
		/// </summary>
		public event Action<IEnumerable<MarketDepth>> NewMarketDepths;

		private void NewMarketDepthsHandler(IEnumerable<MarketDepth> marketDepths)
		{
			AddGuiAction(() => NewMarketDepths.SafeInvoke(marketDepths));
		}

		#endregion

		#region MarketDepthsChanged

		/// <summary>
		/// Order books changed.
		/// </summary>
		public event Action<IEnumerable<MarketDepth>> MarketDepthsChanged;

		private void MarketDepthsChangedHandler(IEnumerable<MarketDepth> marketDepths)
		{
			AddGuiAction(() => MarketDepthsChanged.SafeInvoke(marketDepths));
		}

		#endregion

		#region NewOrderLogItems

		/// <summary>
		/// Order log received.
		/// </summary>
		public event Action<IEnumerable<OrderLogItem>> NewOrderLogItems;

		private void NewOrderLogItemsHandler(IEnumerable<OrderLogItem> items)
		{
			AddGuiAction(() => NewOrderLogItems.SafeInvoke(items));
		}

		#endregion

		#region NewNews

		/// <summary>
		/// News received.
		/// </summary>
		public event Action<News> NewNews;

		private void NewNewsHandler(News news)
		{
			AddGuiAction(() => NewNews.SafeInvoke(news));
		}

		#endregion

		#region NewsChanged

		/// <summary>
		/// News updated (news body received <see cref="StockSharp.BusinessEntities.News.Story"/>).
		/// </summary>
		public event Action<News> NewsChanged;

		private void NewsChangedHandler(News news)
		{
			AddGuiAction(() => NewsChanged.SafeInvoke(news));
		}

		#endregion

		#region NewMessage

		/// <summary>
		/// Message processed <see cref="Message"/>.
		/// </summary>
		public event Action<Message> NewMessage;

		private void NewMessageHandler(Message message)
		{
			AddGuiAction(() => NewMessage.SafeInvoke(message));
		}

		#endregion

		#region Connected

		/// <summary>
		/// Connected.
		/// </summary>
		public event Action Connected;

		private void ConnectedHandler()
		{
			AddGuiAction(() => Connected.SafeInvoke());
		}

		#endregion

		#region Disconnected

		/// <summary>
		/// Disconnected.
		/// </summary>
		public event Action Disconnected;

		private void DisconnectedHandler()
		{
			AddGuiAction(() => Disconnected.SafeInvoke());
		}

		#endregion

		#region ConnectionError

		/// <summary>
		/// Connection error (for example, the connection was aborted by server).
		/// </summary>
		public event Action<Exception> ConnectionError;

		private void ConnectionErrorHandler(Exception exception)
		{
			AddGuiAction(() => ConnectionError.SafeInvoke(exception));
		}

		#endregion

		#region Error

		/// <summary>
		/// Dats process error.
		/// </summary>
		public event Action<Exception> Error;

		private void ErrorHandler(Exception exception)
		{
			AddGuiAction(() => Error.SafeInvoke(exception));
		}

		#endregion

		#region MarketTimeChanged

		/// <summary>
		/// Server time changed <see cref="IConnector.ExchangeBoards"/>. It passed the time difference since the last call of the event. The first time the event passes the value <see cref="TimeSpan.Zero"/>.
		/// </summary>
		public event Action<TimeSpan> MarketTimeChanged;

		private void MarketTimeChangedHandler(TimeSpan diff)
		{
			AddGuiAction(() => MarketTimeChanged.SafeInvoke(diff));
		}

		#endregion

		#region LookupSecuritiesResult

		/// <summary>
		/// Lookup result <see cref="LookupSecurities(StockSharp.BusinessEntities.Security)"/> received.
		/// </summary>
		public event Action<IEnumerable<Security>> LookupSecuritiesResult;

		private void LookupSecuritiesResultHandler(IEnumerable<Security> securities)
		{
			AddGuiAction(() => LookupSecuritiesResult.SafeInvoke(securities));
		}

		#endregion

		#region LookupPortfoliosResult

		/// <summary>
		/// Lookup result <see cref="LookupPortfolios"/> received.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> LookupPortfoliosResult;

		private void LookupPortfoliosResultHandler(IEnumerable<Portfolio> portfolios)
		{
			AddGuiAction(() => LookupPortfoliosResult.SafeInvoke(portfolios));
		}

		#endregion

		#region MarketDataSubscriptionSucceeded

		/// <summary>
		/// Successful subscription market-data.
		/// </summary>
		public event Action<Security, MarketDataTypes> MarketDataSubscriptionSucceeded;

		private void MarketDataSubscriptionSucceededHandler(Security security, MarketDataTypes type)
		{
			AddGuiAction(() => MarketDataSubscriptionSucceeded.SafeInvoke(security, type));
		}

		#endregion

		#region MarketDataSubscriptionFailed

		/// <summary>
		/// Error subscription market-data.
		/// </summary>
		public event Action<Security, MarketDataTypes, Exception> MarketDataSubscriptionFailed;

		private void MarketDataSubscriptionFailedHandler(Security security, MarketDataTypes type, Exception error)
		{
			AddGuiAction(() => MarketDataSubscriptionFailed.SafeInvoke(security, type, error));
		}

		#endregion

		#region SessionStateChanged

		/// <summary>
		/// Session changed.
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
		/// Get session state for required board.
		/// </summary>
		/// <param name="board">Electronic board.</param>
		/// <returns>Session state. If the information about session state does not exist, then <see langword="null" /> will be returned.</returns>
		public SessionStates? GetSessionState(ExchangeBoard board)
		{
			return Connector.GetSessionState(board);
		}

		/// <summary>
		/// List of all exchange boards, for which instruments are loaded <see cref="IConnector.Securities"/>.
		/// </summary>
		public IEnumerable<ExchangeBoard> ExchangeBoards
		{
			get { return Connector.ExchangeBoards; }
		}

		/// <summary>
		/// List of all loaded instruments. It should be called after event <see cref="IConnector.NewSecurities"/> arisen. Otherwise the empty set will be returned.
		/// </summary>
		public IEnumerable<Security> Securities
		{
			get { return Connector.Securities; }
		}

		/// <summary>
		/// Get all orders.
		/// </summary>
		public IEnumerable<Order> Orders
		{
			get { return Connector.Orders; }
		}

		/// <summary>
		/// Get all stop-orders.
		/// </summary>
		public IEnumerable<Order> StopOrders
		{
			get { return Connector.StopOrders; }
		}

		/// <summary>
		/// Get all registration errors.
		/// </summary>
		public IEnumerable<OrderFail> OrderRegisterFails
		{
			get { return Connector.OrderRegisterFails; }
		}

		/// <summary>
		/// Get all cancellation errors.
		/// </summary>
		public IEnumerable<OrderFail> OrderCancelFails
		{
			get { return Connector.OrderCancelFails; }
		}

		/// <summary>
		/// Get all tick trades.
		/// </summary>
		public IEnumerable<Trade> Trades
		{
			get { return Connector.Trades; }
		}

		/// <summary>
		/// Get all own trades.
		/// </summary>
		public IEnumerable<MyTrade> MyTrades
		{
			get { return Connector.MyTrades; }
		}

		/// <summary>
		/// Get all portfolios.
		/// </summary>
		public IEnumerable<Portfolio> Portfolios
		{
			get { return Connector.Portfolios; }
		}

		/// <summary>
		/// Get all positions.
		/// </summary>
		public IEnumerable<Position> Positions
		{
			get { return Connector.Positions; }
		}

		/// <summary>
		/// All news.
		/// </summary>
		public IEnumerable<News> News
		{
			get { return Connector.News; }
		}

		/// <summary>
		/// Connection state.
		/// </summary>
		public ConnectionStates ConnectionState
		{
			get { return Connector.ConnectionState; }
		}

		/// <summary>
		/// Gets a value indicating whether the re-registration orders via the method <see cref="IConnector.ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/> as a single transaction.
		/// </summary>
		public bool IsSupportAtomicReRegister
		{
			get { return Connector.IsSupportAtomicReRegister; }
		}

		/// <summary>
		/// List of all securities, subscribed via <see cref="IConnector.RegisterSecurity"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredSecurities
		{
			get { return Connector.RegisteredSecurities; }
		}

		/// <summary>
		/// List of all securities, subscribed via <see cref="IConnector.RegisterMarketDepth"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredMarketDepths
		{
			get { return Connector.RegisteredMarketDepths; }
		}

		/// <summary>
		/// List of all securities, subscribed via <see cref="IConnector.RegisterTrades"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredTrades
		{
			get { return Connector.RegisteredTrades; }
		}

		/// <summary>
		/// List of all securities, subscribed via <see cref="IConnector.RegisterOrderLog"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredOrderLogs
		{
			get { return Connector.RegisteredOrderLogs; }
		}

		/// <summary>
		/// List of all portfolios, subscribed via <see cref="IConnector.RegisterPortfolio"/>.
		/// </summary>
		public IEnumerable<Portfolio> RegisteredPortfolios
		{
			get { return Connector.RegisteredPortfolios; }
		}

		/// <summary>
		/// Transactional adapter.
		/// </summary>
		public IMessageAdapter TransactionAdapter
		{
			get { return Connector.TransactionAdapter; }
		}

		/// <summary>
		/// Market-data adapter.
		/// </summary>
		public IMessageAdapter MarketDataAdapter
		{
			get { return Connector.MarketDataAdapter; }
		}

		/// <summary>
		/// Connect to trading system.
		/// </summary>
		public void Connect()
		{
			Connector.Connect();
		}

		/// <summary>
		/// Disconnect from trading system.
		/// </summary>
		public void Disconnect()
		{
			Connector.Disconnect();
		}

		/// <summary>
		/// To find instruments that match the filter <paramref name="criteria" />. Found instruments will be passed through the event <see cref="LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		public void LookupSecurities(Security criteria)
		{
			Connector.LookupSecurities(criteria);
		}

		/// <summary>
		/// To find instruments that match the filter <paramref name="criteria" />. Found instruments will be passed through the event <see cref="IConnector.LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		public void LookupSecurities(SecurityLookupMessage criteria)
		{
			Connector.LookupSecurities(criteria);
		}

		/// <summary>
		/// To find portfolios that match the filter <paramref name="criteria" />. Found portfolios will be passed through the event <see cref="LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="criteria">The portfolio which fields will be used as a filter.</param>
		public void LookupPortfolios(Portfolio criteria)
		{
			Connector.LookupPortfolios(criteria);
		}

		/// <summary>
		/// To get the position by portfolio and instrument.
		/// </summary>
		/// <param name="portfolio">The portfolio on which the position should be found.</param>
		/// <param name="security">The instrument on which the position should be found.</param>
		/// <param name="depoName">The depository name where the stock is located physically. By default, an empty string is passed, which means the total position by all depositories.</param>
		/// <returns>Position.</returns>
		public Position GetPosition(Portfolio portfolio, Security security, string depoName = "")
		{
			return Connector.GetPosition(portfolio, security, depoName);
		}

		/// <summary>
		/// To get the quotes order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Order book.</returns>
		public MarketDepth GetMarketDepth(Security security)
		{
			return Connector.GetMarketDepth(security);
		}

		/// <summary>
		/// Get filtered order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Filtered order book.</returns>
		public MarketDepth GetFilteredMarketDepth(Security security)
		{
			return Connector.GetFilteredMarketDepth(security);
		}

		/// <summary>
		/// To sign up to get market data by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started .</param>
		/// <param name="type">Market data type.</param>
		public void SubscribeMarketData(Security security, MarketDataTypes type)
		{
			Connector.SubscribeMarketData(security, type);
		}

		/// <summary>
		/// To unsubscribe from getting market data by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started .</param>
		/// <param name="type">Market data type.</param>
		public void UnSubscribeMarketData(Security security, MarketDataTypes type)
		{
			Connector.UnSubscribeMarketData(security, type);
		}

		/// <summary>
		/// To start getting quotes (order book) by the instrument. Quotes values are available through the event <see cref="MarketDepthsChanged"/>.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be started.</param>
		public void RegisterMarketDepth(Security security)
		{
			Connector.RegisterMarketDepth(security);
		}

		/// <summary>
		/// To stop getting quotes by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be stopped.</param>
		public void UnRegisterMarketDepth(Security security)
		{
			Connector.UnRegisterMarketDepth(security);
		}

		/// <summary>
		/// To start getting filtered quotes (order book) by the instrument. Quotes values are available through the event <see cref="RegisterMarketDepth"/>.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be started.</param>
		public void RegisterFilteredMarketDepth(Security security)
		{
			Connector.RegisterFilteredMarketDepth(security);
		}

		/// <summary>
		/// To stop getting filtered quotes by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be stopped.</param>
		public void UnRegisterFilteredMarketDepth(Security security)
		{
			Connector.RegisterFilteredMarketDepth(security);
		}

		/// <summary>
		/// To start getting trades (tick data) by the instrument. New trades will come through the event <see cref="IConnector.NewTrades"/>.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be started.</param>
		public void RegisterTrades(Security security)
		{
			Connector.RegisterTrades(security);
		}

		/// <summary>
		/// To stop getting trades (tick data) by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be stopped.</param>
		public void UnRegisterTrades(Security security)
		{
			Connector.UnRegisterTrades(security);
		}

		/// <summary>
		/// Subscribe on the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for subscription.</param>
		public void RegisterPortfolio(Portfolio portfolio)
		{
			Connector.RegisterPortfolio(portfolio);
		}

		/// <summary>
		/// Unsubscribe from the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for unsubscription.</param>
		public void UnRegisterPortfolio(Portfolio portfolio)
		{
			Connector.UnRegisterPortfolio(portfolio);
		}

		/// <summary>
		/// Subscribe on news.
		/// </summary>
		public void RegisterNews()
		{
			Connector.RegisterNews();
		}

		/// <summary>
		/// Unsubscribe from news.
		/// </summary>
		public void UnRegisterNews()
		{
			Connector.UnRegisterNews();
		}

		/// <summary>
		/// Request news <see cref="BusinessEntities.News.Story"/> body. After receiving the event <see cref="IConnector.NewsChanged"/> will be triggered.
		/// </summary>
		/// <param name="news">News.</param>
		public void RequestNewsStory(News news)
		{
			Connector.RequestNewsStory(news);
		}

		/// <summary>
		/// Subscribe on order log for the security.
		/// </summary>
		/// <param name="security">Security for subscription.</param>
		public void RegisterOrderLog(Security security)
		{
			Connector.RegisterOrderLog(security);
		}

		/// <summary>
		/// Unsubscribe from order log for the security.
		/// </summary>
		/// <param name="security">Security for unsubscription.</param>
		public void UnRegisterOrderLog(Security security)
		{
			Connector.UnRegisterOrderLog(security);
		}

		/// <summary>
		/// To start getting new information (for example, <see cref="Security.LastTrade"/> or <see cref="Security.BestBid"/>) by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started .</param>
		public void RegisterSecurity(Security security)
		{
			Connector.RegisterSecurity(security);
		}

		/// <summary>
		/// To stop getting new information.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be stopped.</param>
		public void UnRegisterSecurity(Security security)
		{
			Connector.UnRegisterSecurity(security);
		}

		/// <summary>
		/// Register new order.
		/// </summary>
		/// <param name="order">Registration details.</param>
		public void RegisterOrder(Order order)
		{
			Connector.RegisterOrder(order);
		}

		/// <summary>
		/// Reregister the order.
		/// </summary>
		/// <param name="oldOrder">Cancelling order.</param>
		/// <param name="newOrder">New order to register.</param>
		public void ReRegisterOrder(Order oldOrder, Order newOrder)
		{
			Connector.ReRegisterOrder(oldOrder, newOrder);
		}

		/// <summary>
		/// Reregister the order.
		/// </summary>
		/// <param name="oldOrder">Changing order.</param>
		/// <param name="price">Price of the new order.</param>
		/// <param name="volume">Volume of the new order.</param>
		/// <returns>New order.</returns>
		public Order ReRegisterOrder(Order oldOrder, decimal price, decimal volume)
		{
			return Connector.ReRegisterOrder(oldOrder, price, volume);
		}

		/// <summary>
		/// Cancel the order.
		/// </summary>
		/// <param name="order">Order to cancel.</param>
		public void CancelOrder(Order order)
		{
			Connector.CancelOrder(order);
		}

		/// <summary>
		/// Cancel orders by filter.
		/// </summary>
		/// <param name="isStopOrder"><see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.</param>
		/// <param name="portfolio">Portfolio. If the value is equal to <see langword="null" />, then the portfolio does not match the orders cancel filter.</param>
		/// <param name="direction">Order side. If the value is <see langword="null" />, the direction does not use.</param>
		/// <param name="board">Trading board. If the value is equal to <see langword="null" />, then the board does not match the orders cancel filter.</param>
		/// <param name="security">Instrument. If the value is equal to <see langword="null" />, then the instrument does not match the orders cancel filter.</param>
		public void CancelOrders(bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			Connector.CancelOrders(isStopOrder, portfolio, direction, board, security);
		}

		/// <summary>
		/// Security changed.
		/// </summary>
		public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTime> ValuesChanged;

		private void ValuesChangedHandler(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTime localTime)
		{
			AddGuiAction(() => ValuesChanged.SafeInvoke(security, changes, serverTime, localTime));
		}

		/// <summary>
		/// To get the value of market data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="field">Market-data field.</param>
		/// <returns>The field value. If no data, the <see langword="null" /> will be returned.</returns>
		public object GetSecurityValue(Security security, Level1Fields field)
		{
			return Connector.GetSecurityValue(security, field);
		}

		/// <summary>
		/// To get a set of available fields <see cref="Level1Fields"/>, for which there is a market data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Possible fields.</returns>
		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		{
			return Connector.GetLevel1Fields(security);
		}

		/// <summary>
		/// Lookup securities by criteria <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Found instruments.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			return Connector.Lookup(criteria);
		}

		/// <summary>
		/// Get native id.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Native (internal) trading system security id.</returns>
		public object GetNativeId(Security security)
		{
			return Connector.GetNativeId(security);
		}

		/// <summary>
		/// Release resources.
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
			Connector.Error -= ErrorHandler;
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