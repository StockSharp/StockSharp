namespace StockSharp.Algo.Strategies.Decomposed;

using StockSharp.Algo.Statistics;

/// <summary>
/// Order tracking and processing. Handles attach/process/cancel lifecycle.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderPipeline"/>.
/// </remarks>
/// <param name="stats">Statistic manager.</param>
public class OrderPipeline(IStatisticManager stats)
{
	private class OrderInfo
	{
		public bool IsCanceled { get; set; }
		public decimal ReceivedVolume { get; set; }
		public OrderStates PrevState { get; set; } = OrderStates.None;
	}

	private readonly CachedSynchronizedDictionary<Order, OrderInfo> _ordersInfo = [];
	private readonly IStatisticManager _stats = stats ?? throw new ArgumentNullException(nameof(stats));

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

		if (_ordersInfo.ContainsKey(order))
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

		var isRegistered = info != null
			&& info.PrevState == OrderStates.Pending
			&& (order.State == OrderStates.Active || order.State == OrderStates.Done);

		if (info != null)
			info.PrevState = order.State;

		if (isRegistered)
		{
			_stats.AddNewOrder(order);

			if (order.Commission != null)
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
	public bool IsTracked(Order order) => _ordersInfo.ContainsKey(order);

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
	/// Clear all tracked orders.
	/// </summary>
	public void Reset()
	{
		_ordersInfo.Clear();
		Commission = default;
	}
}
