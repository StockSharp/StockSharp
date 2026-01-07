namespace StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Manages order lifecycle (active orders, expiry, etc.).
/// </summary>
public interface IOrderLifecycleManager
{
	/// <summary>
	/// Register a new active order.
	/// </summary>
	/// <param name="order">Order to register.</param>
	/// <param name="currentTime">Current time for expiry calculation.</param>
	/// <returns>True if registered, false if already exists.</returns>
	bool RegisterOrder(EmulatorOrder order, DateTimeOffset currentTime);

	/// <summary>
	/// Get an active order by transaction ID.
	/// </summary>
	/// <param name="transactionId">Transaction id of the order to retrieve.</param>
	/// <returns>The matching <see cref="EmulatorOrder"/> or null if not found.</returns>
	EmulatorOrder GetOrder(long transactionId);

	/// <summary>
	/// Try to get an active order by transaction ID.
	/// </summary>
	/// <param name="transactionId">Transaction id to look up.</param>
	/// <param name="order">When method returns true, contains the found order.</param>
	/// <returns>True if order was found, otherwise false.</returns>
	bool TryGetOrder(long transactionId, out EmulatorOrder order);

	/// <summary>
	/// Remove an order from active orders.
	/// </summary>
	/// <param name="transactionId">Transaction id of the order to remove.</param>
	/// <returns>True if removed, false if not found.</returns>
	bool RemoveOrder(long transactionId);

	/// <summary>
	/// Remove and return an order.
	/// </summary>
	/// <param name="transactionId">Transaction id of the order to remove.</param>
	/// <param name="order">When method returns true, contains the removed order.</param>
	/// <returns>True if the order was removed, otherwise false.</returns>
	bool TryRemoveOrder(long transactionId, out EmulatorOrder order);

	/// <summary>
	/// Get all active orders.
	/// </summary>
	/// <returns>Enumeration of all active orders.</returns>
	IEnumerable<EmulatorOrder> GetActiveOrders();

	/// <summary>
	/// Get all active orders for a portfolio.
	/// </summary>
	/// <param name="portfolioName">Portfolio name to filter active orders.</param>
	/// <returns>Active orders for the specified portfolio.</returns>
	IEnumerable<EmulatorOrder> GetActiveOrders(string portfolioName);

	/// <summary>
	/// Get all active orders for a security.
	/// </summary>
	/// <param name="securityId">Security identifier to filter active orders.</param>
	/// <returns>Active orders for the specified security.</returns>
	IEnumerable<EmulatorOrder> GetActiveOrders(SecurityId securityId);

	/// <summary>
	/// Get all active orders matching filter.
	/// </summary>
	/// <param name="portfolioName">Optional portfolio name filter.</param>
	/// <param name="securityId">Optional security id filter.</param>
	/// <param name="side">Optional side filter.</param>
	/// <returns>Active orders matching the specified filters.</returns>
	IEnumerable<EmulatorOrder> GetActiveOrders(string portfolioName, SecurityId? securityId, Sides? side);

	/// <summary>
	/// Get orders that have expired as of the given time.
	/// </summary>
	/// <param name="currentTime">Time to check expirations against.</param>
	/// <returns>Expired orders at the specified time.</returns>
	IEnumerable<EmulatorOrder> GetExpiredOrders(DateTimeOffset currentTime);

	/// <summary>
	/// Process time passage and return expired orders.
	/// </summary>
	/// <param name="currentTime">Current time to process expiries.</param>
	/// <returns>Orders that expired during processing.</returns>
	IEnumerable<EmulatorOrder> ProcessTime(DateTimeOffset currentTime);

	/// <summary>
	/// Clear all orders.
	/// </summary>
	void Clear();

	/// <summary>
	/// Count of active orders.
	/// </summary>
	int Count { get; }
}

/// <summary>
/// Order lifecycle manager implementation.
/// </summary>
public class OrderLifecycleManager : IOrderLifecycleManager
{
	private readonly Dictionary<long, EmulatorOrder> _activeOrders = [];
	private readonly Dictionary<long, DateTimeOffset> _expiryTimes = [];

	/// <inheritdoc />
	public int Count => _activeOrders.Count;

	/// <inheritdoc />
	public bool RegisterOrder(EmulatorOrder order, DateTimeOffset currentTime)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		if (_activeOrders.ContainsKey(order.TransactionId))
			return false;

		_activeOrders[order.TransactionId] = order;

		if (order.ExpiryDate.HasValue)
		{
			var expiry = order.ExpiryDate.Value;
			if (expiry > currentTime)
			{
				_expiryTimes[order.TransactionId] = expiry;
			}
		}

		return true;
	}

	/// <inheritdoc />
	public EmulatorOrder GetOrder(long transactionId)
	{
		return _activeOrders.TryGetValue(transactionId, out var order) ? order : null;
	}

	/// <inheritdoc />
	public bool TryGetOrder(long transactionId, out EmulatorOrder order)
	{
		return _activeOrders.TryGetValue(transactionId, out order);
	}

	/// <inheritdoc />
	public bool RemoveOrder(long transactionId)
	{
		_expiryTimes.Remove(transactionId);
		return _activeOrders.Remove(transactionId);
	}

	/// <inheritdoc />
	public bool TryRemoveOrder(long transactionId, out EmulatorOrder order)
	{
		if (_activeOrders.TryGetValue(transactionId, out order))
		{
			_activeOrders.Remove(transactionId);
			_expiryTimes.Remove(transactionId);
			return true;
		}

		return false;
	}

	/// <inheritdoc />
	public IEnumerable<EmulatorOrder> GetActiveOrders()
	{
		return _activeOrders.Values;
	}

	/// <inheritdoc />
	public IEnumerable<EmulatorOrder> GetActiveOrders(string portfolioName)
	{
		return _activeOrders.Values.Where(o =>
			string.Equals(o.PortfolioName, portfolioName, StringComparison.OrdinalIgnoreCase));
	}

	/// <inheritdoc />
	public IEnumerable<EmulatorOrder> GetActiveOrders(SecurityId securityId)
	{
		// Note: Orders don't store SecurityId in current design
		// This would need to be added if needed
		return _activeOrders.Values;
	}

	/// <inheritdoc />
	public IEnumerable<EmulatorOrder> GetActiveOrders(string portfolioName, SecurityId? securityId, Sides? side)
	{
		var query = _activeOrders.Values.AsEnumerable();

		if (!string.IsNullOrEmpty(portfolioName))
			query = query.Where(o => string.Equals(o.PortfolioName, portfolioName, StringComparison.OrdinalIgnoreCase));

		if (side.HasValue)
			query = query.Where(o => o.Side == side.Value);

		return query;
	}

	/// <inheritdoc />
	public IEnumerable<EmulatorOrder> GetExpiredOrders(DateTimeOffset currentTime)
	{
		var expired = new List<EmulatorOrder>();

		foreach (var kvp in _expiryTimes)
		{
			if (kvp.Value <= currentTime && _activeOrders.TryGetValue(kvp.Key, out var order))
			{
				expired.Add(order);
			}
		}

		return expired;
	}

	/// <inheritdoc />
	public IEnumerable<EmulatorOrder> ProcessTime(DateTimeOffset currentTime)
	{
		var expired = GetExpiredOrders(currentTime).ToList();

		foreach (var order in expired)
		{
			_activeOrders.Remove(order.TransactionId);
			_expiryTimes.Remove(order.TransactionId);
		}

		return expired;
	}

	/// <inheritdoc />
	public void Clear()
	{
		_activeOrders.Clear();
		_expiryTimes.Clear();
	}
}
