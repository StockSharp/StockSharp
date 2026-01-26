namespace StockSharp.Algo;

/// <summary>
/// State storage for <see cref="SubscriptionManager"/>.
/// </summary>
public interface ISubscriptionManagerState
{
	// Historical requests
	/// <summary>
	/// Add historical request.
	/// </summary>
	void AddHistoricalRequest(long transactionId, ISubscriptionMessage subscription);

	/// <summary>
	/// Try get and remove historical request.
	/// </summary>
	bool TryGetAndRemoveHistoricalRequest(long transactionId, out ISubscriptionMessage subscription);

	/// <summary>
	/// Check if contains historical request.
	/// </summary>
	bool ContainsHistoricalRequest(long transactionId);

	/// <summary>
	/// Try get historical request without removing.
	/// </summary>
	bool TryGetHistoricalRequest(long transactionId, out ISubscriptionMessage subscription);

	/// <summary>
	/// Remove historical request.
	/// </summary>
	bool RemoveHistoricalRequest(long transactionId);

	// Subscriptions
	/// <summary>
	/// Add subscription.
	/// </summary>
	void AddSubscription(long transactionId, ISubscriptionMessage subscription, SubscriptionStates state);

	/// <summary>
	/// Try get subscription.
	/// </summary>
	bool TryGetSubscription(long transactionId, out ISubscriptionMessage subscription, out SubscriptionStates state);

	/// <summary>
	/// Update subscription state.
	/// </summary>
	void UpdateSubscriptionState(long transactionId, SubscriptionStates newState);

	/// <summary>
	/// Remove subscription.
	/// </summary>
	bool RemoveSubscription(long transactionId);

	/// <summary>
	/// Get all active subscriptions for remapping.
	/// </summary>
	IEnumerable<(long transactionId, ISubscriptionMessage subscription)> GetActiveSubscriptions();

	// Replace ID mapping
	/// <summary>
	/// Add replace ID mapping.
	/// </summary>
	void AddReplaceId(long newId, long originalId);

	/// <summary>
	/// Try get original ID by new ID.
	/// </summary>
	bool TryGetOriginalId(long newId, out long originalId);

	/// <summary>
	/// Try get new ID by original ID.
	/// </summary>
	bool TryGetNewId(long originalId, out long newId);

	/// <summary>
	/// Remove replace ID mapping by new ID.
	/// </summary>
	void RemoveReplaceId(long newId);

	/// <summary>
	/// Check if contains replace ID.
	/// </summary>
	bool ContainsReplaceId(long newId);

	/// <summary>
	/// Get count of replace IDs.
	/// </summary>
	int ReplaceIdCount { get; }

	/// <summary>
	/// Clear all replace IDs.
	/// </summary>
	void ClearReplaceIds();

	// All security ID children
	/// <summary>
	/// Add all security ID child.
	/// </summary>
	void AddAllSecIdChild(long transactionId);

	// ReMap subscriptions
	/// <summary>
	/// Add message to remap queue.
	/// </summary>
	void AddReMapSubscription(Message message);

	/// <summary>
	/// Get and clear remap subscriptions.
	/// </summary>
	Message[] GetAndClearReMapSubscriptions();

	/// <summary>
	/// Get count of pending remap subscriptions.
	/// </summary>
	int ReMapSubscriptionCount { get; }

	/// <summary>
	/// Clear remap subscriptions.
	/// </summary>
	void ClearReMapSubscriptions();

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}
