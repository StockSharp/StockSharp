namespace StockSharp.Algo.Basket;

/// <summary>
/// State storage for subscription routing in basket adapter.
/// </summary>
public interface ISubscriptionRoutingState
{
	/// <summary>
	/// Add subscription info.
	/// </summary>
	void AddSubscription(long transactionId, ISubscriptionMessage message, IMessageAdapter[] adapters, DataType dataType);

	/// <summary>
	/// Try get subscription info.
	/// </summary>
	bool TryGetSubscription(long transactionId, out ISubscriptionMessage message, out IMessageAdapter[] adapters, out DataType dataType);

	/// <summary>
	/// Remove subscription.
	/// </summary>
	bool RemoveSubscription(long transactionId);

	/// <summary>
	/// Get subscriber IDs by data type.
	/// </summary>
	long[] GetSubscribers(DataType dataType);

	/// <summary>
	/// Add request by ID.
	/// </summary>
	void AddRequest(long transactionId, ISubscriptionMessage message, IMessageAdapter adapter);

	/// <summary>
	/// Try get request by ID.
	/// </summary>
	bool TryGetRequest(long transactionId, out ISubscriptionMessage message, out IMessageAdapter adapter);

	/// <summary>
	/// Remove request by ID.
	/// </summary>
	bool RemoveRequest(long transactionId);

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}
