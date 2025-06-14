namespace StockSharp.Algo;

using System.Net;
using System.Xml.Linq;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Positions;
using StockSharp.Algo.Testing;

partial class TraderHelper
{
	/// <summary>
	/// To calculate the current price by the order book depending on the order direction.
	/// </summary>
	/// <param name="depth">The order book for the current price calculation.</param>
	/// <param name="side">The order direction. If it is a buy, <see cref="MarketDepth.BestAsk"/> value is used, otherwise <see cref="MarketDepth.BestBid"/>.</param>
	/// <param name="priceStep"><see cref="SecurityMessage.PriceStep"/></param>
	/// <param name="priceType">The type of current price.</param>
	/// <param name="orders">Orders to be ignored.</param>
	/// <returns>The current price. If information in order book is insufficient, then <see langword="null" /> will be returned.</returns>
	/// <remarks>
	/// For correct operation of the method the order book export shall be launched.
	/// </remarks>
	[Obsolete("Use subscriptions.")]
	public static Unit GetCurrentPrice(this IOrderBookMessage depth, Sides side, decimal? priceStep, MarketPriceTypes priceType = MarketPriceTypes.Following, IEnumerable<Order> orders = null)
	{
		if (depth == null)
			throw new ArgumentNullException(nameof(depth));

		if (orders != null)
		{
			var dict = new Dictionary<Tuple<Sides, decimal>, HashSet<Order>>();

			foreach (var order in orders)
			{
				if (!dict.SafeAdd(Tuple.Create(order.Side, order.Price)).Add(order))
					throw new InvalidOperationException(LocalizedStrings.HasDuplicates.Put(order));
			}

			var bids = depth.Bids.ToList();
			var asks = depth.Asks.ToList();

			for (var i = 0; i < bids.Count; i++)
			{
				var quote = bids[i];

				if (dict.TryGetValue(Tuple.Create(Sides.Buy, quote.Price), out var bidOrders))
				{
					foreach (var order in bidOrders)
					{
						if (!orders.Contains(order))
							quote.Volume -= order.Balance;
					}

					if (quote.Volume <= 0)
					{
						bids.RemoveAt(i);
						i--;
					}
					else
						bids[i] = quote;
				}
			}

			for (var i = 0; i < asks.Count; i++)
			{
				var quote = asks[i];

				if (dict.TryGetValue(Tuple.Create(Sides.Sell, quote.Price), out var asksOrders))
				{
					foreach (var order in asksOrders)
					{
						if (!orders.Contains(order))
							quote.Volume -= order.Balance;
					}

					if (quote.Volume <= 0)
					{
						asks.RemoveAt(i);
						i--;
					}
					else
						asks[i] = quote;
				}
			}

			depth = new QuoteChangeMessage
			{
				SecurityId = depth.SecurityId,
				ServerTime = depth.ServerTime,
				Bids = [.. bids],
				Asks = [.. asks],
			};
		}

		var (bid, ask) = depth.GetBestPair();
		return new MarketDepthPair(bid, ask).GetCurrentPrice(side, priceStep, priceType);
	}

	/// <summary>
	/// To calculate the current price based on the best pair of quotes, depending on the order direction.
	/// </summary>
	/// <param name="bestPair">The best pair of quotes, used for the current price calculation.</param>
	/// <param name="side">The order direction. If it is a buy, <see cref="MarketDepthPair.Ask"/> value is used, otherwise <see cref="MarketDepthPair.Bid"/>.</param>
	/// <param name="priceStep"><see cref="SecurityMessage.PriceStep"/></param>
	/// <param name="priceType">The type of current price.</param>
	/// <returns>The current price. If information in order book is insufficient, then <see langword="null" /> will be returned.</returns>
	/// <remarks>
	/// For correct operation of the method the order book export shall be launched.
	/// </remarks>
	[Obsolete("Use subscriptions.")]
	public static Unit GetCurrentPrice(this MarketDepthPair bestPair, Sides side, decimal? priceStep, MarketPriceTypes priceType = MarketPriceTypes.Following)
	{
		if (bestPair == null)
			throw new ArgumentNullException(nameof(bestPair));

		decimal? currentPrice;

		switch (priceType)
		{
			case MarketPriceTypes.Opposite:
			{
				var quote = side == Sides.Buy ? bestPair.Ask : bestPair.Bid;
				currentPrice = quote?.Price;
				break;
			}
			case MarketPriceTypes.Following:
			{
				var quote = side == Sides.Buy ? bestPair.Bid : bestPair.Ask;
				currentPrice = quote?.Price;
				break;
			}
			case MarketPriceTypes.Middle:
			{
				if (bestPair.IsFull)
					currentPrice = bestPair.GetMiddlePrice(priceStep);
				else
					currentPrice = null;
				break;
			}
			default:
				throw new ArgumentOutOfRangeException(nameof(priceType), priceType, LocalizedStrings.InvalidValue);
		}

		return currentPrice == null
			? null
			: new Unit(currentPrice.Value);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use subscriptions.")]
	public static IEnumerable<MyTrade> GetTrades(this Order order, IConnector connector)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		if (connector == null)
			throw new ArgumentNullException(nameof(connector));

		return [];
	}

	/// <summary>
	/// To filter orders for the given instrument.
	/// </summary>
	/// <param name="trades">All trades, in which the required shall be searched for.</param>
	/// <param name="security">The instrument, for which the trades shall be filtered.</param>
	/// <returns>Filtered trades.</returns>
	[Obsolete("Use ITickTradeMessage.")]
	public static IEnumerable<Trade> Filter(this IEnumerable<Trade> trades, Security security)
	{
		if (trades == null)
			throw new ArgumentNullException(nameof(trades));

		if (security == null)
			throw new ArgumentNullException(nameof(security));

		var basket = security as BasketSecurity;
		return basket?.InnerSecurityIds.SelectMany(id => trades.Where(o => o.Security.ToSecurityId() == id)) ?? trades.Where(t => t.Security == security);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use subscriptions.")]
	public static decimal GetAveragePrice(this Order order, IConnector connector)
	{
		return order.GetTrades(connector).GetAveragePrice();
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use subscriptions.")]
	public static decimal GetMatchedVolume(this Order order, IConnector connector)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		if (order.Type == OrderTypes.Conditional)
			throw new ArgumentException(nameof(order));

		return order.GetTrades(connector).Sum(o => o.Trade.Volume);
	}

	/// <summary>
	/// </summary>
	[Obsolete]
	public static decimal? GetPosition(this ExecutionMessage message, bool byOrder)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var sign = message.Side == Sides.Buy ? 1 : -1;

		decimal? position;

		if (byOrder)
			position = message.OrderVolume - message.Balance;
		else
			position = message.TradeVolume;

		return position * sign;
	}

	private sealed class NativePositionManager : IPositionManager
	{
		//private readonly Position _position;

		public NativePositionManager(Position position)
		{
			if (position is null)
				throw new ArgumentNullException(nameof(position));
			//_position = position ?? throw new ArgumentNullException(nameof(position));
		}

		PositionChangeMessage IPositionManager.ProcessMessage(Message message) => null;

		void IPersistable.Load(SettingsStorage storage) { }
		void IPersistable.Save(SettingsStorage storage) { }
	}

	/// <summary>
	/// </summary>
	[Obsolete]
	public static IPositionManager ToPositionManager(this Position position)
		=> new NativePositionManager(position);

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

	/// <summary>
	/// It returns yesterday's data at the end of day (EOD, End-Of-Day) by the selected instrument.
	/// </summary>
	/// <param name="securityName">Security name.</param>
	/// <returns>Yesterday's market-data.</returns>
	/// <remarks>
	/// Date is determined by the system time.
	/// </remarks>
	[Obsolete]
	public static Level1ChangeMessage GetFortsYesterdayEndOfDay(this string securityName)
	{
		var time = DateTime.Now;
		time -= TimeSpan.FromDays(1);
		return GetFortsEndOfDay(securityName, time, time).FirstOrDefault();
	}

	/// <summary>
	/// It returns a list of the data at the end of day (EOD, End-Of-Day) by the selected instrument for the specified period.
	/// </summary>
	/// <param name="securityName">Security name.</param>
	/// <param name="fromDate">Begin period.</param>
	/// <param name="toDate">End period.</param>
	/// <returns>Historical market-data.</returns>
	[Obsolete]
	public static IEnumerable<Level1ChangeMessage> GetFortsEndOfDay(this string securityName, DateTime fromDate, DateTime toDate)
	{
		if (fromDate > toDate)
			throw new ArgumentOutOfRangeException(nameof(fromDate), fromDate, LocalizedStrings.StartCannotBeMoreEnd.Put(fromDate, toDate));

		static decimal GetPart(string item)
			=> !decimal.TryParse(item, out var pardesData) ? 0 : pardesData;

		using var client = new WebClient();

		var csvUrl = "https://moex.com/en/derivatives/contractresults-exp.aspx?day1={0:yyyyMMdd}&day2={1:yyyyMMdd}&code={2}"
			.Put(fromDate.Date, toDate.Date, securityName);

		using var stream = client.OpenRead(csvUrl) ?? throw new InvalidOperationException(LocalizedStrings.Error);

		return Do.Invariant(() =>
		{
			var message = new List<Level1ChangeMessage>();

			using (var reader = new StreamReader(stream, StringHelper.WindowsCyrillic))
			{
				reader.ReadLine();

				string newLine;
				while ((newLine = reader.ReadLine()) != null)
				{
					var row = newLine.Split(',');

					var time = row[0].ToDateTime("dd.MM.yyyy");

					message.Add(new Level1ChangeMessage
					{
						ServerTime = time.EndOfDay().ApplyMoscow(),
						SecurityId = new SecurityId
						{
							SecurityCode = securityName,
							BoardCode = BoardCodes.Forts,
						},
					}
					.TryAdd(Level1Fields.SettlementPrice, GetPart(row[1]))
					.TryAdd(Level1Fields.AveragePrice, GetPart(row[2]))
					.TryAdd(Level1Fields.OpenPrice, GetPart(row[3]))
					.TryAdd(Level1Fields.HighPrice, GetPart(row[4]))
					.TryAdd(Level1Fields.LowPrice, GetPart(row[5]))
					.TryAdd(Level1Fields.ClosePrice, GetPart(row[6]))
					.TryAdd(Level1Fields.Change, GetPart(row[7]))
					.TryAdd(Level1Fields.LastTradeVolume, GetPart(row[8]))
					.TryAdd(Level1Fields.Volume, GetPart(row[11]))
					.TryAdd(Level1Fields.OpenInterest, GetPart(row[13])));
				}
			}

			return message;
		});
	}

	/// <summary>
	/// The earliest date for which there is an indicative rate of US dollar to the Russian ruble. It is 2 November 2009.
	/// </summary>
	[Obsolete]
	public static DateTime UsdRateMinAvailableTime { get; } = new(2009, 11, 2);

	/// <summary>
	/// To get an indicative exchange rate of a currency pair.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="fromDate">Begin period.</param>
	/// <param name="toDate">End period.</param>
	/// <returns>The indicative rate of US dollar to the Russian ruble.</returns>
	[Obsolete]
	public static IDictionary<DateTimeOffset, decimal> GetFortsRate(this SecurityId securityId, DateTime fromDate, DateTime toDate)
	{
		if (fromDate > toDate)
			throw new ArgumentOutOfRangeException(nameof(fromDate), fromDate, LocalizedStrings.StartCannotBeMoreEnd.Put(fromDate, toDate));

		using var client = new WebClient();

		var url = $"https://moex.com/export/derivatives/currency-rate.aspx?language=en&currency={securityId.SecurityCode.Replace("/", "__")}&moment_start={fromDate:yyyy-MM-dd}&moment_end={toDate:yyyy-MM-dd}";

		using var stream = client.OpenRead(url) ?? throw new InvalidOperationException(LocalizedStrings.Error);

		return Do.Invariant(() =>
			(from rate in XDocument.Load(stream).Descendants("rate")
			 select new KeyValuePair<DateTimeOffset, decimal>(
				 rate.GetAttributeValue<string>("moment").ToDateTime("yyyy-MM-dd HH:mm:ss").ApplyMoscow(),
				 rate.GetAttributeValue<decimal>("value"))).OrderBy(p => p.Key).ToDictionary());
	}

	/// <summary>
	/// To delete in order book levels, which shall disappear in case of trades occurrence <paramref name="trades" />.
	/// </summary>
	/// <param name="depth">The order book to be cleared.</param>
	/// <param name="trades">Trades.</param>
	[Obsolete]
	public static void EmulateTrades(this MarketDepth depth, IEnumerable<ExecutionMessage> trades)
	{
		if (depth == null)
			throw new ArgumentNullException(nameof(depth));

		if (trades == null)
			throw new ArgumentNullException(nameof(trades));

		var changedVolume = new Dictionary<decimal, decimal>();

		var maxTradePrice = decimal.MinValue;
		var minTradePrice = decimal.MaxValue;

		foreach (var trade in trades)
		{
			var price = trade.GetTradePrice();

			minTradePrice = minTradePrice.Min(price);
			maxTradePrice = maxTradePrice.Max(price);

			var q = depth.GetQuote(price);

			if (q is null)
				continue;

			var quote = q.Value;

			if (!changedVolume.TryGetValue(price, out var vol))
				vol = quote.Volume;

			vol -= trade.SafeGetVolume();
			changedVolume[quote.Price] = vol;
		}

		var bids = new QuoteChange[depth.Bids.Length];

		void B1()
		{
			var i = 0;
			var count = 0;

			for (; i < depth.Bids.Length; i++)
			{
				var quote = depth.Bids[i];
				var price = quote.Price;

				if (price > minTradePrice)
					continue;

				if (price == minTradePrice)
				{
					if (changedVolume.TryGetValue(price, out var vol))
					{
						if (vol <= 0)
							continue;

						//quote = quote.Clone();
						quote.Volume = vol;
					}
				}

				bids[count++] = quote;
				i++;

				break;
			}

			Array.Copy(depth.Bids, i, bids, count, depth.Bids.Length - i);
			Array.Resize(ref bids, count + (depth.Bids.Length - i));
		}

		B1();

		var asks = new QuoteChange[depth.Asks.Length];

		void A1()
		{
			var i = 0;
			var count = 0;

			for (; i < depth.Asks.Length; i++)
			{
				var quote = depth.Asks[i];
				var price = quote.Price;

				if (price < maxTradePrice)
					continue;

				if (price == maxTradePrice)
				{
					if (changedVolume.TryGetValue(price, out var vol))
					{
						if (vol <= 0)
							continue;

						//quote = quote.Clone();
						quote.Volume = vol;
					}
				}

				asks[count++] = quote;
				i++;

				break;
			}

			Array.Copy(depth.Asks, i, asks, count, depth.Asks.Length - i);
			Array.Resize(ref asks, count + (depth.Asks.Length - i));
		}

		A1();

		depth.Update(bids, asks, depth.ServerTime);
	}

	/// <summary>
	/// To get probable trades for order book for the given order.
	/// </summary>
	/// <param name="depth">The order book, reflecting situation on market at the moment of function call.</param>
	/// <param name="order">The order, for which probable trades shall be calculated.</param>
	/// <returns>Probable trades.</returns>
	[Obsolete]
	public static IEnumerable<MyTrade> GetTheoreticalTrades(this MarketDepth depth, Order order)
	{
		if (depth == null)
			throw new ArgumentNullException(nameof(depth));

		if (order == null)
			throw new ArgumentNullException(nameof(order));

		if (depth.Security != order.Security)
			throw new ArgumentException(nameof(order));

		order = order.ReRegisterClone();
		depth = depth.Clone();

		var now = DateTimeOffset.UtcNow;
		order.ServerTime = depth.ServerTime = now;
		order.LocalTime = depth.LocalTime = now;

		var testPf = Portfolio.CreateSimulator();
		order.Portfolio = testPf;

		var trades = new List<MyTrade>();

		using (IMarketEmulator emulator = new MarketEmulator(new CollectionSecurityProvider([order.Security]), new CollectionPortfolioProvider([testPf]), new InMemoryExchangeInfoProvider(), new IncrementalIdGenerator()))
		{
			var errors = new List<Exception>();

			emulator.NewOutMessage += msg =>
			{
				if (msg is not ExecutionMessage execMsg)
					return;

				if (!execMsg.IsOk())
					errors.Add(execMsg.Error);

				if (execMsg.HasTradeInfo())
				{
					trades.Add(new MyTrade
					{
						Order = order,
						Trade = execMsg.ToTrade(new Trade { Security = order.Security })
					});
				}
			};

			var depthMsg = depth.ToMessage();
			var regMsg = order.CreateRegisterMessage();
			var pfMsg = testPf.ToChangeMessage();

			pfMsg.ServerTime = depthMsg.ServerTime = order.ServerTime;
			pfMsg.LocalTime = regMsg.LocalTime = depthMsg.LocalTime = order.LocalTime;

			emulator.SendInMessage(pfMsg);
			emulator.SendInMessage(depthMsg);
			emulator.SendInMessage(regMsg);

			if (errors.Count > 0)
				throw new AggregateException(errors);
		}

		return trades;
	}

	/// <summary>
	/// To get probable trades by the order book for the market price and given volume.
	/// </summary>
	/// <param name="depth">The order book, reflecting situation on market at the moment of function call.</param>
	/// <param name="orderDirection">Order side.</param>
	/// <param name="volume">The volume, supposed to be implemented.</param>
	/// <returns>Probable trades.</returns>
	[Obsolete]
	public static IEnumerable<MyTrade> GetTheoreticalTrades(this MarketDepth depth, Sides orderDirection, decimal volume)
	{
		return depth.GetTheoreticalTrades(orderDirection, volume, 0);
	}

	/// <summary>
	/// To get probable trades by order book for given price and volume.
	/// </summary>
	/// <param name="depth">The order book, reflecting situation on market at the moment of function call.</param>
	/// <param name="side">Order side.</param>
	/// <param name="volume">The volume, supposed to be implemented.</param>
	/// <param name="price">The price, based on which the order is supposed to be forwarded. If it equals 0, option of market order will be considered.</param>
	/// <returns>Probable trades.</returns>
	[Obsolete]
	public static IEnumerable<MyTrade> GetTheoreticalTrades(this MarketDepth depth, Sides side, decimal volume, decimal price)
	{
		if (depth == null)
			throw new ArgumentNullException(nameof(depth));

		return depth.GetTheoreticalTrades(new Order
		{
			Side = side,
			Type = price == 0 ? OrderTypes.Market : OrderTypes.Limit,
			Security = depth.Security,
			Price = price,
			Volume = volume
		});
	}

	/// <summary>
	/// To check, does the order log contain the order registration.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns><see langword="true" />, if the order log contains the order registration, otherwise, <see langword="false" />.</returns>
	[Obsolete("Use messages only.")]
	public static bool IsRegistered(this OrderLogItem item)
	{
		return item.ToMessage().IsOrderLogRegistered();
	}

	/// <summary>
	/// To check, does the order log contain the cancelled order.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns><see langword="true" />, if the order log contain the cancelled order, otherwise, <see langword="false" />.</returns>
	[Obsolete("Use messages only.")]
	public static bool IsCanceled(this OrderLogItem item)
	{
		return item.ToMessage().IsOrderLogCanceled();
	}

	/// <summary>
	/// To check, does the order log contain the order matching.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns><see langword="true" />, if the order log contains order matching, otherwise, <see langword="false" />.</returns>
	[Obsolete("Use messages only.")]
	public static bool IsMatched(this OrderLogItem item)
	{
		return item.ToMessage().IsOrderLogMatched();
	}

	/// <summary>
	/// To get the reason for cancelling order in orders log.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns>The reason for order cancelling in order log.</returns>
	[Obsolete("Use messages only.")]
	public static OrderLogCancelReasons GetCancelReason(this OrderLogItem item)
	{
		return item.ToMessage().GetOrderLogCancelReason();
	}

	/// <summary>
	/// Build market depths from order log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <param name="builder">Order log to market depth builder.</param>
	/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new item of orders log.</param>
	/// <param name="maxDepth">The maximal depth of order book. The default is <see cref="Int32.MaxValue"/>, which means endless depth.</param>
	/// <returns>Market depths.</returns>
	[Obsolete("Use messages only.")]
	public static IEnumerable<MarketDepth> ToOrderBooks(this IEnumerable<OrderLogItem> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default, int maxDepth = int.MaxValue)
	{
		var first = items.FirstOrDefault();

		if (first == null)
			return [];

		return items.ToMessages<OrderLogItem, ExecutionMessage>()
			.ToOrderBooks(builder, interval)
			.BuildIfNeed()
			.ToEntities<QuoteChangeMessage, MarketDepth>(first.Order.Security);
	}

	/// <summary>
	/// To build tick trades from the orders log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <returns>Tick trades.</returns>
	[Obsolete("Use messages only.")]
	public static IEnumerable<Trade> ToTrades(this IEnumerable<OrderLogItem> items)
	{
		var first = items.FirstOrDefault();

		if (first == null)
			return [];

		var ticks = items
			.Select(i => i.ToMessage())
			.ToTicks();

		return ticks.Select(m => m.ToTrade(first.Order.Security));
	}

	/// <summary>
	/// Is MICEX board.
	/// </summary>
	/// <param name="board">Board to check.</param>
	/// <returns>Check result.</returns>
	[Obsolete]
	public static bool IsMicex(this ExchangeBoard board)
	{
		if (board == null)
			throw new ArgumentNullException(nameof(board));

		return board.Exchange == Exchange.Moex && board != ExchangeBoard.Forts;
	}

	/// <summary>
	/// Is the UX exchange stock market board.
	/// </summary>
	/// <param name="board">Board to check.</param>
	/// <returns>Check result.</returns>
	[Obsolete]
	public static bool IsUxStock(this ExchangeBoard board)
	{
		if (board == null)
			throw new ArgumentNullException(nameof(board));

		return board.Exchange == Exchange.Ux && board != ExchangeBoard.Ux;
	}
}