namespace StockSharp.Algo
{
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
		//			throw new InvalidOperationException(LocalizedStrings.Str1211);

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
		//		throw new ArgumentException(LocalizedStrings.Str1217Params.Put(securityId.BoardCode), nameof(securityId));

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
	}
}