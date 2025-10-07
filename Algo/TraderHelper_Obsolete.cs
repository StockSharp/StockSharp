namespace StockSharp.Algo;

using StockSharp.Algo.Candles;

partial class TraderHelper
{
	/// <summary>
	/// Subscribe on orders changes.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">Security for subscription.</param>
	/// <param name="from">The initial date from which you need to get data.</param>
	/// <param name="to">The final date by which you need to get data.</param>
	/// <param name="count">Max count.</param>
	/// <param name="states">Filter order by the specified states.</param>
	/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
	/// <param name="skip">Skip count.</param>
	/// <param name="fillGaps"><see cref="FillGapsDays"/></param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeOrders(this ISubscriptionProvider provider, Security security = default, DateTimeOffset? from = default, DateTimeOffset? to = default, long? count = default, IEnumerable<OrderStates> states = default, IMessageAdapter adapter = default, long? skip = default, FillGapsDays? fillGaps = default)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		var message = new OrderStatusMessage
		{
			IsSubscribe = true,
			SecurityId = security?.ToSecurityId() ?? default,
			From = from,
			To = to,
			Adapter = adapter,
			Count = count,
			Skip = skip,
			FillGaps = fillGaps,
		};

		if (states != null)
			message.States = [.. states];

		var subscription = new Subscription(message);
		provider.Subscribe(subscription);
		return subscription;
	}

	/// <summary>
	/// Subscribe on positions changes.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">The instrument on which the position should be found.</param>
	/// <param name="portfolio">The portfolio on which the position should be found.</param>
	/// <param name="from">The initial date from which you need to get data.</param>
	/// <param name="to">The final date by which you need to get data.</param>
	/// <param name="count">Max count.</param>
	/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
	/// <param name="skip">Skip count.</param>
	/// <param name="fillGaps"><see cref="FillGapsDays"/></param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribePositions(this ISubscriptionProvider provider, Security security = default, Portfolio portfolio = default, DateTimeOffset? from = default, DateTimeOffset? to = default, long? count = default, IMessageAdapter adapter = default, long? skip = default, FillGapsDays? fillGaps = default)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		var subscription = new Subscription(new PortfolioLookupMessage
		{
			Adapter = adapter,
			IsSubscribe = true,
			SecurityId = security?.ToSecurityId(),
			PortfolioName = portfolio?.Name,
			From = from,
			To = to,
			Count = count,
			Skip = skip,
			FillGaps = fillGaps,
		});

		provider.Subscribe(subscription);
		return subscription;
	}

	/// <summary>
	/// To find instruments that match the filter <paramref name="criteria" />. Found instruments will be passed through the event <see cref="IMarketDataProvider.LookupSecuritiesResult"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="criteria">The criterion which fields will be used as a filter.</param>
	/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
	/// <param name="offlineMode">Offline mode handling message.</param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription LookupSecurities(this ISubscriptionProvider provider, Security criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
	{
		var msg = criteria.ToLookupMessage();
		
		msg.Adapter = adapter;
		msg.OfflineMode = offlineMode;

		return provider.LookupSecurities(msg);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription LookupOrders(this ISubscriptionProvider provider, Order criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
	{
		var msg = criteria.ToLookupCriteria(null, null);

		msg.Adapter = adapter;
		msg.OfflineMode = offlineMode;

		return provider.LookupOrders(msg);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeCandles(this ISubscriptionProvider provider,
		CandleSeries series, DateTimeOffset? from = default, DateTimeOffset? to = default,
		long? count = default, long? transactionId = default, IMessageAdapter adapter = default,
		long? skip = default, FillGapsDays? fillGaps = default)
	{
		return SubscribeCandles(provider, series.Security, series.ToDataType(), from, to, count, transactionId, adapter, skip, fillGaps);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeCandles(this ISubscriptionProvider provider,
		Security security, DataType dataType, DateTimeOffset? from = default, DateTimeOffset? to = default,
		long? count = default, long? transactionId = default, IMessageAdapter adapter = default,
		long? skip = default, FillGapsDays? fillGaps = default)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		if (provider is ILogReceiver logs)
			logs.LogDebug(nameof(SubscribeCandles));

		var subscription = new Subscription(dataType, security);

		var mdMsg = subscription.MarketData;

		if (from != null)
			mdMsg.From = from.Value;

		if (to != null)
			mdMsg.To = to.Value;

		if (count != null)
			mdMsg.Count = count.Value;

		if (skip != null)
			mdMsg.Skip = skip.Value;

		if (fillGaps != null)
			mdMsg.FillGaps = fillGaps;

		mdMsg.Adapter = adapter;

		if (transactionId != null)
			subscription.TransactionId = transactionId.Value;

		provider.Subscribe(subscription);
		return subscription;
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribeCandles(this ISubscriptionProvider provider, CandleSeries series)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		if (series is null)
			throw new ArgumentNullException(nameof(series));

		var subscription = provider.Subscriptions.FirstOrDefault(s => s.CandleSeries == series);

		if (subscription is null)
		{
			if (provider is ILogReceiver logs)
				logs.AddWarningLog(LocalizedStrings.SubscriptionNonExist, series);
		}
		else
			provider.UnSubscribe(subscription);
	}

	/// <summary>
	/// To find portfolios that match the filter <paramref name="criteria" />. Found portfolios will be passed through the event <see cref="ITransactionProvider.LookupPortfoliosResult"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="criteria">The criterion which fields will be used as a filter.</param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribePositions(this ISubscriptionProvider provider, PortfolioLookupMessage criteria)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		var subscription = new Subscription(criteria);
		provider.Subscribe(subscription);
		return subscription;
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribePositions(this ISubscriptionProvider provider, long originalTransactionId = 0)
	{
		var subscription = provider.TryGetSubscription(originalTransactionId, DataType.PositionChanges, null);

		if (subscription != null)
			provider.UnSubscribe(subscription);
	}

	/// <summary>
	/// To find instruments that match the filter <paramref name="criteria" />. Found instruments will be passed through the event <see cref="IMarketDataProvider.LookupSecuritiesResult"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="criteria">The criterion which fields will be used as a filter.</param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription LookupSecurities(this ISubscriptionProvider provider, SecurityLookupMessage criteria)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		var subscription = new Subscription(criteria);
		provider.Subscribe(subscription);
		return subscription;
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription LookupDataTypes(this ISubscriptionProvider provider, DataTypeLookupMessage criteria)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		var subscription = new Subscription(criteria);
		provider.Subscribe(subscription);
		return subscription;
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription LookupOrders(this ISubscriptionProvider provider, OrderStatusMessage criteria)
	{
		return provider.SubscribeOrders(criteria);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription LookupPortfolios(this ISubscriptionProvider provider, Portfolio criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
	{
		if (criteria == null)
			throw new ArgumentNullException(nameof(criteria));

		var msg = criteria.ToLookupCriteria();

		msg.Adapter = adapter;
		msg.OfflineMode = offlineMode;

		return provider.LookupPortfolios(msg);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription LookupPortfolios(this ISubscriptionProvider provider, PortfolioLookupMessage criteria)
	{
		return provider.SubscribePositions(criteria);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription LookupBoards(this ISubscriptionProvider provider, ExchangeBoard criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
	{
		if (criteria == null)
			throw new ArgumentNullException(nameof(criteria));

		var msg = new BoardLookupMessage
		{
			Like = criteria.Code,
			Adapter = adapter,
			OfflineMode = offlineMode,
		};

		return provider.LookupBoards(msg);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription LookupBoards(this ISubscriptionProvider provider, BoardLookupMessage criteria)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		var subscription = new Subscription(criteria);
		provider.Subscribe(subscription);
		return subscription;
	}

	/// <summary>
	/// Subscribe on the board changes.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="board">Board for subscription.</param>
	/// <param name="from">The initial date from which you need to get data.</param>
	/// <param name="to">The final date by which you need to get data.</param>
	/// <param name="count">Max count.</param>
	/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
	/// <param name="skip">Skip count.</param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeBoard(this ISubscriptionProvider provider, ExchangeBoard board, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null, long? skip = null)
	{
		if (board is null)
			throw new ArgumentNullException(nameof(board));

		return provider.SubscribeMarketData(null, DataType.BoardState, from, to, count, adapter: adapter, skip: skip);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribeBoard(this ISubscriptionProvider provider, ExchangeBoard board)
	{
		if (board is null)
			throw new ArgumentNullException(nameof(board));

		var subscription = provider.TryGetSubscription(0, DataType.BoardState, null);

		if (subscription != null)
			provider.UnSubscribe(subscription);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribe(this ISubscriptionProvider provider, long subscriptionId)
	{
		var subscription = provider.TryGetSubscription(subscriptionId, null, null);

		if (subscription != null)
			provider.UnSubscribe(subscription);
	}

	/// <summary>
	/// To find orders that match the filter <paramref name="criteria" />. Found orders will be passed through the event <see cref="ISubscriptionProvider.OrderReceived"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="criteria">The order which fields will be used as a filter.</param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeOrders(this ISubscriptionProvider provider, OrderStatusMessage criteria)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		var subscription = new Subscription(criteria);
		provider.Subscribe(subscription);
		return subscription;
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribeOrders(this ISubscriptionProvider provider, long originalTransactionId = 0)
	{
		var security = provider.TryGetSubscription(originalTransactionId, DataType.Transactions, null);

		if (security != null)
			provider.UnSubscribe(security);
	}

	/// <summary>
	/// To start getting quotes (order book) by the instrument. Quotes values are available through the event <see cref="ISubscriptionProvider.OrderBookReceived"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">The instrument by which quotes getting should be started.</param>
	/// <param name="from">The initial date from which you need to get data.</param>
	/// <param name="to">The final date by which you need to get data.</param>
	/// <param name="count">Max count.</param>
	/// <param name="buildMode">Build mode.</param>
	/// <param name="buildFrom">Which market-data type is used as a source value.</param>
	/// <param name="maxDepth">Max depth of requested order book.</param>
	/// <param name="refreshSpeed">Interval for data refresh.</param>
	/// <param name="depthBuilder">Order log to market depth builder.</param>
	/// <param name="passThroughOrderBookIncrement">Pass through incremental <see cref="QuoteChangeMessage"/>.</param>
	/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
	/// <param name="skip">Skip count.</param>
	/// <param name="fillGaps"><see cref="FillGapsDays"/></param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeMarketDepth(this ISubscriptionProvider provider, Security security, DateTimeOffset? from = default, DateTimeOffset? to = default, long? count = default, MarketDataBuildModes buildMode = default, DataType buildFrom = default, int? maxDepth = default, TimeSpan? refreshSpeed = default, IOrderLogMarketDepthBuilder depthBuilder = default, bool passThroughOrderBookIncrement = default, IMessageAdapter adapter = default, long? skip = default, FillGapsDays? fillGaps = default)
	{
		return provider.SubscribeMarketData(security, DataType.MarketDepth, from, to, count, buildMode, buildFrom, null, maxDepth, refreshSpeed, depthBuilder, passThroughOrderBookIncrement, adapter, skip: skip, fillGaps: fillGaps);
	}

	/// <summary>
	/// To stop getting quotes by the instrument.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">The instrument by which quotes getting should be stopped.</param>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribeMarketDepth(this ISubscriptionProvider provider, Security security)
	{
		provider.UnSubscribeMarketData(security, DataType.MarketDepth);
	}

	/// <summary>
	/// To start getting trades (tick data) by the instrument. New trades will come through the event <see cref="ISubscriptionProvider.TickTradeReceived"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">The instrument by which trades getting should be started.</param>
	/// <param name="from">The initial date from which you need to get data.</param>
	/// <param name="to">The final date by which you need to get data.</param>
	/// <param name="count">Max count.</param>
	/// <param name="buildMode">Build mode.</param>
	/// <param name="buildFrom">Which market-data type is used as a source value.</param>
	/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
	/// <param name="skip">Skip count.</param>
	/// <param name="fillGaps"><see cref="FillGapsDays"/></param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeTrades(this ISubscriptionProvider provider, Security security, DateTimeOffset? from = default, DateTimeOffset? to = default, long? count = default, MarketDataBuildModes buildMode = default, DataType buildFrom = default, IMessageAdapter adapter = default, long? skip = default, FillGapsDays? fillGaps = default)
	{
		return provider.SubscribeMarketData(security, DataType.Ticks, from, to, count, buildMode, buildFrom, adapter: adapter, skip: skip, fillGaps: fillGaps);
	}

	/// <summary>
	/// To stop getting trades (tick data) by the instrument.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">The instrument by which trades getting should be stopped.</param>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribeTrades(this ISubscriptionProvider provider, Security security)
	{
		provider.UnSubscribeMarketData(security, DataType.Ticks);
	}

	/// <summary>
	/// To start getting new information (for example, <see cref="Security.LastTick"/> or <see cref="Security.BestBid"/>) by the instrument.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">The instrument by which new information getting should be started.</param>
	/// <param name="from">The initial date from which you need to get data.</param>
	/// <param name="to">The final date by which you need to get data.</param>
	/// <param name="count">Max count.</param>
	/// <param name="buildMode">Build mode.</param>
	/// <param name="buildFrom">Which market-data type is used as a source value.</param>
	/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
	/// <param name="skip">Skip count.</param>
	/// <param name="fillGaps"><see cref="FillGapsDays"/></param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeLevel1(this ISubscriptionProvider provider, Security security, DateTimeOffset? from = default, DateTimeOffset? to = default, long? count = default, MarketDataBuildModes buildMode = default, DataType buildFrom = default, IMessageAdapter adapter = default, long? skip = default, FillGapsDays? fillGaps = default)
	{
		return provider.SubscribeMarketData(security, DataType.Level1, from, to, count, buildMode, buildFrom, adapter: adapter, skip: skip, fillGaps: fillGaps);
	}

	/// <summary>
	/// To stop getting new information.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">The instrument by which new information getting should be stopped.</param>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribeLevel1(this ISubscriptionProvider provider, Security security)
	{
		provider.UnSubscribeMarketData(security, DataType.Level1);
	}

	/// <summary>
	/// Subscribe on order log for the security.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">Security for subscription.</param>
	/// <param name="from">The initial date from which you need to get data.</param>
	/// <param name="to">The final date by which you need to get data.</param>
	/// <param name="count">Max count.</param>
	/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
	/// <returns>Subscription.</returns>
	/// <param name="skip">Skip count.</param>
	/// <param name="fillGaps"><see cref="FillGapsDays"/></param>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeOrderLog(this ISubscriptionProvider provider, Security security, DateTimeOffset? from = default, DateTimeOffset? to = default, long? count = default, IMessageAdapter adapter = default, long? skip = default, FillGapsDays? fillGaps = default)
	{
		return provider.SubscribeMarketData(security, DataType.OrderLog, from, to, count, adapter: adapter, skip: skip, fillGaps: fillGaps);
	}

	/// <summary>
	/// Unsubscribe from order log for the security.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">Security for unsubscription.</param>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribeOrderLog(this ISubscriptionProvider provider, Security security)
	{
		provider.UnSubscribeMarketData(security, DataType.OrderLog);
	}

	/// <summary>
	/// Subscribe on news.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">Security for subscription.</param>
	/// <param name="from">The initial date from which you need to get data.</param>
	/// <param name="to">The final date by which you need to get data.</param>
	/// <param name="count">Max count.</param>
	/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
	/// <param name="skip">Skip count.</param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeNews(this ISubscriptionProvider provider, Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null, long? skip = null)
	{
		return provider.SubscribeMarketData(security ?? EntitiesExtensions.NewsSecurity, DataType.News, from, to, count, adapter: adapter, skip: skip);
	}

	/// <summary>
	/// Unsubscribe from news.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">Security for subscription.</param>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribeNews(this ISubscriptionProvider provider, Security security = null)
	{
		provider.UnSubscribeMarketData(security ?? EntitiesExtensions.NewsSecurity, DataType.News);
	}

	/// <summary>
	/// To subscribe to get market data by the instrument.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">The instrument by which new information getting should be started.</param>
	/// <param name="message">The message that contain subscribe info.</param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeMarketData(this ISubscriptionProvider provider, Security security, MarketDataMessage message)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		var subscription = new Subscription(message, security);
		provider.Subscribe(subscription);
		return subscription;
	}

	/// <summary>
	/// To unsubscribe from getting market data by the instrument.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">The instrument by which new information getting should be started.</param>
	/// <param name="message">The message that contain unsubscribe info.</param>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribeMarketData(this ISubscriptionProvider provider, Security security, MarketDataMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var subscription = provider.TryGetSubscription(0, message.DataType2, security);

		if (subscription != null)
			provider.UnSubscribe(subscription);
	}

	/// <summary>
	/// To subscribe to get market data.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="message">The message that contain subscribe info.</param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeMarketData(this ISubscriptionProvider provider, MarketDataMessage message)
	{
		return provider.SubscribeMarketData(null, message);
	}

	/// <summary>
	/// To unsubscribe from getting market data.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="message">The message that contain unsubscribe info.</param>
	[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
	public static void UnSubscribeMarketData(this ISubscriptionProvider provider, MarketDataMessage message)
	{
		provider.UnSubscribeMarketData(null, message);
	}

	/// <summary>
	/// To start getting filtered quotes (order book) by the instrument. Quotes values are available through the event <see cref="ISubscriptionProvider.OrderBookReceived"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">The instrument by which quotes getting should be started.</param>
	/// <returns>Subscription.</returns>
	[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
	public static Subscription SubscribeFilteredMarketDepth(this ISubscriptionProvider provider, Security security)
	{
		return provider.SubscribeMarketData(security, DataType.FilteredMarketDepth);
	}

	[Obsolete]
	private static Subscription SubscribeMarketData(
		this ISubscriptionProvider provider, Security security, DataType type,
		DateTimeOffset? from = default, DateTimeOffset? to = default,
		long? count = default, MarketDataBuildModes buildMode = default,
		DataType buildFrom = default, Level1Fields? buildField = default,
		int? maxDepth = default, TimeSpan? refreshSpeed = default,
		IOrderLogMarketDepthBuilder depthBuilder = default,
		bool doNotBuildOrderBookIncrement = default,
		IMessageAdapter adapter = default, long? skip = default,
		FillGapsDays? fillGaps = default)
	{
		return provider.SubscribeMarketData(security, new MarketDataMessage
		{
			DataType2 = type,
			IsSubscribe = true,
			From = from,
			To = to,
			Count = count,
			BuildMode = buildMode,
			BuildFrom = buildFrom,
			BuildField = buildField,
			MaxDepth = maxDepth,
			RefreshSpeed = refreshSpeed,
			DepthBuilder = depthBuilder,
			DoNotBuildOrderBookIncrement = doNotBuildOrderBookIncrement,
			Adapter = adapter,
			Skip = skip,
			FillGaps = fillGaps,
		});
	}

	[Obsolete]
	private static void UnSubscribeMarketData(this ISubscriptionProvider provider, Security security, DataType type)
	{
		provider.UnSubscribeMarketData(security, new MarketDataMessage
		{
			DataType2 = type,
			IsSubscribe = false,
		});
	}

	private static Subscription TryGetSubscription(this ISubscriptionProvider provider, long id, DataType dataType, Security security)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		var secId = security?.ToSecurityId();

		var subscription = id > 0
			? provider.Subscriptions.FirstOrDefault(s => s.TransactionId == id)
			: provider.Subscriptions.FirstOrDefault(s => s.DataType == dataType && s.SecurityId == secId && s.State.IsActive());

		if (subscription != null && id > 0 && !subscription.State.IsActive())
		{
			subscription = null;
		}

		if (subscription == null)
		{
			if (provider is ILogReceiver logs)
				logs.AddWarningLog(LocalizedStrings.SubscriptionNonExist, id > 0 ? id : Tuple.Create(dataType, security));
		}

		return subscription;
	}
}