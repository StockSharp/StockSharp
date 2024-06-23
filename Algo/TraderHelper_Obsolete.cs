namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.IO;
	using System.Xml.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Positions;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;

	partial class TraderHelper
	{
		///// <summary>
		///// To get the position by the order.
		///// </summary>
		///// <param name="order">The order, used for the position calculation. At buy the position is taken with positive sign, at sell - with negative.</param>
		///// <param name="connector">The connection of interaction with trade systems.</param>
		///// <returns>Position.</returns>
		//public static decimal GetPosition(this Order order, IConnector connector)
		//{
		//	var volume = order.GetMatchedVolume(connector);

		//	return order.Direction == Sides.Buy ? volume : -volume;
		//}

		///// <summary>
		///// To get the position by the portfolio.
		///// </summary>
		///// <param name="portfolio">The portfolio, for which the position needs to be got.</param>
		///// <param name="connector">The connection of interaction with trade systems.</param>
		///// <returns>The position by the portfolio.</returns>
		//public static decimal GetPosition(this Portfolio portfolio, IConnector connector)
		//{
		//	if (portfolio == null)
		//		throw new ArgumentNullException(nameof(portfolio));

		//	if (connector == null)
		//		throw new ArgumentNullException(nameof(connector));

		//	return connector.Positions.Filter(portfolio).Sum(p => p.CurrentValue);
		//}

		///// <summary>
		///// To get the position by My trades.
		///// </summary>
		///// <param name="trades">My trades, used for the position calculation using the <see cref="GetPosition(StockSharp.BusinessEntities.MyTrade)"/> method.</param>
		///// <returns>Position.</returns>
		//public static decimal GetPosition(this IEnumerable<MyTrade> trades)
		//{
		//	return trades.Sum(t => t.GetPosition());
		//}

		///// <summary>
		///// To get the trade volume, collatable with the position size.
		///// </summary>
		///// <param name="position">The position by the instrument.</param>
		///// <returns>Order volume.</returns>
		//public static decimal GetOrderVolume(this Position position)
		//{
		//	if (position == null)
		//		throw new ArgumentNullException(nameof(position));

		//	return (position.CurrentValue / position.Security.VolumeStep ?? 1m).Abs();
		//}

		///// <summary>
		///// To group orders by instrument and portfolio.
		///// </summary>
		///// <param name="orders">Initial orders.</param>
		///// <returns>Grouped orders.</returns>
		///// <remarks>
		///// Recommended to use to reduce trade costs.
		///// </remarks>
		//public static IEnumerable<Order> Join(this IEnumerable<Order> orders)
		//{
		//	if (orders == null)
		//		throw new ArgumentNullException(nameof(orders));

		//	return orders.GroupBy(o => Tuple.Create(o.Security, o.Portfolio)).Select(g =>
		//	{
		//		Order firstOrder = null;

		//		foreach (var order in g)
		//		{
		//			if (firstOrder == null)
		//			{
		//				firstOrder = order;
		//			}
		//			else
		//			{
		//				var sameDir = firstOrder.Direction == order.Direction;

		//				firstOrder.Volume += (sameDir ? 1 : -1) * order.Volume;

		//				if (firstOrder.Volume < 0)
		//				{
		//					firstOrder.Direction = firstOrder.Direction.Invert();
		//					firstOrder.Volume = firstOrder.Volume.Abs();
		//				}

		//				firstOrder.Price = sameDir ? firstOrder.Price.GetMiddle(order.Price) : order.Price;
		//			}
		//		}

		//		if (firstOrder == null)
		//			throw new InvalidOperationException();

		//		if (firstOrder.Volume == 0)
		//			return null;

		//		firstOrder.ShrinkPrice();
		//		return firstOrder;
		//	})
		//	.Where(o => o != null);
		//}

		///// <summary>
		///// To calculate profit-loss based on trades.
		///// </summary>
		///// <param name="trades">Trades, for which the profit-loss shall be calculated.</param>
		///// <returns>Profit-loss.</returns>
		//public static decimal GetPnL(this IEnumerable<MyTrade> trades)
		//{
		//	return trades.Select(t => t.ToMessage()).GetPnL();
		//}

		///// <summary>
		///// To calculate profit-loss based on trades.
		///// </summary>
		///// <param name="trades">Trades, for which the profit-loss shall be calculated.</param>
		///// <returns>Profit-loss.</returns>
		//public static decimal GetPnL(this IEnumerable<ExecutionMessage> trades)
		//{
		//	return trades.GroupBy(t => t.SecurityId).Sum(g =>
		//	{
		//		var queue = new PnLQueue(g.Key);

		//		g.OrderBy(t => t.ServerTime).ForEach(t => queue.Process(t));

		//		return queue.RealizedPnL + queue.UnrealizedPnL;
		//	});
		//}

		///// <summary>
		///// To calculate profit-loss for trade.
		///// </summary>
		///// <param name="trade">The trade for which the profit-loss shall be calculated.</param>
		///// <param name="currentPrice">The current price of the instrument.</param>
		///// <returns>Profit-loss.</returns>
		//public static decimal GetPnL(this MyTrade trade, decimal currentPrice)
		//{
		//	if (trade == null)
		//		throw new ArgumentNullException(nameof(trade));

		//	return trade.ToMessage().GetPnL(currentPrice);
		//}

		///// <summary>
		///// To calculate profit-loss for trade.
		///// </summary>
		///// <param name="trade">The trade for which the profit-loss shall be calculated.</param>
		///// <param name="currentPrice">The current price of the instrument.</param>
		///// <returns>Profit-loss.</returns>
		//public static decimal GetPnL(this ExecutionMessage trade, decimal currentPrice)
		//{
		//	return GetPnL(trade.GetTradePrice(), trade.SafeGetVolume(), trade.Side, currentPrice);
		//}

		///// <summary>
		///// To calculate the position cost.
		///// </summary>
		///// <param name="position">Position.</param>
		///// <param name="currentPrice">The current price of the instrument.</param>
		///// <returns>Position price.</returns>
		//public static decimal GetPrice(this Position position, decimal currentPrice)
		//{
		//	if (position == null)
		//		throw new ArgumentNullException(nameof(position));

		//	var security = position.Security;

		//	return currentPrice * position.CurrentValue * security.StepPrice / security.PriceStep ?? 1;
		//}

		///// <summary>
		///// To calculate delay based on difference between the server and local time.
		///// </summary>
		///// <param name="security">Security.</param>
		///// <param name="serverTime">Server time.</param>
		///// <param name="localTime">Local time.</param>
		///// <returns>Latency.</returns>
		//public static TimeSpan GetLatency(this Security security, DateTimeOffset serverTime, DateTimeOffset localTime)
		//{
		//	return localTime - serverTime;
		//}

		///// <summary>
		///// To calculate delay based on difference between the server and local time.
		///// </summary>
		///// <param name="securityId">Security ID.</param>
		///// <param name="serverTime">Server time.</param>
		///// <param name="localTime">Local time.</param>
		///// <returns>Latency.</returns>
		//public static TimeSpan GetLatency(this SecurityId securityId, DateTimeOffset serverTime, DateTimeOffset localTime)
		//{
		//	var board = ExchangeBoard.GetBoard(securityId.BoardCode);

		//	if (board == null)
		//		throw new ArgumentException(LocalizedStrings.BoardNotFound.Put(securityId.BoardCode), nameof(securityId));

		//	return localTime - serverTime;
		//}

		///// <summary>
		///// To get the size of clear funds in the portfolio.
		///// </summary>
		///// <param name="portfolio">Portfolio.</param>
		///// <param name="useLeverage">Whether to use shoulder size for calculation.</param>
		///// <returns>The size of clear funds.</returns>
		//public static decimal GetFreeMoney(this Portfolio portfolio, bool useLeverage = false)
		//{
		//	if (portfolio == null)
		//		throw new ArgumentNullException(nameof(portfolio));

		//	var freeMoney = portfolio.Board == ExchangeBoard.Forts
		//		? portfolio.BeginValue - portfolio.CurrentValue + portfolio.VariationMargin
		//		: portfolio.CurrentValue;

		//	return useLeverage ? freeMoney * portfolio.Leverage : freeMoney;
		//}

		//private sealed class CashPosition : Position, IDisposable
		//{
		//	private readonly Portfolio _portfolio;
		//	private readonly IConnector _connector;

		//	public CashPosition(Portfolio portfolio, IConnector connector)
		//	{
		//		if (portfolio == null)
		//			throw new ArgumentNullException(nameof(portfolio));

		//		if (connector == null)
		//			throw new ArgumentNullException(nameof(connector));

		//		_portfolio = portfolio;
		//		_connector = connector;

		//		Portfolio = _portfolio;
		//		Security = new Security
		//		{
		//			Id = _portfolio.Name,
		//			Name = _portfolio.Name,
		//		};

		//		UpdatePosition();

		//		_connector.PortfoliosChanged += TraderOnPortfoliosChanged;
		//	}

		//	private void UpdatePosition()
		//	{
		//		BeginValue = _portfolio.BeginValue;
		//		CurrentValue = _portfolio.CurrentValue;
		//		BlockedValue = _portfolio.Commission;
		//	}

		//	private void TraderOnPortfoliosChanged(IEnumerable<Portfolio> portfolios)
		//	{
		//		if (portfolios.Contains(_portfolio))
		//			UpdatePosition();
		//	}

		//	void IDisposable.Dispose()
		//	{
		//		_connector.PortfoliosChanged -= TraderOnPortfoliosChanged;
		//	}
		//}

		///// <summary>
		///// To convert portfolio into the monetary position.
		///// </summary>
		///// <param name="portfolio">Portfolio with trading account.</param>
		///// <param name="connector">The connection of interaction with trading system.</param>
		///// <returns>Money position.</returns>
		//public static Position ToCashPosition(this Portfolio portfolio, IConnector connector)
		//{
		//	return new CashPosition(portfolio, connector);
		//}

		//private sealed class LookupSecurityUpdate : Disposable
		//{
		//	private readonly IConnector _connector;
		//	private TimeSpan _timeOut;
		//	private readonly SyncObject _syncRoot = new SyncObject();

		//	private readonly SynchronizedList<Security> _securities;

		//	public LookupSecurityUpdate(IConnector connector, Security criteria, TimeSpan timeOut)
		//	{
		//		if (connector == null)
		//			throw new ArgumentNullException(nameof(connector));

		//		if (criteria == null)
		//			throw new ArgumentNullException(nameof(criteria));

		//		_securities = new SynchronizedList<Security>();

		//		_connector = connector;
		//		_timeOut = timeOut;

		//		_connector.LookupSecuritiesResult += OnLookupSecuritiesResult;
		//		_connector.LookupSecurities(criteria);
		//	}

		//	public IEnumerable<Security> Wait()
		//	{
		//		while (true)
		//		{
		//			if (!_syncRoot.Wait(_timeOut))
		//				break;
		//		}

		//		return _securities;
		//	}

		//	private void OnLookupSecuritiesResult(IEnumerable<Security> securities)
		//	{
		//		_securities.AddRange(securities);

		//		_timeOut = securities.Any()
		//			           ? TimeSpan.FromSeconds(10)
		//			           : TimeSpan.Zero;

		//		_syncRoot.Pulse();
		//	}

		//	protected override void DisposeManaged()
		//	{
		//		_connector.LookupSecuritiesResult -= OnLookupSecuritiesResult;
		//	}
		//}

		///// <summary>
		///// To perform blocking search of instruments, corresponding to the criteria filter.
		///// </summary>
		///// <param name="connector">The connection of interaction with trading system.</param>
		///// <param name="criteria">Instruments search criteria.</param>
		///// <returns>Found instruments.</returns>
		//public static IEnumerable<Security> SyncLookupSecurities(this IConnector connector, Security criteria)
		//{
		//	if (connector == null)
		//		throw new ArgumentNullException(nameof(connector));

		//	if (criteria == null)
		//		throw new ArgumentNullException(nameof(criteria));

		//	using (var lsu = new LookupSecurityUpdate(connector, criteria, TimeSpan.FromSeconds(180)))
		//	{
		//		return lsu.Wait();
		//	}
		//}

		/// <summary>
		/// To get order trades.
		/// </summary>
		/// <param name="order">Orders.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Trades.</returns>
		[Obsolete]
		public static IEnumerable<MyTrade> GetTrades(this Order order, IConnector connector)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			return connector.MyTrades.Filter(order);
		}

		/// <summary>
		/// To get weighted mean price of order matching.
		/// </summary>
		/// <param name="order">The order, for which the weighted mean matching price shall be got.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>The weighted mean price. If no order exists no trades, 0 is returned.</returns>
		[Obsolete]
		public static decimal GetAveragePrice(this Order order, IConnector connector)
		{
			return order.GetTrades(connector).GetAveragePrice();
		}

		/// <summary>
		/// To calculate the implemented part of volume for order.
		/// </summary>
		/// <param name="order">The order, for which the implemented part of volume shall be calculated.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>The implemented part of volume.</returns>
		[Obsolete]
		public static decimal GetMatchedVolume(this Order order, IConnector connector)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			if (order.Type == OrderTypes.Conditional)
				throw new ArgumentException(nameof(order));

			return order.GetTrades(connector).Sum(o => o.Trade.Volume);
		}

		/// <summary>
		/// To get the position on own trade.
		/// </summary>
		/// <param name="message">Own trade, used for position calculation. At buy the trade volume <see cref="ExecutionMessage.TradeVolume"/> is taken with positive sign, at sell - with negative.</param>
		/// <param name="byOrder">To check implemented volume by order balance (<see cref="ExecutionMessage.Balance"/>) or by received trades. The default is checked by the order.</param>
		/// <returns>Position.</returns>
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
		}

		/// <summary>
		/// Convert the position object to the type <see cref="IPositionManager"/>.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <returns>Position calc manager.</returns>
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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
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
				message.States = states.ToArray();

			var subscription = new Subscription(message, (SecurityMessage)null);
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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
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
			}, (SecurityMessage)null);

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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription LookupSecurities(this ISubscriptionProvider provider, Security criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
		{
			var msg = criteria.ToLookupMessage();
			
			msg.Adapter = adapter;
			msg.OfflineMode = offlineMode;

			return provider.LookupSecurities(msg);
		}

		/// <summary>
		/// To find orders that match the filter <paramref name="criteria" />. Found orders will be passed through the event <see cref="ITransactionProvider.NewOrder"/>.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="criteria">The order which fields will be used as a filter.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <param name="offlineMode">Offline mode handling message.</param>
		/// <returns>Subscription.</returns>
		[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription LookupOrders(this ISubscriptionProvider provider, Order criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
		{
			var msg = criteria.ToLookupCriteria(null, null);

			msg.Adapter = adapter;
			msg.OfflineMode = offlineMode;

			return provider.LookupOrders(msg);
		}

		/// <summary>
		/// Subscribe to receive new candles.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="series">Candles series.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Candles count.</param>
		/// <param name="transactionId">Transaction ID.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <param name="skip">Skip count.</param>
		/// <param name="fillGaps"><see cref="FillGapsDays"/></param>
		/// <returns>Subscription.</returns>
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription SubscribeCandles(this ISubscriptionProvider provider,
			CandleSeries series, DateTimeOffset? from = default, DateTimeOffset? to = default,
			long? count = default, long? transactionId = default, IMessageAdapter adapter = default,
			long? skip = default, FillGapsDays? fillGaps = default)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			if (provider is ILogReceiver logs)
				logs.AddDebugLog(nameof(SubscribeCandles));

			var subscription = new Subscription(series);

			var mdMsg = (MarketDataMessage)subscription.SubscriptionMessage;

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
		/// To stop the candles receiving subscription, previously created by <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="series">Candles series.</param>
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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription SubscribePositions(this ISubscriptionProvider provider, PortfolioLookupMessage criteria)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			var subscription = new Subscription(criteria, (SecurityMessage)null);
			provider.Subscribe(subscription);
			return subscription;
		}

		/// <summary>
		/// Unsubscribe from positions changes.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="originalTransactionId">ID of the original message <see cref="SubscribePositions(ISubscriptionProvider, PortfolioLookupMessage)"/> for which this message is a response.</param>
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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription LookupSecurities(this ISubscriptionProvider provider, SecurityLookupMessage criteria)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			var subscription = new Subscription(criteria, (SecurityMessage)null);
			provider.Subscribe(subscription);
			return subscription;
		}

		/// <summary>
		/// To find time-frames that match the filter <paramref name="criteria" />. Found time-frames will be passed through the event <see cref="IMarketDataProvider.LookupTimeFramesResult"/>.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <returns>Subscription.</returns>
		[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription LookupTimeFrames(this ISubscriptionProvider provider, TimeFrameLookupMessage criteria)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			var subscription = new Subscription(criteria, (SecurityMessage)null);
			provider.Subscribe(subscription);
			return subscription;
		}

		/// <summary>
		/// To find orders that match the filter <paramref name="criteria" />. Found orders will be passed through the event <see cref="ITransactionProvider.NewOrder"/>.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="criteria">The order which fields will be used as a filter.</param>
		/// <returns>Subscription.</returns>
		[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription LookupOrders(this ISubscriptionProvider provider, OrderStatusMessage criteria)
		{
			return provider.SubscribeOrders(criteria);
		}

		/// <summary>
		/// To find portfolios that match the filter <paramref name="criteria" />. Found portfolios will be passed through the event <see cref="ITransactionProvider.LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <param name="offlineMode">Offline mode handling message.</param>
		/// <returns>Subscription.</returns>
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
		/// To find portfolios that match the filter <paramref name="criteria" />. Found portfolios will be passed through the event <see cref="ITransactionProvider.LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <returns>Subscription.</returns>
		[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription LookupPortfolios(this ISubscriptionProvider provider, PortfolioLookupMessage criteria)
		{
			return provider.SubscribePositions(criteria);
		}

		/// <summary>
		/// To find boards that match the filter <paramref name="criteria" />. Found boards will be passed through the event <see cref="IMarketDataProvider.LookupBoardsResult"/>.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		/// <param name="offlineMode">Offline mode handling message.</param>
		/// <returns>Subscription.</returns>
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
		/// To find boards that match the filter <paramref name="criteria" />. Found boards will be passed through the event <see cref="IMarketDataProvider.LookupBoardsResult"/>.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <returns>Subscription.</returns>
		[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription LookupBoards(this ISubscriptionProvider provider, BoardLookupMessage criteria)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			var subscription = new Subscription(criteria, (SecurityMessage)null);
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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription SubscribeBoard(this ISubscriptionProvider provider, ExchangeBoard board, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null, long? skip = null)
		{
			if (board is null)
				throw new ArgumentNullException(nameof(board));

			return provider.SubscribeMarketData(null, DataType.BoardState, from, to, count, adapter: adapter, skip: skip);
		}

		/// <summary>
		/// Unsubscribe from the board changes.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="board">Board for unsubscription.</param>
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
		/// Unsubscribe.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="subscriptionId">Subscription id.</param>
		[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
		public static void UnSubscribe(this ISubscriptionProvider provider, long subscriptionId)
		{
			var subscription = provider.TryGetSubscription(subscriptionId, null, null);

			if (subscription != null)
				provider.UnSubscribe(subscription);
		}

		/// <summary>
		/// To find orders that match the filter <paramref name="criteria" />. Found orders will be passed through the event <see cref="ITransactionProvider.NewOrder"/>.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="criteria">The order which fields will be used as a filter.</param>
		/// <returns>Subscription.</returns>
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription SubscribeOrders(this ISubscriptionProvider provider, OrderStatusMessage criteria)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			var subscription = new Subscription(criteria, (SecurityMessage)null);
			provider.Subscribe(subscription);
			return subscription;
		}

		/// <summary>
		/// Unsubscribe from orders changes.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="originalTransactionId">ID of the original message <see cref="SubscribeOrders(ISubscriptionProvider, OrderStatusMessage)"/> for which this message is a response.</param>
		[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
		public static void UnSubscribeOrders(this ISubscriptionProvider provider, long originalTransactionId = 0)
		{
			var security = provider.TryGetSubscription(originalTransactionId, DataType.Transactions, null);

			if (security != null)
				provider.UnSubscribe(security);
		}

		/// <summary>
		/// To start getting quotes (order book) by the instrument. Quotes values are available through the event <see cref="IMarketDataProvider.MarketDepthChanged"/>.
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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
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
		/// To start getting trades (tick data) by the instrument. New trades will come through the event <see cref="IMarketDataProvider.NewTrade"/>.
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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription SubscribeNews(this ISubscriptionProvider provider, Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null, long? skip = null)
		{
			return provider.SubscribeMarketData(security ?? NewsSecurity, DataType.News, from, to, count, adapter: adapter, skip: skip);
		}

		/// <summary>
		/// Unsubscribe from news.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="security">Security for subscription.</param>
		[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
		public static void UnSubscribeNews(this ISubscriptionProvider provider, Security security = null)
		{
			provider.UnSubscribeMarketData(security ?? NewsSecurity, DataType.News);
		}

		/// <summary>
		/// To subscribe to get market data by the instrument.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="security">The instrument by which new information getting should be started.</param>
		/// <param name="message">The message that contain subscribe info.</param>
		/// <returns>Subscription.</returns>
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
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
		//[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
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
		//[Obsolete("Use ISubscriptionProvider.Subscribe method.")]
		public static Subscription SubscribeMarketData(this ISubscriptionProvider provider, MarketDataMessage message)
		{
			return provider.SubscribeMarketData(null, message);
		}

		/// <summary>
		/// To unsubscribe from getting market data.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="message">The message that contain unsubscribe info.</param>
		//[Obsolete("Use ISubscriptionProvider.UnSubscribe method.")]
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
		public static Subscription SubscribeFilteredMarketDepth(this ISubscriptionProvider provider, Security security)
		{
			return provider.SubscribeMarketData(security, DataType.FilteredMarketDepth);
		}

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
			var time = TimeHelper.Now;
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
		/// To get the weighted mean price of matching by own trades.
		/// </summary>
		/// <param name="trades">Trades, for which the weighted mean price of matching shall be got.</param>
		/// <returns>The weighted mean price. If no trades, 0 is returned.</returns>
		[Obsolete]
		public static decimal GetAveragePrice(this IEnumerable<MyTrade> trades)
		{
			if (trades == null)
				throw new ArgumentNullException(nameof(trades));

			var numerator = 0m;
			var denominator = 0m;
			var currentAvgPrice = 0m;

			foreach (var myTrade in trades)
			{
				var order = myTrade.Order;
				var trade = myTrade.Trade;

				var direction = (order.Side == Sides.Buy) ? 1m : -1m;

				//≈сли открываемс€ или переворачиваемс€
				if (direction != denominator.Sign() && trade.Volume > denominator.Abs())
				{
					var newVolume = trade.Volume - denominator.Abs();
					numerator = direction * trade.Price * newVolume;
					denominator = direction * newVolume;
				}
				else
				{
					//≈сли добавл€емс€ в сторону уже открытой позиции
					if (direction == denominator.Sign())
						numerator += direction * trade.Price * trade.Volume;
					else
						numerator += direction * currentAvgPrice * trade.Volume;

					denominator += direction * trade.Volume;
				}

				currentAvgPrice = (denominator != 0) ? numerator / denominator : 0m;
			}

			return currentAvgPrice;
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

			order.ServerTime = depth.ServerTime = DateTimeOffset.Now;
			order.LocalTime = depth.LocalTime = DateTime.Now;

			var testPf = Portfolio.CreateSimulator();
			order.Portfolio = testPf;

			var trades = new List<MyTrade>();

			using (IMarketEmulator emulator = new MarketEmulator(new CollectionSecurityProvider(new[] { order.Security }), new CollectionPortfolioProvider(new[] { testPf }), new InMemoryExchangeInfoProvider(), new IncrementalIdGenerator()))
			{
				var errors = new List<Exception>();

				emulator.NewOutMessage += msg =>
				{
					if (msg is not ExecutionMessage execMsg)
						return;

					if (execMsg.Error != null)
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
	}
}