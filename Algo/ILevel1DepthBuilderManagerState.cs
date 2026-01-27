namespace StockSharp.Algo;

/// <summary>
/// State storage for <see cref="Level1DepthBuilderManager"/>.
/// </summary>
public interface ILevel1DepthBuilderManagerState
{
	/// <summary>
	/// Add a new Level1-to-depth subscription.
	/// </summary>
	void AddSubscription(long transactionId, SecurityId securityId);

	/// <summary>
	/// Mark subscription as online.
	/// </summary>
	void OnSubscriptionOnline(long transactionId);

	/// <summary>
	/// Process a Level1 change message for a subscription and build order book.
	/// </summary>
	/// <param name="subscriptionId">Subscription ID.</param>
	/// <param name="l1Msg">Level1 change message.</param>
	/// <param name="subscriptionIds">All subscription IDs that should receive this data.</param>
	/// <returns>Built order book, or null.</returns>
	QuoteChangeMessage TryBuildDepth(long subscriptionId, Level1ChangeMessage l1Msg, out long[] subscriptionIds);

	/// <summary>
	/// Check if subscription exists.
	/// </summary>
	bool ContainsSubscription(long subscriptionId);

	/// <summary>
	/// Whether there are any tracked subscriptions.
	/// </summary>
	bool HasAnySubscriptions { get; }

	/// <summary>
	/// Remove subscription.
	/// </summary>
	void RemoveSubscription(long id);

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}
