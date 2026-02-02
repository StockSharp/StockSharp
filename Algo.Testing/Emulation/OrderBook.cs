namespace StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Order book level containing orders.
/// </summary>
class OrderBookLevelImpl(decimal price)
{
	private readonly Dictionary<long, EmulatorOrder> _ordersByTransId = [];

	public decimal Price { get; } = price;
	public decimal MarketVolume { get; set; }

	public decimal TotalVolume => MarketVolume + _ordersByTransId.Values.Sum(o => o.Balance);
	public int OrderCount => _ordersByTransId.Count;
	public IEnumerable<EmulatorOrder> Orders => _ordersByTransId.Values;

	public void AddOrder(EmulatorOrder order)
	{
		if (order.TransactionId == default)
			throw new ArgumentException("TransactionId cannot be default", nameof(order));

		_ordersByTransId[order.TransactionId] = order;
	}

	public bool RemoveOrder(long transactionId, out EmulatorOrder order)
	{
		return _ordersByTransId.TryGetValue(transactionId, out order) && _ordersByTransId.Remove(transactionId);
	}

	public bool TryGetOrder(long transactionId, out EmulatorOrder order)
	{
		return _ordersByTransId.TryGetValue(transactionId, out order);
	}

	public IEnumerable<EmulatorOrder> GetAllOrders() => [.. _ordersByTransId.Values];

	public bool IsEmpty => MarketVolume <= 0 && _ordersByTransId.Count == 0;
}

/// <summary>
/// Order book implementation.
/// </summary>
/// <remarks>
/// Create a new order book.
/// </remarks>
public class OrderBook(SecurityId securityId) : IOrderBook
{
	private readonly SortedDictionary<decimal, OrderBookLevelImpl> _bids = new(new BackwardComparer<decimal>());
	private readonly SortedDictionary<decimal, OrderBookLevelImpl> _asks = [];

	private decimal _totalBidVolume;
	private decimal _totalAskVolume;

	/// <inheritdoc />
	public SecurityId SecurityId { get; } = securityId;

	/// <inheritdoc />
	public (decimal price, decimal volume)? BestBid
	{
		get
		{
			var first = _bids.FirstOrDefault();
			return first.Value is null ? null : (first.Key, first.Value.TotalVolume);
		}
	}

	/// <inheritdoc />
	public (decimal price, decimal volume)? BestAsk
	{
		get
		{
			var first = _asks.FirstOrDefault();
			return first.Value is null ? null : (first.Key, first.Value.TotalVolume);
		}
	}

	/// <inheritdoc />
	public decimal TotalBidVolume => _totalBidVolume;

	/// <inheritdoc />
	public decimal TotalAskVolume => _totalAskVolume;

	/// <inheritdoc />
	public int BidLevels => _bids.Count;

	/// <inheritdoc />
	public int AskLevels => _asks.Count;

	/// <summary>
	/// Get worst (lowest) bid level.
	/// </summary>
	public (decimal price, decimal volume)? GetWorstBid()
	{
		var last = _bids.LastOrDefault();
		return last.Value is null ? null : (last.Key, last.Value.TotalVolume);
	}

	/// <summary>
	/// Get worst (highest) ask level.
	/// </summary>
	public (decimal price, decimal volume)? GetWorstAsk()
	{
		var last = _asks.LastOrDefault();
		return last.Value is null ? null : (last.Key, last.Value.TotalVolume);
	}

	private SortedDictionary<decimal, OrderBookLevelImpl> GetQuotes(Sides side)
		=> side == Sides.Buy ? _bids : _asks;

	private OrderBookLevelImpl GetOrCreateLevel(Sides side, decimal price)
	{
		var quotes = GetQuotes(side);

		if (!quotes.TryGetValue(price, out var level))
		{
			level = new OrderBookLevelImpl(price);
			quotes[price] = level;
		}

		return level;
	}

	/// <inheritdoc />
	public void AddQuote(EmulatorOrder order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		var level = GetOrCreateLevel(order.Side, order.Price);

		if (order.TransactionId != default)
		{
			level.AddOrder(order);
		}
		else
		{
			level.MarketVolume += order.Balance;
		}

		AddTotalVolume(order.Side, order.Balance);
	}

	/// <inheritdoc />
	public bool RemoveQuote(long transactionId, Sides side, decimal price)
	{
		var quotes = GetQuotes(side);

		if (!quotes.TryGetValue(price, out var level))
			return false;

		if (!level.RemoveOrder(transactionId, out var order))
			return false;

		AddTotalVolume(side, -order.Balance);

		if (level.IsEmpty)
			quotes.Remove(price);

		return true;
	}

	/// <inheritdoc />
	public void UpdateLevel(Sides side, decimal price, decimal volume)
	{
		var quotes = GetQuotes(side);

		if (quotes.TryGetValue(price, out var level))
		{
			var diff = volume - level.MarketVolume;
			level.MarketVolume = volume;
			AddTotalVolume(side, diff);

			if (level.IsEmpty)
				quotes.Remove(price);
		}
		else if (volume > 0)
		{
			level = new OrderBookLevelImpl(price) { MarketVolume = volume };
			quotes[price] = level;
			AddTotalVolume(side, volume);
		}
	}

	/// <inheritdoc />
	public IEnumerable<EmulatorOrder> RemoveLevel(Sides side, decimal price)
	{
		var quotes = GetQuotes(side);

		if (!quotes.TryGetValue(price, out var level))
			return [];

		var orders = level.GetAllOrders();
		var totalVolume = level.TotalVolume;

		quotes.Remove(price);
		AddTotalVolume(side, -totalVolume);

		return orders;
	}

	/// <inheritdoc />
	public IEnumerable<OrderBookLevel> GetLevels(Sides side)
	{
		var quotes = GetQuotes(side);

		foreach (var kvp in quotes)
		{
			yield return new OrderBookLevel(kvp.Key, kvp.Value.TotalVolume, [.. kvp.Value.Orders]);
		}
	}

	/// <inheritdoc />
	public decimal GetVolumeAtPrice(Sides side, decimal price)
	{
		var quotes = GetQuotes(side);
		return quotes.TryGetValue(price, out var level) ? level.TotalVolume : 0;
	}

	/// <inheritdoc />
	public IEnumerable<EmulatorOrder> GetOrdersAtPrice(Sides side, decimal price)
	{
		var quotes = GetQuotes(side);
		return quotes.TryGetValue(price, out var level) ? level.Orders : [];
	}

	/// <inheritdoc />
	public bool HasLevel(Sides side, decimal price)
	{
		return GetQuotes(side).ContainsKey(price);
	}

	/// <inheritdoc />
	public void Clear()
	{
		_bids.Clear();
		_asks.Clear();
		_totalBidVolume = 0;
		_totalAskVolume = 0;
	}

	/// <inheritdoc />
	public void Clear(Sides side)
	{
		var quotes = GetQuotes(side);
		quotes.Clear();

		if (side == Sides.Buy)
			_totalBidVolume = 0;
		else
			_totalAskVolume = 0;
	}

	/// <inheritdoc />
	public void SetSnapshot(IEnumerable<QuoteChange> bids, IEnumerable<QuoteChange> asks)
	{
		// Preserve user orders
		var userBidOrders = _bids.Values
			.SelectMany(l => l.Orders.Where(o => o.IsUserOrder))
			.ToList();

		var userAskOrders = _asks.Values
			.SelectMany(l => l.Orders.Where(o => o.IsUserOrder))
			.ToList();

		Clear();

		// Set market quotes from snapshot
		foreach (var bid in bids)
		{
			var level = GetOrCreateLevel(Sides.Buy, bid.Price);
			level.MarketVolume = bid.Volume;
			_totalBidVolume += bid.Volume;
		}

		foreach (var ask in asks)
		{
			var level = GetOrCreateLevel(Sides.Sell, ask.Price);
			level.MarketVolume = ask.Volume;
			_totalAskVolume += ask.Volume;
		}

		// Restore user orders
		foreach (var order in userBidOrders)
		{
			var level = GetOrCreateLevel(Sides.Buy, order.Price);
			level.AddOrder(order);
			_totalBidVolume += order.Balance;
		}

		foreach (var order in userAskOrders)
		{
			var level = GetOrCreateLevel(Sides.Sell, order.Price);
			level.AddOrder(order);
			_totalAskVolume += order.Balance;
		}
	}

	/// <inheritdoc />
	public QuoteChangeMessage ToMessage(DateTime localTime, DateTime serverTime)
	{
		return new QuoteChangeMessage
		{
			SecurityId = SecurityId,
			LocalTime = localTime,
			ServerTime = serverTime,
			Bids = [.. _bids.Select(kvp => new QuoteChange(kvp.Key, kvp.Value.TotalVolume))],
			Asks = [.. _asks.Select(kvp => new QuoteChange(kvp.Key, kvp.Value.TotalVolume))],
		};
	}

	/// <inheritdoc />
	public decimal GetTotalVolume(Sides side)
		=> side == Sides.Buy ? _totalBidVolume : _totalAskVolume;

	/// <inheritdoc />
	public IEnumerable<EmulatorOrder> TrimToDepth(Sides side, int maxDepth)
	{
		var quotes = GetQuotes(side);
		var result = new List<EmulatorOrder>();

		while (quotes.Count > maxDepth)
		{
			var worst = quotes.Last();
			result.AddRange(worst.Value.Orders);
			AddTotalVolume(side, -worst.Value.TotalVolume);
			quotes.Remove(worst.Key);
		}

		return result;
	}

	private void AddTotalVolume(Sides side, decimal diff)
	{
		if (side == Sides.Buy)
			_totalBidVolume += diff;
		else
			_totalAskVolume += diff;
	}

	/// <summary>
	/// Find and remove order by transaction ID from any level.
	/// </summary>
	public bool TryRemoveOrder(long transactionId, Sides side, out EmulatorOrder order)
	{
		var quotes = GetQuotes(side);

		foreach (var level in quotes.Values)
		{
			if (level.RemoveOrder(transactionId, out order))
			{
				AddTotalVolume(side, -order.Balance);

				if (level.IsEmpty)
					quotes.Remove(level.Price);

				return true;
			}
		}

		order = null;
		return false;
	}

	/// <summary>
	/// Consume volume from best levels (for matching).
	/// </summary>
	/// <param name="side">Side to consume from (opposite to order side).</param>
	/// <param name="maxPrice">Maximum price for buy / minimum for sell.</param>
	/// <param name="volume">Volume to consume.</param>
	/// <returns>Executions (price, volume, affected orders).</returns>
	public IEnumerable<(decimal price, decimal volume, IReadOnlyList<EmulatorOrder> orders)> ConsumeVolume(
		Sides side,
		decimal? maxPrice,
		decimal volume)
	{
		var quotes = GetQuotes(side);
		var remaining = volume;
		var toRemove = new List<decimal>();

		foreach (var kvp in quotes)
		{
			if (remaining <= 0)
				break;

			var price = kvp.Key;

			// Check price limit
			if (maxPrice.HasValue)
			{
				if (side == Sides.Sell && price > maxPrice.Value)
					break;
				if (side == Sides.Buy && price < maxPrice.Value)
					break;
			}

			var level = kvp.Value;
			var available = level.TotalVolume;
			var consumed = Math.Min(remaining, available);

			if (consumed > 0)
			{
				var affectedOrders = level.Orders.ToList();

				// Reduce market volume first
				var marketConsumed = Math.Min(consumed, level.MarketVolume);
				level.MarketVolume -= marketConsumed;
				var orderConsumed = consumed - marketConsumed;

				// Then reduce user orders
				foreach (var order in affectedOrders)
				{
					if (orderConsumed <= 0)
						break;

					var orderConsume = Math.Min(orderConsumed, order.Balance);
					order.Balance -= orderConsume;
					orderConsumed -= orderConsume;
				}

				AddTotalVolume(side, -consumed);
				remaining -= consumed;

				yield return (price, consumed, affectedOrders);

				if (level.IsEmpty)
					toRemove.Add(price);
			}
		}

		foreach (var price in toRemove)
			quotes.Remove(price);
	}
}
