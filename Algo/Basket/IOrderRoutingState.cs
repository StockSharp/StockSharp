namespace StockSharp.Algo.Basket;

/// <summary>
/// State storage for order routing in basket adapter.
/// </summary>
public interface IOrderRoutingState
{
	/// <summary>
	/// Add order transaction to adapter mapping.
	/// </summary>
	void TryAddOrderAdapter(long transactionId, IMessageAdapter adapter);

	/// <summary>
	/// Try get adapter for order transaction.
	/// </summary>
	bool TryGetOrderAdapter(long transactionId, out IMessageAdapter adapter);

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}
