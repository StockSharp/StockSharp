namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.Statistics;

/// <summary>
/// Order tracking and processing. Handles attach/process/cancel lifecycle.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderPipeline"/>.
/// </remarks>
/// <param name="stats">Statistic manager.</param>
public class OrderPipeline(IStatisticManager stats) : IEnumerable<Order>
{
	/// <inheritdoc />
	public IEnumerator<Order> GetEnumerator() => Orders.GetEnumerator();

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

	private class OrderInfo
	{
		public bool IsCanceled { get; set; }
		public decimal ReceivedVolume { get; set; }
		public OrderStates PrevState { get; set; } = OrderStates.None;
	}

	private readonly CachedSynchronizedDictionary<Order, OrderInfo> _ordersInfo = [];
	private IStatisticManager _stats = stats ?? throw new ArgumentNullException(nameof(stats));

	/// <summary>
	/// Swap the statistic manager (used when <see cref="Strategy.StatisticManager"/> is reassigned).
	/// </summary>
	/// <param name="stats">The new statistic manager.</param>
	public void SetStatisticManager(IStatisticManager stats)
		=> _stats = stats ?? throw new ArgumentNullException(nameof(stats));

	/// <summary>
	/// Fires when an order transitions from Pending to Active/Done.
	/// </summary>
	public event Action<Order> Registered;

	/// <summary>
	/// Fires on order state changes (non-registration).
	/// </summary>
	public event Action<Order> Changed;

	/// <summary>
	/// Fires when a new order is added to tracking.
	/// </summary>
	public event Action<Order> NewOrder;

	/// <summary>
	/// Total commission from registered orders.
	/// </summary>
	public decimal? Commission { get; private set; }

	/// <summary>
	/// Try to attach (track) an order. Returns false if already tracked.
	/// </summary>
	public bool TryAttach(Order order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		if (TryGetTracked(order) != null)
			return false;

		_ordersInfo.Add(order, new());
		NewOrder?.Invoke(order);
		return true;
	}

	/// <summary>
	/// Process order state change. Detects registration (Pending->Active/Done)
	/// and fires appropriate events.
	/// </summary>
	public void ProcessOrder(Order order, bool isChanging)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		var info = _ordersInfo.TryGetValue(order);

		// Count the order as registered on its first transition into a confirmed state. The previous state is
		// usually Pending, but an order can be confirmed without a separately observed Pending (e.g. an
		// emulator that reports the acceptance directly as Active), so None counts as not-yet-confirmed too.
		var isRegistered = info != null
			&& info.PrevState is (OrderStates.None or OrderStates.Pending)
			&& (order.State == OrderStates.Active || order.State == OrderStates.Done);

		if (info != null)
			info.PrevState = order.State;

		if (isRegistered)
		{
			_stats.AddNewOrder(order);

			// Fold the order commission into the running total only for non-conditional orders, matching
			// the monolith ProcessOrder which counts conditional (stop/take) orders into statistics but
			// never folds their commission.
			if (order.Type != OrderTypes.Conditional && order.Commission != null)
			{
				Commission ??= 0;
				Commission += order.Commission;
			}

			Registered?.Invoke(order);
		}
		else if (isChanging)
		{
			_stats.AddChangedOrder(order);
			Changed?.Invoke(order);
		}
	}

	/// <summary>
	/// Attach and immediately process an order (used for restored orders).
	/// </summary>
	public void AttachAndProcess(Order order)
	{
		TryAttach(order);
		ProcessOrder(order, false);
	}

	/// <summary>
	/// Check if an order is currently tracked.
	/// </summary>
	public bool IsTracked(Order order) => TryGetTracked(order) != null;

	/// <summary>
	/// Try to find the tracked order instance by reference or transaction id.
	/// </summary>
	public Order TryGetTracked(Order order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		if (_ordersInfo.ContainsKey(order))
			return order;

		var transactionId = order.TransactionId;

		if (transactionId == 0)
			return null;

		return _ordersInfo.CachedKeys.FirstOrDefault(o => o.TransactionId == transactionId);
	}

	/// <summary>
	/// Mark all active orders as canceled.
	/// </summary>
	public void CancelAll()
	{
		foreach (var pair in _ordersInfo.CachedPairs)
		{
			if (!pair.Value.IsCanceled && !pair.Key.State.IsFinal())
				pair.Value.IsCanceled = true;
		}
	}

	/// <summary>
	/// Mark a single tracked order as canceled. Returns <see langword="true"/> only on the transition
	/// (i.e. the order was tracked and had not been marked canceled yet), so the caller can issue the
	/// cancel exactly once - mirroring the monolith ProcessOrder stop-time re-cancel guard.
	/// </summary>
	/// <param name="order">The order to mark.</param>
	/// <returns><see langword="true"/> if the order was newly marked canceled.</returns>
	public bool TryMarkCanceled(Order order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		using (_ordersInfo.EnterScope())
		{
			if (!_ordersInfo.TryGetValue(order, out var info) || info.IsCanceled)
				return false;

			info.IsCanceled = true;
			return true;
		}
	}

	/// <summary>
	/// Track received volume for WaitAllTrades logic.
	/// </summary>
	public void AddReceivedVolume(Order order, decimal volume)
	{
		using (_ordersInfo.EnterScope())
		{
			if (_ordersInfo.TryGetValue(order, out var info))
				info.ReceivedVolume += volume;
		}
	}

	/// <summary>
	/// Get all tracked orders.
	/// </summary>
	public IEnumerable<Order> Orders => _ordersInfo.CachedKeys;

	/// <summary>
	/// Remove old completed orders.
	/// </summary>
	/// <param name="time">Minimum order time to keep.</param>
	public void RemoveDoneBefore(DateTime time)
	{
		_ordersInfo.SyncDo(orders => orders.RemoveWhere(pair => pair.Key.State == OrderStates.Done && pair.Key.Time < time));
	}

	/// <summary>
	/// Remove completed orders with non-positive volume.
	/// </summary>
	public void RemoveDoneWithNonPositiveVolume()
	{
		_ordersInfo.SyncDo(orders => orders.RemoveWhere(pair => pair.Key.State == OrderStates.Done && pair.Key.Volume <= 0));
	}

	/// <summary>
	/// Clear all tracked orders.
	/// </summary>
	public void Reset()
	{
		_ordersInfo.Clear();
		Commission = default;
	}
}
