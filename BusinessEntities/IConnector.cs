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
	public interface IConnector : IPersistable, ILogReceiver, IMarketDataProvider, ITransactionProvider, ISecurityProvider, INewsProvider, IPortfolioProvider, IPositionProvider, IMessageSender
	{
		/// <summary>
		/// Own trades received.
		/// </summary>
		event Action<IEnumerable<MyTrade>> NewMyTrades;

		/// <summary>
		/// Tick trades received.
		/// </summary>
		event Action<IEnumerable<Trade>> NewTrades;

		/// <summary>
		/// Orders received.
		/// </summary>
		event Action<IEnumerable<Order>> NewOrders;

		/// <summary>
		/// Orders changed (cancelled, matched).
		/// </summary>
		event Action<IEnumerable<Order>> OrdersChanged;

		/// <summary>
		/// Order registration errors event.
		/// </summary>
		event Action<IEnumerable<OrderFail>> OrdersRegisterFailed;

		/// <summary>
		/// Order cancellation errors event.
		/// </summary>
		event Action<IEnumerable<OrderFail>> OrdersCancelFailed;

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
		/// Securities received.
		/// </summary>
		event Action<IEnumerable<Security>> NewSecurities;

		/// <summary>
		/// Securities changed.
		/// </summary>
		event Action<IEnumerable<Security>> SecuritiesChanged;

		/// <summary>
		/// Portfolios received.
		/// </summary>
		event Action<IEnumerable<Portfolio>> NewPortfolios;

		/// <summary>
		/// Portfolios changed.
		/// </summary>
		event Action<IEnumerable<Portfolio>> PortfoliosChanged;

		/// <summary>
		/// Positions received.
		/// </summary>
		event Action<IEnumerable<Position>> NewPositions;

		/// <summary>
		/// Positions changed.
		/// </summary>
		event Action<IEnumerable<Position>> PositionsChanged;

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
		/// Data process error.
		/// </summary>
		event Action<Exception> Error;

		/// <summary>
		/// Server time changed <see cref="IConnector.ExchangeBoards"/>. It passed the time difference since the last call of the event. The first time the event passes the value <see cref="TimeSpan.Zero"/>.
		/// </summary>
		event Action<TimeSpan> MarketTimeChanged;

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
		/// List of all securities, subscribed via <see cref="IMarketDataProvider.RegisterSecurity"/>.
		/// </summary>
		IEnumerable<Security> RegisteredSecurities { get; }

		/// <summary>
		/// List of all securities, subscribed via <see cref="IMarketDataProvider.RegisterMarketDepth"/>.
		/// </summary>
		IEnumerable<Security> RegisteredMarketDepths { get; }

		/// <summary>
		/// List of all securities, subscribed via <see cref="IMarketDataProvider.RegisterTrades"/>.
		/// </summary>
		IEnumerable<Security> RegisteredTrades { get; }

		/// <summary>
		/// List of all securities, subscribed via <see cref="IMarketDataProvider.RegisterOrderLog"/>.
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
		/// To find instruments that match the filter <paramref name="criteria" />. Found instruments will be passed through the event <see cref="IMarketDataProvider.LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <param name="offlineMode">Offline mode handling message.</param>
		void LookupSecurities(Security criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None);

		/// <summary>
		/// Get <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Security ID.</returns>
		SecurityId GetSecurityId(Security security);

		/// <summary>
		/// To find boards that match the filter <paramref name="criteria" />. Found boards will be passed through the event <see cref="IMarketDataProvider.LookupBoardsResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <param name="offlineMode">Offline mode handling message.</param>
		void LookupBoards(ExchangeBoard criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None);

		/// <summary>
		/// To find portfolios that match the filter <paramref name="criteria" />. Found portfolios will be passed through the event <see cref="ITransactionProvider.LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <param name="offlineMode">Offline mode handling message.</param>
		void LookupPortfolios(Portfolio criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None);

		/// <summary>
		/// To find orders that match the filter <paramref name="criteria" />. Found orders will be passed through the event <see cref="ITransactionProvider.NewOrder"/>.
		/// </summary>
		/// <param name="criteria">The order which fields will be used as a filter.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		void LookupOrders(Order criteria, IMessageAdapter adapter = null);

		/// <summary>
		/// Get security by identifier.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Security.</returns>
		Security GetSecurity(SecurityId securityId);

		/// <summary>
		/// The order was initialized and ready to send for registration.
		/// </summary>
		event Action<Order> OrderInitialized;
	}
}