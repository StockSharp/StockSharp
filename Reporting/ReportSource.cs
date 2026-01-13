namespace StockSharp.Reporting;

/// <summary>
/// Report data source that allows external code to add information and supports aggregation.
/// </summary>
public class ReportSource : IReportSource
{
	private readonly List<ReportOrder> _orders = [];
	private readonly List<ReportTrade> _trades = [];
	private readonly List<(string Name, object Value)> _parameters = [];
	private readonly List<(string Name, object Value)> _statisticParameters = [];

	private readonly Lock _lock = new();

	private bool _ordersAggregationTriggered;
	private bool _tradesAggregationTriggered;

	/// <summary>
	/// Initializes a new instance of the <see cref="ReportSource"/>.
	/// </summary>
	public ReportSource()
		: this(string.Empty)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ReportSource"/>.
	/// </summary>
	/// <param name="name">Strategy name.</param>
	public ReportSource(string name)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
	}

	/// <inheritdoc />
	public void Prepare()
	{
		// Data is already up-to-date in ReportSource, nothing to do
	}

	/// <inheritdoc />
	public string Name { get; set; }

	/// <inheritdoc />
	public TimeSpan TotalWorkingTime { get; set; }

	/// <inheritdoc />
	public decimal? Commission { get; set; }

	/// <inheritdoc />
	public decimal Position { get; set; }

	/// <inheritdoc />
	public decimal PnL { get; set; }

	/// <inheritdoc />
	public decimal? Slippage { get; set; }

	/// <inheritdoc />
	public TimeSpan? Latency { get; set; }

	/// <summary>
	/// Maximum number of orders before automatic aggregation is triggered.
	/// Default is 10000. Set to 0 to disable automatic aggregation.
	/// </summary>
	public int MaxOrdersBeforeAggregation { get; set; } = 10000;

	/// <summary>
	/// Maximum number of trades before automatic aggregation is triggered.
	/// Default is 10000. Set to 0 to disable automatic aggregation.
	/// </summary>
	public int MaxTradesBeforeAggregation { get; set; } = 10000;

	/// <summary>
	/// Time interval for aggregation. Orders/trades within the same interval are grouped together.
	/// Default is 1 hour. Set to <see cref="TimeSpan.Zero"/> to disable time-based grouping
	/// (items will be aggregated by count only when threshold is exceeded).
	/// </summary>
	public TimeSpan AggregationInterval { get; set; } = TimeSpan.FromHours(1);

	/// <inheritdoc />
	public IEnumerable<(string Name, object Value)> Parameters
	{
		get
		{
			using (_lock.EnterScope())
				return [.. _parameters];
		}
	}

	/// <inheritdoc />
	public IEnumerable<(string Name, object Value)> StatisticParameters
	{
		get
		{
			using (_lock.EnterScope())
				return [.. _statisticParameters];
		}
	}

	/// <inheritdoc />
	public IEnumerable<ReportOrder> Orders
	{
		get
		{
			using (_lock.EnterScope())
				return [.. _orders];
		}
	}

	/// <inheritdoc />
	public IEnumerable<ReportTrade> OwnTrades
	{
		get
		{
			using (_lock.EnterScope())
				return [.. _trades];
		}
	}

	/// <summary>
	/// Current orders count.
	/// </summary>
	public int OrdersCount
	{
		get
		{
			using (_lock.EnterScope())
				return _orders.Count;
		}
	}

	/// <summary>
	/// Current trades count.
	/// </summary>
	public int TradesCount
	{
		get
		{
			using (_lock.EnterScope())
				return _trades.Count;
		}
	}

	/// <summary>
	/// Add a parameter.
	/// </summary>
	/// <param name="name">Parameter name.</param>
	/// <param name="value">Parameter value.</param>
	/// <returns>This instance for chaining.</returns>
	public ReportSource AddParameter(string name, object value)
	{
		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		using (_lock.EnterScope())
			_parameters.Add((name, value));

		return this;
	}

	/// <summary>
	/// Add multiple parameters.
	/// </summary>
	/// <param name="parameters">Parameters to add.</param>
	/// <returns>This instance for chaining.</returns>
	public ReportSource AddParameters(IEnumerable<(string Name, object Value)> parameters)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		using (_lock.EnterScope())
			_parameters.AddRange(parameters);

		return this;
	}

	/// <summary>
	/// Add a statistic parameter.
	/// </summary>
	/// <param name="name">Parameter name.</param>
	/// <param name="value">Parameter value.</param>
	/// <returns>This instance for chaining.</returns>
	public ReportSource AddStatisticParameter(string name, object value)
	{
		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		using (_lock.EnterScope())
			_statisticParameters.Add((name, value));

		return this;
	}

	/// <summary>
	/// Add multiple statistic parameters.
	/// </summary>
	/// <param name="parameters">Parameters to add.</param>
	/// <returns>This instance for chaining.</returns>
	public ReportSource AddStatisticParameters(IEnumerable<(string Name, object Value)> parameters)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		using (_lock.EnterScope())
			_statisticParameters.AddRange(parameters);

		return this;
	}

	/// <summary>
	/// Add an order.
	/// </summary>
	/// <param name="order">Order to add.</param>
	/// <returns>This instance for chaining.</returns>
	public ReportSource AddOrder(ReportOrder order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		using (_lock.EnterScope())
		{
			_orders.Add(order);
			CheckAndAggregateOrders();
		}

		return this;
	}

	/// <summary>
	/// Add an order with individual parameters.
	/// </summary>
	public ReportSource AddOrder(
		long? id,
		long transactionId,
		Sides side,
		DateTime time,
		decimal price,
		OrderStates? state,
		decimal? balance,
		decimal? volume,
		OrderTypes? type)
	{
		return AddOrder(new ReportOrder(id, transactionId, side, time, price, state, balance, volume, type));
	}

	/// <summary>
	/// Add multiple orders.
	/// </summary>
	/// <param name="orders">Orders to add.</param>
	/// <returns>This instance for chaining.</returns>
	public ReportSource AddOrders(IEnumerable<ReportOrder> orders)
	{
		if (orders is null)
			throw new ArgumentNullException(nameof(orders));

		using (_lock.EnterScope())
		{
			_orders.AddRange(orders);
			CheckAndAggregateOrders();
		}

		return this;
	}

	/// <summary>
	/// Add a trade.
	/// </summary>
	/// <param name="trade">Trade to add.</param>
	/// <returns>This instance for chaining.</returns>
	public ReportSource AddTrade(ReportTrade trade)
	{
		if (trade is null)
			throw new ArgumentNullException(nameof(trade));

		using (_lock.EnterScope())
		{
			_trades.Add(trade);
			CheckAndAggregateTrades();
		}

		return this;
	}

	/// <summary>
	/// Add a trade with individual parameters.
	/// </summary>
	public ReportSource AddTrade(
		long? tradeId,
		long orderTransactionId,
		DateTime time,
		decimal tradePrice,
		decimal orderPrice,
		decimal volume,
		Sides side,
		long? orderId,
		decimal? slippage,
		decimal? pnl,
		decimal? position)
	{
		return AddTrade(new ReportTrade(tradeId, orderTransactionId, time, tradePrice, orderPrice, volume, side, orderId, slippage, pnl, position));
	}

	/// <summary>
	/// Add multiple trades.
	/// </summary>
	/// <param name="trades">Trades to add.</param>
	/// <returns>This instance for chaining.</returns>
	public ReportSource AddTrades(IEnumerable<ReportTrade> trades)
	{
		if (trades is null)
			throw new ArgumentNullException(nameof(trades));

		using (_lock.EnterScope())
		{
			_trades.AddRange(trades);
			CheckAndAggregateTrades();
		}

		return this;
	}

	/// <summary>
	/// Manually trigger orders aggregation.
	/// </summary>
	/// <param name="interval">Time interval for grouping. Use <see cref="TimeSpan.Zero"/> for no time grouping.</param>
	/// <returns>This instance for chaining.</returns>
	public ReportSource AggregateOrders(TimeSpan interval)
	{
		using (_lock.EnterScope())
		{
			var aggregated = AggregateOrdersInternal(_orders, interval);
			_orders.Clear();
			_orders.AddRange(aggregated);
		}

		return this;
	}

	/// <summary>
	/// Manually trigger trades aggregation.
	/// </summary>
	/// <param name="interval">Time interval for grouping. Use <see cref="TimeSpan.Zero"/> for no time grouping.</param>
	/// <returns>This instance for chaining.</returns>
	public ReportSource AggregateTrades(TimeSpan interval)
	{
		using (_lock.EnterScope())
		{
			var aggregated = AggregateTradesInternal(_trades, interval);
			_trades.Clear();
			_trades.AddRange(aggregated);
		}

		return this;
	}

	/// <summary>
	/// Clear all orders.
	/// </summary>
	/// <returns>This instance for chaining.</returns>
	public ReportSource ClearOrders()
	{
		using (_lock.EnterScope())
		{
			_orders.Clear();
			_ordersAggregationTriggered = false;
		}

		return this;
	}

	/// <summary>
	/// Clear all trades.
	/// </summary>
	/// <returns>This instance for chaining.</returns>
	public ReportSource ClearTrades()
	{
		using (_lock.EnterScope())
		{
			_trades.Clear();
			_tradesAggregationTriggered = false;
		}

		return this;
	}

	/// <summary>
	/// Clear all parameters.
	/// </summary>
	/// <returns>This instance for chaining.</returns>
	public ReportSource ClearParameters()
	{
		using (_lock.EnterScope())
			_parameters.Clear();

		return this;
	}

	/// <summary>
	/// Clear all statistic parameters.
	/// </summary>
	/// <returns>This instance for chaining.</returns>
	public ReportSource ClearStatisticParameters()
	{
		using (_lock.EnterScope())
			_statisticParameters.Clear();

		return this;
	}

	/// <summary>
	/// Clear all data.
	/// </summary>
	/// <returns>This instance for chaining.</returns>
	public ReportSource Clear()
	{
		using (_lock.EnterScope())
		{
			_orders.Clear();
			_trades.Clear();
			_parameters.Clear();
			_statisticParameters.Clear();
			_ordersAggregationTriggered = false;
			_tradesAggregationTriggered = false;
		}

		return this;
	}

	private void CheckAndAggregateOrders()
	{
		if (MaxOrdersBeforeAggregation <= 0)
			return;

		// First time threshold exceeded - enter aggregation mode
		if (!_ordersAggregationTriggered && _orders.Count > MaxOrdersBeforeAggregation)
			_ordersAggregationTriggered = true;

		// Once in aggregation mode, always aggregate (new orders join the tail)
		if (_ordersAggregationTriggered)
		{
			var aggregated = AggregateOrdersInternal(_orders, AggregationInterval);
			_orders.Clear();
			_orders.AddRange(aggregated);
		}
	}

	private void CheckAndAggregateTrades()
	{
		if (MaxTradesBeforeAggregation <= 0)
			return;

		// First time threshold exceeded - enter aggregation mode
		if (!_tradesAggregationTriggered && _trades.Count > MaxTradesBeforeAggregation)
			_tradesAggregationTriggered = true;

		// Once in aggregation mode, always aggregate (new trades join the tail)
		if (_tradesAggregationTriggered)
		{
			var aggregated = AggregateTradesInternal(_trades, AggregationInterval);
			_trades.Clear();
			_trades.AddRange(aggregated);
		}
	}

	private static DateTime TruncateTime(DateTime time, TimeSpan interval)
	{
		if (interval <= TimeSpan.Zero)
			return DateTime.MinValue; // All items go to same bucket when no time grouping

		var ticks = time.Ticks / interval.Ticks * interval.Ticks;
		return new DateTime(ticks, time.Kind);
	}

	private static OrderStates? CalculateAggregatedState(IReadOnlyList<ReportOrder> items)
	{
		// Priority: Active > Failed > Done > null
		if (items.Any(o => o.State == OrderStates.Active))
			return OrderStates.Active;
		if (items.Any(o => o.State == OrderStates.Failed))
			return OrderStates.Failed;
		if (items.Any(o => o.State == OrderStates.Done))
			return OrderStates.Done;
		return null;
	}

	private static List<ReportOrder> AggregateOrdersInternal(List<ReportOrder> orders, TimeSpan interval)
	{
		if (orders.Count == 0)
			return orders;

		// Group by time period, side, and type
		var groups = orders
			.GroupBy(o => (TruncateTime(o.Time, interval), o.Side, o.Type))
			.OrderBy(g => g.Key.Item1)
			.ThenBy(g => g.Key.Side);

		var result = new List<ReportOrder>();
		long aggregatedTransactionId = -1;

		foreach (var group in groups)
		{
			var items = group.ToList();

			if (items.Count == 1)
			{
				result.Add(items[0]);
				continue;
			}

			// Aggregate: sum volumes, weighted average price
			var totalVolume = items.Sum(o => o.Volume ?? 0);
			var weightedPrice = totalVolume > 0
				? items.Sum(o => o.Price * (o.Volume ?? 0)) / totalVolume
				: items.Average(o => o.Price);

			// Use the earliest time in the group for the aggregated order
			var aggregatedTime = interval > TimeSpan.Zero
				? group.Key.Item1
				: items.Min(o => o.Time);

			result.Add(new ReportOrder(
				Id: null,
				TransactionId: aggregatedTransactionId--,
				Side: group.Key.Side,
				Time: aggregatedTime,
				Price: weightedPrice,
				State: CalculateAggregatedState(items),
				Balance: null,
				Volume: totalVolume > 0 ? totalVolume : null,
				Type: group.Key.Type
			));
		}

		return result;
	}

	private static List<ReportTrade> AggregateTradesInternal(List<ReportTrade> trades, TimeSpan interval)
	{
		if (trades.Count == 0)
			return trades;

		// Group by time period and side
		var groups = trades
			.GroupBy(t => (TruncateTime(t.Time, interval), t.Side))
			.OrderBy(g => g.Key.Item1)
			.ThenBy(g => g.Key.Side);

		var result = new List<ReportTrade>();
		long aggregatedTransactionId = -1;

		foreach (var group in groups)
		{
			var items = group.ToList();

			if (items.Count == 1)
			{
				result.Add(items[0]);
				continue;
			}

			// Aggregate: sum volumes, weighted average prices, sum PnL and slippage
			var totalVolume = items.Sum(t => t.Volume);
			var weightedTradePrice = totalVolume > 0
				? items.Sum(t => t.TradePrice * t.Volume) / totalVolume
				: items.Average(t => t.TradePrice);
			var weightedOrderPrice = totalVolume > 0
				? items.Sum(t => t.OrderPrice * t.Volume) / totalVolume
				: items.Average(t => t.OrderPrice);

			var totalPnL = items.Any(t => t.PnL.HasValue)
				? items.Sum(t => t.PnL ?? 0)
				: (decimal?)null;

			var totalSlippage = items.Any(t => t.Slippage.HasValue)
				? items.Sum(t => t.Slippage ?? 0)
				: (decimal?)null;

			// Position is the last position value in the group
			var lastPosition = items
				.Where(t => t.Position.HasValue)
				.OrderBy(t => t.Time)
				.LastOrDefault()?.Position;

			// Use the earliest time in the group for the aggregated trade
			var aggregatedTime = interval > TimeSpan.Zero
				? group.Key.Item1
				: items.Min(t => t.Time);

			result.Add(new ReportTrade(
				TradeId: null,
				OrderTransactionId: aggregatedTransactionId--,
				Time: aggregatedTime,
				TradePrice: weightedTradePrice,
				OrderPrice: weightedOrderPrice,
				Volume: totalVolume,
				Side: group.Key.Side,
				OrderId: null,
				Slippage: totalSlippage,
				PnL: totalPnL,
				Position: lastPosition
			));
		}

		return result;
	}
}
