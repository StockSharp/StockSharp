namespace StockSharp.Algo
{
	using System;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The market data by the instrument provider interface.
	/// </summary>
	public interface IMarketDataProviderEx : IMarketDataProvider, ISubscriptionProvider
	{
		/// <summary>
		/// To subscribe to get market data by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started.</param>
		/// <param name="message">The message that contain subscribe info.</param>
		/// <returns>Subscription.</returns>
		Subscription SubscribeMarketData(Security security, MarketDataMessage message);

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
		/// <returns>Subscription.</returns>
		Subscription SubscribeMarketData(MarketDataMessage message);

		/// <summary>
		/// To unsubscribe from getting market data.
		/// </summary>
		/// <param name="message">The message that contain unsubscribe info.</param>
		void UnSubscribeMarketData(MarketDataMessage message);

		/// <summary>
		/// To start getting filtered quotes (order book) by the instrument. Quotes values are available through the event <see cref="IMarketDataProvider.FilteredMarketDepthChanged"/>.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be started.</param>
		/// <returns>Subscription.</returns>
		Subscription SubscribeFilteredMarketDepth(Security security);

		/// <summary>
		/// To start getting quotes (order book) by the instrument. Quotes values are available through the event <see cref="IMarketDataProvider.MarketDepthChanged"/>.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		/// <param name="buildMode">Build mode.</param>
		/// <param name="buildFrom">Which market-data type is used as a source value.</param>
		/// <param name="maxDepth">Max depth of requested order book.</param>
		/// <param name="refreshSpeed">Interval for data refresh.</param>
		/// <param name="depthBuilder">Order log to market depth builder.</param>
		/// <param name="passThroughOrderBookInrement">Pass through incremental <see cref="QuoteChangeMessage"/>.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <returns>Subscription.</returns>
		Subscription SubscribeMarketDepth(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, int? maxDepth = null, TimeSpan? refreshSpeed = null, IOrderLogMarketDepthBuilder depthBuilder = null, bool passThroughOrderBookInrement = false, IMessageAdapter adapter = null);

		/// <summary>
		/// To stop getting quotes by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be stopped.</param>
		void UnSubscribeMarketDepth(Security security);

		/// <summary>
		/// To start getting trades (tick data) by the instrument. New trades will come through the event <see cref="IMarketDataProvider.NewTrade"/>.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		/// <param name="buildMode">Build mode.</param>
		/// <param name="buildFrom">Which market-data type is used as a source value.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <returns>Subscription.</returns>
		Subscription SubscribeTrades(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, IMessageAdapter adapter = null);

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
		/// <returns>Subscription.</returns>
		Subscription SubscribeLevel1(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, IMessageAdapter adapter = null);

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
		/// <returns>Subscription.</returns>
		Subscription SubscribeOrderLog(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null);

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
		/// <returns>Subscription.</returns>
		Subscription SubscribeNews(Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null);

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
		/// <returns>Subscription.</returns>
		Subscription SubscribeBoard(ExchangeBoard board, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null);

		/// <summary>
		/// Unsubscribe from the board changes.
		/// </summary>
		/// <param name="board">Board for unsubscription.</param>
		void UnSubscribeBoard(ExchangeBoard board);

		/// <summary>
		/// Unsubscribe.
		/// </summary>
		/// <param name="subscriptionId">Subscription id.</param>
		void UnSubscribe(long subscriptionId);
	}
}