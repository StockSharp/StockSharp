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

	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The main interface providing the connection to the trading systems.
	/// </summary>
	public interface IConnector : IPersistable, ILogReceiver, IMarketDataProvider, ITransactionProvider, ISecurityProvider, INewsProvider, IMessageChannel
	{
		/// <summary>
		/// Own trades received.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<MyTrade>> NewMyTrades;

		/// <summary>
		/// Tick trades received.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<Trade>> NewTrades;

		/// <summary>
		/// Orders received.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<Order>> NewOrders;

		/// <summary>
		/// Orders changed (cancelled, matched).
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<Order>> OrdersChanged;

		/// <summary>
		/// Order registration errors event.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<OrderFail>> OrdersRegisterFailed;

		/// <summary>
		/// Order cancellation errors event.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<OrderFail>> OrdersCancelFailed;

		/// <summary>
		/// Stop-order registration errors event.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<OrderFail>> StopOrdersRegisterFailed;

		/// <summary>
		/// Stop-order cancellation errors event.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<OrderFail>> StopOrdersCancelFailed;

		/// <summary>
		/// Stop-orders received.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<Order>> NewStopOrders;

		/// <summary>
		/// Stop orders state change event.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<Order>> StopOrdersChanged;

		/// <summary>
		/// Securities received.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<Security>> NewSecurities;

		/// <summary>
		/// Securities changed.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<Security>> SecuritiesChanged;

		/// <summary>
		/// Portfolios received.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<Portfolio>> NewPortfolios;

		/// <summary>
		/// Portfolios changed.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<Portfolio>> PortfoliosChanged;

		/// <summary>
		/// Positions received.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<Position>> NewPositions;

		/// <summary>
		/// Positions changed.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<Position>> PositionsChanged;

		/// <summary>
		/// Order books received.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<MarketDepth>> NewMarketDepths;

		/// <summary>
		/// Order books changed.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<MarketDepth>> MarketDepthsChanged;

		/// <summary>
		/// Order log received.
		/// </summary>
		[Obsolete("Use single item event overload.")]
		event Action<IEnumerable<OrderLogItem>> NewOrderLogItems;

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
		/// Connection lost.
		/// </summary>
		event Action<IMessageAdapter> ConnectionLost;

		/// <summary>
		/// Connection restored.
		/// </summary>
		event Action<IMessageAdapter> ConnectionRestored;

		/// <summary>
		/// Data process error.
		/// </summary>
		event Action<Exception> Error;

		/// <summary>
		/// Change password result.
		/// </summary>
		event Action<long, Exception> ChangePasswordResult;

		/// <summary>
		/// Server time changed <see cref="IConnector.ExchangeBoards"/>. It passed the time difference since the last call of the event. The first time the event passes the value <see cref="TimeSpan.Zero"/>.
		/// </summary>
		event Action<TimeSpan> MarketTimeChanged;

		/// <summary>
		/// Session changed.
		/// </summary>
		event Action<ExchangeBoard, SessionStates> SessionStateChanged;

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
		[Obsolete("Use NewOrder event to collect data.")]
		IEnumerable<Order> Orders { get; }

		/// <summary>
		/// Get all stop-orders.
		/// </summary>
		[Obsolete("Use NewStopOrder event to collect data.")]
		IEnumerable<Order> StopOrders { get; }

		/// <summary>
		/// Get all registration errors.
		/// </summary>
		[Obsolete("Use OrderRegisterFailed event to collect data.")]
		IEnumerable<OrderFail> OrderRegisterFails { get; }

		/// <summary>
		/// Get all cancellation errors.
		/// </summary>
		[Obsolete("Use OrderCancelFailed event to collect data.")]
		IEnumerable<OrderFail> OrderCancelFails { get; }

		/// <summary>
		/// Get all tick trades.
		/// </summary>
		[Obsolete("Use NewTrade event to collect data.")]
		IEnumerable<Trade> Trades { get; }

		/// <summary>
		/// Get all own trades.
		/// </summary>
		[Obsolete("Use NewMyTrade event to collect data.")]
		IEnumerable<MyTrade> MyTrades { get; }

		///// <summary>
		///// Get all positions.
		///// </summary>
		//IEnumerable<Position> Positions { get; }

		/// <summary>
		/// All news.
		/// </summary>
		[Obsolete("Use NewNews event to collect data.")]
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
		/// List of all securities, subscribed via <see cref="RegisteredMarketDepths"/>.
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
		/// List of all portfolios, subscribed via <see cref="ITransactionProvider.RegisterPortfolio"/>.
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
		/// Get <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Security ID.</returns>
		SecurityId GetSecurityId(Security security);

		/// <summary>
		/// Get security by identifier.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Security.</returns>
		Security GetSecurity(SecurityId securityId);

		/// <summary>
		/// Send outgoing message.
		/// </summary>
		/// <param name="message">Message.</param>
		void SendOutMessage(Message message);

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
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		[Obsolete("Use SubscribeMarketDepth method instead.")]
		void RegisterMarketDepth(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, int? maxDepth = null, IMessageAdapter adapter = null);

		/// <summary>
		/// To stop getting quotes by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be stopped.</param>
		[Obsolete("Use UnSubscribeMarketDepth method instead.")]
		void UnRegisterMarketDepth(Security security);

		/// <summary>
		/// To start getting trades (tick data) by the instrument. New trades will come through the event <see cref="IConnector.NewTrades"/>.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		/// <param name="buildMode">Build mode.</param>
		/// <param name="buildFrom">Which market-data type is used as a source value.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		[Obsolete("Use SubscribeTrades method instead.")]
		void RegisterTrades(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, IMessageAdapter adapter = null);

		/// <summary>
		/// To stop getting trades (tick data) by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be stopped.</param>
		[Obsolete("Use UnSubscribeTrades method instead.")]
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
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		[Obsolete("Use SubscribeLevel1 method instead.")]
		void RegisterSecurity(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, IMessageAdapter adapter = null);

		/// <summary>
		/// To stop getting new information.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be stopped.</param>
		[Obsolete("Use UnSubscribeLevel1 method instead.")]
		void UnRegisterSecurity(Security security);

		/// <summary>
		/// Subscribe on order log for the security.
		/// </summary>
		/// <param name="security">Security for subscription.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		[Obsolete("Use SubscribeOrderLog method instead.")]
		void RegisterOrderLog(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null);

		/// <summary>
		/// Unsubscribe from order log for the security.
		/// </summary>
		/// <param name="security">Security for unsubscription.</param>
		[Obsolete("Use UnSubscribeOrderLog method instead.")]
		void UnRegisterOrderLog(Security security);

		/// <summary>
		/// Subscribe on news.
		/// </summary>
		/// <param name="security">Security for subscription.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		[Obsolete("Use SubscribeNews method instead.")]
		void RegisterNews(Security security = null, IMessageAdapter adapter = null);

		/// <summary>
		/// Unsubscribe from news.
		/// </summary>
		/// <param name="security">Security for subscription.</param>
		[Obsolete("Use UnSubscribeNews method instead.")]
		void UnRegisterNews(Security security = null);
	}
}