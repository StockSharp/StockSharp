namespace StockSharp.Algo;

/// <summary>
/// State storage for <see cref="OrderBookIncrementManager"/>.
/// Stores subscription tracking and order book increment builders.
/// </summary>
public interface IOrderBookIncrementManagerState
{
	/// <summary>
	/// Add a new order book increment subscription.
	/// </summary>
	void AddSubscription(long transactionId, SecurityId securityId, ILogReceiver builderParent);

	/// <summary>
	/// Add a pass-through subscription (no increment building).
	/// </summary>
	void AddPassThrough(long transactionId);

	/// <summary>
	/// Add an all-security subscription.
	/// </summary>
	void AddAllSecSubscription(long transactionId);

	/// <summary>
	/// Add an all-security pass-through subscription.
	/// </summary>
	void AddAllSecPassThrough(long transactionId);

	/// <summary>
	/// Mark subscription as online (transition from by-id to security-keyed tracking).
	/// </summary>
	void OnSubscriptionOnline(long transactionId);

	/// <summary>
	/// Try apply an incremental quote update for a subscription.
	/// </summary>
	/// <param name="subscriptionId">Subscription ID.</param>
	/// <param name="quoteMsg">The incremental quote message.</param>
	/// <param name="subscriptionIds">Output: all subscription IDs that should receive this data.</param>
	/// <returns>The built full order book, or null if not applicable.</returns>
	QuoteChangeMessage TryApply(long subscriptionId, QuoteChangeMessage quoteMsg, out long[] subscriptionIds);

	/// <summary>
	/// Check if subscription is pass-through.
	/// </summary>
	bool IsPassThrough(long subscriptionId);

	/// <summary>
	/// Check if subscription is all-security pass-through.
	/// </summary>
	bool IsAllSecPassThrough(long subscriptionId);

	/// <summary>
	/// Whether there are any tracked subscriptions at all.
	/// </summary>
	bool HasAnySubscriptions { get; }

	/// <summary>
	/// Get all-security subscription IDs.
	/// </summary>
	long[] GetAllSecSubscriptionIds();

	/// <summary>
	/// Check if subscription exists in by-id tracking.
	/// </summary>
	bool ContainsSubscription(long subscriptionId);

	/// <summary>
	/// Remove subscription.
	/// </summary>
	void RemoveSubscription(long id);

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}
