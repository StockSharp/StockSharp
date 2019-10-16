#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: IMarketDataProvider.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

	/// <summary>
	/// The market data by the instrument provider interface.
	/// </summary>
	public interface IMarketDataProvider
	{
		/// <summary>
		/// Security changed.
		/// </summary>
		event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> ValuesChanged;

		/// <summary>
		/// To get the quotes order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Order book.</returns>
		MarketDepth GetMarketDepth(Security security);

		/// <summary>
		/// To get the value of market data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="field">Market-data field.</param>
		/// <returns>The field value. If no data, the <see langword="null" /> will be returned.</returns>
		object GetSecurityValue(Security security, Level1Fields field);

		/// <summary>
		/// To get a set of available fields <see cref="Level1Fields"/>, for which there is a market data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Possible fields.</returns>
		IEnumerable<Level1Fields> GetLevel1Fields(Security security);

		/// <summary>
		/// Tick trade received.
		/// </summary>
		event Action<Trade> NewTrade;

		/// <summary>
		/// Security received.
		/// </summary>
		event Action<Security> NewSecurity;

		/// <summary>
		/// Security changed.
		/// </summary>
		event Action<Security> SecurityChanged;

		/// <summary>
		/// Order book received.
		/// </summary>
		event Action<MarketDepth> NewMarketDepth;

		/// <summary>
		/// Order book changed.
		/// </summary>
		event Action<MarketDepth> MarketDepthChanged;

		/// <summary>
		/// Order log received.
		/// </summary>
		event Action<OrderLogItem> NewOrderLogItem;

		/// <summary>
		/// News received.
		/// </summary>
		event Action<News> NewNews;

		/// <summary>
		/// News updated (news body received <see cref="News.Story"/>).
		/// </summary>
		event Action<News> NewsChanged;

		/// <summary>
		/// Lookup result <see cref="LookupSecurities"/> received.
		/// </summary>
		event Action<SecurityLookupMessage, IEnumerable<Security>, Exception> LookupSecuritiesResult;

		/// <summary>
		/// Lookup result <see cref="LookupSecurities"/> received.
		/// </summary>
		event Action<SecurityLookupMessage, IEnumerable<Security>, IEnumerable<Security>, Exception> LookupSecuritiesResult2;

		/// <summary>
		/// Lookup result <see cref="LookupBoards"/> received.
		/// </summary>
		event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, Exception> LookupBoardsResult;

		/// <summary>
		/// Lookup result <see cref="LookupBoards"/> received.
		/// </summary>
		event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, IEnumerable<ExchangeBoard>, Exception> LookupBoardsResult2;

		/// <summary>
		/// Successful subscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage> MarketDataSubscriptionSucceeded;

		/// <summary>
		/// Error subscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage, Exception> MarketDataSubscriptionFailed;

		/// <summary>
		/// Error subscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage, MarketDataMessage> MarketDataSubscriptionFailed2;

		/// <summary>
		/// Successful unsubscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage> MarketDataUnSubscriptionSucceeded;

		/// <summary>
		/// Error unsubscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage, Exception> MarketDataUnSubscriptionFailed;

		/// <summary>
		/// Error unsubscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage, MarketDataMessage> MarketDataUnSubscriptionFailed2;

		/// <summary>
		/// Subscription market-data finished.
		/// </summary>
		event Action<Security, MarketDataFinishedMessage> MarketDataSubscriptionFinished;

		/// <summary>
		/// Market-data subscription unexpected cancelled.
		/// </summary>
		event Action<Security, MarketDataMessage, Exception> MarketDataUnexpectedCancelled;

		/// <summary>
		/// To find instruments that match the filter <paramref name="criteria" />. Found instruments will be passed through the event <see cref="LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		void LookupSecurities(SecurityLookupMessage criteria);

		/// <summary>
		/// To find boards that match the filter <paramref name="criteria" />. Found boards will be passed through the event <see cref="LookupBoardsResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		void LookupBoards(BoardLookupMessage criteria);

		/// <summary>
		/// Get filtered order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Filtered order book.</returns>
		MarketDepth GetFilteredMarketDepth(Security security);

		/// <summary>
		/// To subscribe to get market data by the instrument.
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
		/// To subscribe to get market data.
		/// </summary>
		/// <param name="message">The message that contain subscribe info.</param>
		void SubscribeMarketData(MarketDataMessage message);

		/// <summary>
		/// To unsubscribe from getting market data.
		/// </summary>
		/// <param name="message">The message that contain unsubscribe info.</param>
		void UnSubscribeMarketData(MarketDataMessage message);

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
		void SubscribeMarketDepth(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, int? maxDepth = null, IMessageAdapter adapter = null);

		/// <summary>
		/// To stop getting quotes by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be stopped.</param>
		void UnSubscribeMarketDepth(Security security);

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
		void SubscribeTrades(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, IMessageAdapter adapter = null);

		/// <summary>
		/// To stop getting trades (tick data) by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be stopped.</param>
		void UnSubscribeTrades(Security security);

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
		void SubscribeLevel1(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, IMessageAdapter adapter = null);

		/// <summary>
		/// To stop getting new information.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be stopped.</param>
		void UnSubscribeLevel1(Security security);

		/// <summary>
		/// Subscribe on order log for the security.
		/// </summary>
		/// <param name="security">Security for subscription.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		void SubscribeOrderLog(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null);

		/// <summary>
		/// Unsubscribe from order log for the security.
		/// </summary>
		/// <param name="security">Security for unsubscription.</param>
		void UnSubscribeOrderLog(Security security);

		/// <summary>
		/// Subscribe on news.
		/// </summary>
		/// <param name="security">Security for subscription.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		void SubscribeNews(Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null);

		/// <summary>
		/// Unsubscribe from news.
		/// </summary>
		/// <param name="security">Security for subscription.</param>
		void UnSubscribeNews(Security security = null);

		/// <summary>
		/// Subscribe on the board changes.
		/// </summary>
		/// <param name="board">Board for subscription.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		void SubscribeBoard(ExchangeBoard board, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null);

		/// <summary>
		/// Unsubscribe from the board changes.
		/// </summary>
		/// <param name="board">Board for unsubscription.</param>
		void UnSubscribeBoard(ExchangeBoard board);
	}
}