#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: IConnector.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The main interface providing the connection to the trading systems.
	/// </summary>
	public interface IConnector : IPersistable, ILogReceiver, IMarketDataProvider, ISecurityProvider, INewsProvider, IPortfolioProvider, IPositionProvider, IMessageSender
	{
		/// <summary>
		/// Own trade received.
		/// </summary>
		event Action<MyTrade> NewMyTrade;

		/// <summary>
		/// Own trades received.
		/// </summary>
		event Action<IEnumerable<MyTrade>> NewMyTrades;

		/// <summary>
		/// Tick trade received.
		/// </summary>
		event Action<Trade> NewTrade;

		/// <summary>
		/// Tick trades received.
		/// </summary>
		event Action<IEnumerable<Trade>> NewTrades;

		/// <summary>
		/// Order received.
		/// </summary>
		event Action<Order> NewOrder;

		/// <summary>
		/// Orders received.
		/// </summary>
		event Action<IEnumerable<Order>> NewOrders;

		/// <summary>
		/// Order changed (cancelled, matched).
		/// </summary>
		event Action<Order> OrderChanged;

		/// <summary>
		/// Orders changed (cancelled, matched).
		/// </summary>
		event Action<IEnumerable<Order>> OrdersChanged;

		/// <summary>
		/// Order registration error event.
		/// </summary>
		event Action<OrderFail> OrderRegisterFailed;

		/// <summary>
		/// Order cancellation error event.
		/// </summary>
		event Action<OrderFail> OrderCancelFailed;

		/// <summary>
		/// Order registration errors event.
		/// </summary>
		event Action<IEnumerable<OrderFail>> OrdersRegisterFailed;

		/// <summary>
		/// Order cancellation errors event.
		/// </summary>
		event Action<IEnumerable<OrderFail>> OrdersCancelFailed;

		/// <summary>
		/// Mass order cancellation event.
		/// </summary>
		event Action<long> MassOrderCanceled;

		/// <summary>
		/// Mass order cancellation errors event.
		/// </summary>
		event Action<long, Exception> MassOrderCancelFailed;

		/// <summary>
		/// Failed order status request event.
		/// </summary>
		event Action<long, Exception> OrderStatusFailed;

		/// <summary>
		/// Stop-order registration errors event.
		/// </summary>
		event Action<IEnumerable<OrderFail>> StopOrdersRegisterFailed;

		/// <summary>
		/// Stop-order cancellation errors event.
		/// </summary>
		event Action<IEnumerable<OrderFail>> StopOrdersCancelFailed;

		/// <summary>
		/// Stop-orders received.
		/// </summary>
		event Action<IEnumerable<Order>> NewStopOrders;

		/// <summary>
		/// Stop orders state change event.
		/// </summary>
		event Action<IEnumerable<Order>> StopOrdersChanged;

		/// <summary>
		/// Stop-order registration error event.
		/// </summary>
		event Action<OrderFail> StopOrderRegisterFailed;

		/// <summary>
		/// Stop-order cancellation error event.
		/// </summary>
		event Action<OrderFail> StopOrderCancelFailed;

		/// <summary>
		/// Stop-order received.
		/// </summary>
		event Action<Order> NewStopOrder;

		/// <summary>
		/// Stop order state change event.
		/// </summary>
		event Action<Order> StopOrderChanged;

		/// <summary>
		/// Security received.
		/// </summary>
		event Action<Security> NewSecurity;

		/// <summary>
		/// Securities received.
		/// </summary>
		event Action<IEnumerable<Security>> NewSecurities;

		/// <summary>
		/// Security changed.
		/// </summary>
		event Action<Security> SecurityChanged;

		/// <summary>
		/// Securities changed.
		/// </summary>
		event Action<IEnumerable<Security>> SecuritiesChanged;

		/// <summary>
		/// Portfolios received.
		/// </summary>
		event Action<IEnumerable<Portfolio>> NewPortfolios;

		///// <summary>
		///// Portfolio changed.
		///// </summary>
		//event Action<Portfolio> PortfolioChanged;

		/// <summary>
		/// Portfolios changed.
		/// </summary>
		event Action<IEnumerable<Portfolio>> PortfoliosChanged;

		///// <summary>
		///// Position received.
		///// </summary>
		//event Action<Position> NewPosition;

		/// <summary>
		/// Positions received.
		/// </summary>
		event Action<IEnumerable<Position>> NewPositions;

		///// <summary>
		///// Position changed.
		///// </summary>
		//event Action<Position> PositionChanged;

		/// <summary>
		/// Positions changed.
		/// </summary>
		event Action<IEnumerable<Position>> PositionsChanged;

		/// <summary>
		/// Order book received.
		/// </summary>
		event Action<MarketDepth> NewMarketDepth;

		/// <summary>
		/// Order book changed.
		/// </summary>
		event Action<MarketDepth> MarketDepthChanged;

		/// <summary>
		/// Order books received.
		/// </summary>
		event Action<IEnumerable<MarketDepth>> NewMarketDepths;

		/// <summary>
		/// Order books changed.
		/// </summary>
		event Action<IEnumerable<MarketDepth>> MarketDepthsChanged;

		/// <summary>
		/// Order log received.
		/// </summary>
		event Action<OrderLogItem> NewOrderLogItem;

		/// <summary>
		/// Order log received.
		/// </summary>
		event Action<IEnumerable<OrderLogItem>> NewOrderLogItems;

		/// <summary>
		/// News received.
		/// </summary>
		event Action<News> NewNews;

		/// <summary>
		/// News updated (news body received <see cref="StockSharp.BusinessEntities.News.Story"/>).
		/// </summary>
		event Action<News> NewsChanged;

		/// <summary>
		/// Message processed <see cref="Message"/>.
		/// </summary>
		event Action<Message> NewMessage;

		/// <summary>
		/// Connected.
		/// </summary>
		event Action Connected;

		/// <summary>
		/// Disconnected.
		/// </summary>
		event Action Disconnected;

		/// <summary>
		/// Connection error (for example, the connection was aborted by server).
		/// </summary>
		event Action<Exception> ConnectionError;

		/// <summary>
		/// Connected.
		/// </summary>
		event Action<IMessageAdapter> ConnectedEx;

		/// <summary>
		/// Disconnected.
		/// </summary>
		event Action<IMessageAdapter> DisconnectedEx;

		/// <summary>
		/// Connection error (for example, the connection was aborted by server).
		/// </summary>
		event Action<IMessageAdapter, Exception> ConnectionErrorEx;

		/// <summary>
		/// Data process error.
		/// </summary>
		event Action<Exception> Error;

		/// <summary>
		/// Server time changed <see cref="IConnector.ExchangeBoards"/>. It passed the time difference since the last call of the event. The first time the event passes the value <see cref="TimeSpan.Zero"/>.
		/// </summary>
		event Action<TimeSpan> MarketTimeChanged;

		/// <summary>
		/// Lookup result <see cref="LookupSecurities(Security,IMessageAdapter,MessageOfflineModes)"/> received.
		/// </summary>
		event Action<Exception, IEnumerable<Security>> LookupSecuritiesResult;

		/// <summary>
		/// Lookup result <see cref="LookupPortfolios(Portfolio,IMessageAdapter,MessageOfflineModes)"/> received.
		/// </summary>
		event Action<Exception, IEnumerable<Portfolio>> LookupPortfoliosResult;

		/// <summary>
		/// Lookup result <see cref="LookupBoards(ExchangeBoard,IMessageAdapter,MessageOfflineModes)"/> received.
		/// </summary>
		event Action<Exception, IEnumerable<ExchangeBoard>> LookupBoardsResult;

		/// <summary>
		/// Successful subscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage> MarketDataSubscriptionSucceeded;

		/// <summary>
		/// Error subscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage, Exception> MarketDataSubscriptionFailed;

		/// <summary>
		/// Successful unsubscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage> MarketDataUnSubscriptionSucceeded;

		/// <summary>
		/// Error unsubscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage, Exception> MarketDataUnSubscriptionFailed;

		/// <summary>
		/// Subscription market-data finished.
		/// </summary>
		event Action<Security, MarketDataFinishedMessage> MarketDataSubscriptionFinished;

		/// <summary>
		/// Session changed.
		/// </summary>
		event Action<ExchangeBoard, SessionStates> SessionStateChanged;

		/// <summary>
		/// Transaction id generator.
		/// </summary>
		IdGenerator TransactionIdGenerator { get; }

		/// <summary>
		/// Get session state for required board.
		/// </summary>
		/// <param name="board">Electronic board.</param>
		/// <returns>Session state. If the information about session state does not exist, then <see langword="null" /> will be returned.</returns>
		SessionStates? GetSessionState(ExchangeBoard board);

		/// <summary>
		/// List of all exchange boards, for which instruments are loaded <see cref="Securities"/>.
		/// </summary>
		IEnumerable<ExchangeBoard> ExchangeBoards { get; }

		/// <summary>
		/// List of all loaded instruments. It should be called after event <see cref="IConnector.NewSecurities"/> arisen. Otherwise the empty set will be returned.
		/// </summary>
		IEnumerable<Security> Securities { get; }

		/// <summary>
		/// Get all orders.
		/// </summary>
		IEnumerable<Order> Orders { get; }

		/// <summary>
		/// Get all stop-orders.
		/// </summary>
		IEnumerable<Order> StopOrders { get; }

		/// <summary>
		/// Get all registration errors.
		/// </summary>
		IEnumerable<OrderFail> OrderRegisterFails { get; }

		/// <summary>
		/// Get all cancellation errors.
		/// </summary>
		IEnumerable<OrderFail> OrderCancelFails { get; }

		/// <summary>
		/// Get all tick trades.
		/// </summary>
		IEnumerable<Trade> Trades { get; }

		/// <summary>
		/// Get all own trades.
		/// </summary>
		IEnumerable<MyTrade> MyTrades { get; }

		///// <summary>
		///// Get all positions.
		///// </summary>
		//IEnumerable<Position> Positions { get; }

		/// <summary>
		/// All news.
		/// </summary>
		IEnumerable<News> News { get; }

		/// <summary>
		/// Connection state.
		/// </summary>
		ConnectionStates ConnectionState { get; }

		///// <summary>
		///// Gets a value indicating whether the re-registration orders via the method <see cref="ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/> as a single transaction.
		///// </summary>
		//bool IsSupportAtomicReRegister { get; }

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterSecurity"/>.
		/// </summary>
		IEnumerable<Security> RegisteredSecurities { get; }

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterMarketDepth"/>.
		/// </summary>
		IEnumerable<Security> RegisteredMarketDepths { get; }

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterTrades"/>.
		/// </summary>
		IEnumerable<Security> RegisteredTrades { get; }

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterOrderLog"/>.
		/// </summary>
		IEnumerable<Security> RegisteredOrderLogs { get; }

		/// <summary>
		/// List of all portfolios, subscribed via <see cref="RegisterPortfolio"/>.
		/// </summary>
		IEnumerable<Portfolio> RegisteredPortfolios { get; }

		/// <summary>
		/// Transactional adapter.
		/// </summary>
		IMessageAdapter TransactionAdapter { get; }

		/// <summary>
		/// Market-data adapter.
		/// </summary>
		IMessageAdapter MarketDataAdapter { get; }

		/// <summary>
		/// Connect to trading system.
		/// </summary>
		void Connect();

		/// <summary>
		/// Disconnect from trading system.
		/// </summary>
		void Disconnect();

		/// <summary>
		/// To find instruments that match the filter <paramref name="criteria" />. Found instruments will be passed through the event <see cref="LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <param name="offlineMode">Offline mode handling message.</param>
		void LookupSecurities(Security criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None);

		/// <summary>
		/// To find instruments that match the filter <paramref name="criteria" />. Found instruments will be passed through the event <see cref="LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		void LookupSecurities(SecurityLookupMessage criteria);

		/// <summary>
		/// Get <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Security ID.</returns>
		SecurityId GetSecurityId(Security security);

		/// <summary>
		/// To find boards that match the filter <paramref name="criteria" />. Found boards will be passed through the event <see cref="LookupBoardsResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <param name="offlineMode">Offline mode handling message.</param>
		void LookupBoards(ExchangeBoard criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None);

		/// <summary>
		/// To find boards that match the filter <paramref name="criteria" />. Found boards will be passed through the event <see cref="LookupBoardsResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		void LookupBoards(BoardLookupMessage criteria);

		/// <summary>
		/// To find portfolios that match the filter <paramref name="criteria" />. Found portfolios will be passed through the event <see cref="LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <param name="offlineMode">Offline mode handling message.</param>
		void LookupPortfolios(Portfolio criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None);

		/// <summary>
		/// To find portfolios that match the filter <paramref name="criteria" />. Found portfolios will be passed through the event <see cref="LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		void LookupPortfolios(PortfolioLookupMessage criteria);

		/// <summary>
		/// To find orders that match the filter <paramref name="criteria" />. Found orders will be passed through the event <see cref="NewOrder"/>.
		/// </summary>
		/// <param name="criteria">The order which fields will be used as a filter.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		void LookupOrders(Order criteria, IMessageAdapter adapter = null);

		/// <summary>
		/// To find orders that match the filter <paramref name="criteria" />. Found orders will be passed through the event <see cref="NewOrder"/>.
		/// </summary>
		/// <param name="criteria">The order which fields will be used as a filter.</param>
		void LookupOrders(OrderStatusMessage criteria);

		/// <summary>
		/// Lookup security by identifier.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Security.</returns>
		Security LookupSecurity(SecurityId securityId);

		/// <summary>
		/// To get the portfolio by the name. If the portfolio is not registered, it will be created.
		/// </summary>
		/// <param name="name">Portfolio name.</param>
		/// <returns>Portfolio.</returns>
		Portfolio GetPortfolio(string name);

		/// <summary>
		/// To get the position by portfolio and instrument.
		/// </summary>
		/// <param name="portfolio">The portfolio on which the position should be found.</param>
		/// <param name="security">The instrument on which the position should be found.</param>
		/// <param name="clientCode">The client code.</param>
		/// <param name="depoName">The depository name where the stock is located physically. By default, an empty string is passed, which means the total position by all depositories.</param>
		/// <returns>Position.</returns>
		Position GetPosition(Portfolio portfolio, Security security, string clientCode = "", string depoName = "");

		/// <summary>
		/// Get filtered order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Filtered order book.</returns>
		MarketDepth GetFilteredMarketDepth(Security security);

		/// <summary>
		/// Register new order.
		/// </summary>
		/// <param name="order">Registration details.</param>
		void RegisterOrder(Order order);

		/// <summary>
		/// Reregister the order.
		/// </summary>
		/// <param name="oldOrder">Cancelling order.</param>
		/// <param name="newOrder">New order to register.</param>
		void ReRegisterOrder(Order oldOrder, Order newOrder);

		/// <summary>
		/// Reregister the order.
		/// </summary>
		/// <param name="oldOrder">Changing order.</param>
		/// <param name="price">Price of the new order.</param>
		/// <param name="volume">Volume of the new order.</param>
		/// <returns>New order.</returns>
		Order ReRegisterOrder(Order oldOrder, decimal price, decimal volume);

		/// <summary>
		/// Cancel the order.
		/// </summary>
		/// <param name="order">The order which should be canceled.</param>
		void CancelOrder(Order order);

		/// <summary>
		/// Cancel orders by filter.
		/// </summary>
		/// <param name="isStopOrder"><see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.</param>
		/// <param name="portfolio">Portfolio. If the value is equal to <see langword="null" />, then the portfolio does not match the orders cancel filter.</param>
		/// <param name="direction">Order side. If the value is <see langword="null" />, the direction does not use.</param>
		/// <param name="board">Trading board. If the value is equal to <see langword="null" />, then the board does not match the orders cancel filter.</param>
		/// <param name="security">Instrument. If the value is equal to <see langword="null" />, then the instrument does not match the orders cancel filter.</param>
		/// <param name="securityType">Security type. If the value is <see langword="null" />, the type does not use.</param>
		/// <param name="transactionId">Order cancellation transaction id.</param>
		void CancelOrders(bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null, SecurityTypes? securityType = null, long? transactionId = null);

		/// <summary>
		/// To sign up to get market data by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started.</param>
		/// <param name="message">The message that contain subscribe info.</param>
		void SubscribeMarketData(Security security, MarketDataMessage message);

		/// <summary>
		/// To unsubscribe from getting market data by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started.</param>
		/// <param name="message">The message that contain unsubscribe info.</param>
		void UnSubscribeMarketData(Security security, MarketDataMessage message);

		/// <summary>
		/// To start getting quotes (order book) by the instrument. Quotes values are available through the event <see cref="IConnector.MarketDepthsChanged"/>.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		/// <param name="buildMode">Build mode.</param>
		/// <param name="buildFrom">Which market-data type is used as a source value.</param>
		/// <param name="maxDepth">Max depth of requested order book.</param>
		void RegisterMarketDepth(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, int? maxDepth = null);

		/// <summary>
		/// To stop getting quotes by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be stopped.</param>
		void UnRegisterMarketDepth(Security security);

		/// <summary>
		/// To start getting filtered quotes (order book) by the instrument. Quotes values are available through the event <see cref="GetFilteredMarketDepth"/>.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be started.</param>
		void RegisterFilteredMarketDepth(Security security);

		/// <summary>
		/// To stop getting filtered quotes by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be stopped.</param>
		void UnRegisterFilteredMarketDepth(Security security);

		/// <summary>
		/// To start getting trades (tick data) by the instrument. New trades will come through the event <see cref="IConnector.NewTrades"/>.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		/// <param name="buildMode">Build mode.</param>
		/// <param name="buildFrom">Which market-data type is used as a source value.</param>
		void RegisterTrades(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null);

		/// <summary>
		/// To stop getting trades (tick data) by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be stopped.</param>
		void UnRegisterTrades(Security security);

		/// <summary>
		/// To start getting new information (for example, <see cref="Security.LastTrade"/> or <see cref="Security.BestBid"/>) by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		/// <param name="buildMode">Build mode.</param>
		/// <param name="buildFrom">Which market-data type is used as a source value.</param>
		void RegisterSecurity(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null);

		/// <summary>
		/// To stop getting new information.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be stopped.</param>
		void UnRegisterSecurity(Security security);

		/// <summary>
		/// Subscribe on order log for the security.
		/// </summary>
		/// <param name="security">Security for subscription.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		void RegisterOrderLog(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null);

		/// <summary>
		/// Unsubscribe from order log for the security.
		/// </summary>
		/// <param name="security">Security for unsubscription.</param>
		void UnRegisterOrderLog(Security security);

		/// <summary>
		/// Subscribe on the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for subscription.</param>
		void RegisterPortfolio(Portfolio portfolio);

		/// <summary>
		/// Unsubscribe from the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for unsubscription.</param>
		void UnRegisterPortfolio(Portfolio portfolio);

		/// <summary>
		/// Subscribe on news.
		/// </summary>
		void RegisterNews();

		/// <summary>
		/// Unsubscribe from news.
		/// </summary>
		void UnRegisterNews();

		/// <summary>
		/// Subscribe on the board changes.
		/// </summary>
		/// <param name="board">Board for subscription.</param>
		void SubscribeBoard(ExchangeBoard board);

		/// <summary>
		/// Unsubscribe from the board changes.
		/// </summary>
		/// <param name="board">Board for unsubscription.</param>
		void UnSubscribeBoard(ExchangeBoard board);
	}
}