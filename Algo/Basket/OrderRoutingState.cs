namespace StockSharp.Algo.Basket;

/// <summary>
/// Default implementation of <see cref="IOrderRoutingState"/>.
/// </summary>
public class OrderRoutingState : IOrderRoutingState
{
	private readonly SynchronizedDictionary<long, IMessageAdapter> _orderAdapters = [];

	/// <inheritdoc />
	public void TryAddOrderAdapter(long transactionId, IMessageAdapter adapter)
	{
		_orderAdapters.TryAdd2(transactionId, adapter);
	}

	/// <inheritdoc />
	public bool TryGetOrderAdapter(long transactionId, out IMessageAdapter adapter)
	{
		return _orderAdapters.TryGetValue(transactionId, out adapter);
	}

	/// <inheritdoc />
	public void Clear()
	{
		_orderAdapters.Clear();
	}
}
