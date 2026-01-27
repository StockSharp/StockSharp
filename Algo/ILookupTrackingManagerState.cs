namespace StockSharp.Algo;

/// <summary>
/// State storage for <see cref="LookupTrackingMessageAdapter"/>.
/// Stores active lookup requests, queued lookups and timeout tracking.
/// </summary>
public interface ILookupTrackingManagerState
{
	/// <summary>
	/// Add a tracked lookup with timeout.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="subscription">Subscription message (cloned).</param>
	/// <param name="timeout">Timeout duration.</param>
	void AddLookup(long transactionId, ISubscriptionMessage subscription, TimeSpan timeout);

	/// <summary>
	/// Try get and remove a tracked lookup.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="subscription">Found subscription, or null.</param>
	/// <returns>True if found and removed.</returns>
	bool TryGetAndRemoveLookup(long transactionId, out ISubscriptionMessage subscription);

	/// <summary>
	/// Increase timeout for subscriptions that received data.
	/// </summary>
	/// <param name="subscriptionIds">Subscription IDs that received data.</param>
	void IncreaseTimeOut(long[] subscriptionIds);

	/// <summary>
	/// Remove a tracked lookup.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	void RemoveLookup(long transactionId);

	/// <summary>
	/// Try to enqueue a subscription into the per-type queue.
	/// </summary>
	/// <param name="type">Message type.</param>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="message">Subscription message (cloned).</param>
	/// <returns>True if the subscription was queued (not the first in queue), false if it should proceed immediately.</returns>
	bool TryEnqueue(MessageTypes type, long transactionId, ISubscriptionMessage message);

	/// <summary>
	/// Try to dequeue the next subscription of the given type after removing the specified ID.
	/// </summary>
	/// <param name="type">Message type.</param>
	/// <param name="removingId">Transaction ID to remove from queue.</param>
	/// <returns>Next queued message, or null.</returns>
	Message TryDequeueNext(MessageTypes type, long removingId);

	/// <summary>
	/// Try to dequeue from any type queue after removing the specified ID.
	/// </summary>
	/// <param name="removingId">Transaction ID to remove.</param>
	/// <returns>Next queued message, or null.</returns>
	Message TryDequeueFromAnyType(long removingId);

	/// <summary>
	/// Process time elapsed and return timed-out lookups.
	/// Removes timed-out entries from tracking.
	/// </summary>
	/// <param name="diff">Time elapsed since last check.</param>
	/// <param name="ignoreIds">Subscription IDs to skip (they just received data).</param>
	/// <returns>Timed-out lookups.</returns>
	IEnumerable<(ISubscriptionMessage subscription, Message nextInQueue)> ProcessTimeouts(TimeSpan diff, long[] ignoreIds);

	/// <summary>
	/// Previous time for diff calculation.
	/// </summary>
	DateTime PreviousTime { get; set; }

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}
