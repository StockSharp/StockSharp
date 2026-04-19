namespace StockSharp.Messages;

/// <summary>
/// Tracks active subscriptions for connectors that handle reconnection internally.
/// After reconnection, provides cloned subscriptions with <see cref="ISubscriptionMessage.From"/> = null
/// (online-only, no history re-request).
/// </summary>
/// <remarks>
/// Designed for connectors like FIX and IMEX that reconnect TCP internally
/// without going through the pipeline's OfflineMessageAdapter. Multi-channel
/// adapters (e.g. IMEX Trade/Risk) use one tracker per logical channel so each
/// reconnect replays only the subscriptions for that channel.
/// </remarks>
public class SubscriptionReplayTracker
{
	private readonly SynchronizedDictionary<long, ISubscriptionMessage> _subscriptions = [];

	/// <summary>
	/// Track a subscription. Call when the adapter receives a subscribe message.
	/// </summary>
	/// <param name="message">Subscription message (<see cref="ISubscriptionMessage.IsSubscribe"/> must be true).</param>
	public void Track(ISubscriptionMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		if (!message.IsSubscribe)
			return;

		// History-only subscriptions don't need replay after reconnect
		if (message.IsHistoryOnly())
			return;

		_subscriptions[message.TransactionId] = message.TypedClone();
	}

	/// <summary>
	/// Track or untrack a subscription based on <see cref="ISubscriptionMessage.IsSubscribe"/>.
	/// </summary>
	/// <param name="message">Subscription message.</param>
	public void Process(ISubscriptionMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		if (message.IsSubscribe)
			Track(message);
		else
			Untrack(message.OriginalTransactionId);
	}

	/// <summary>
	/// Stop tracking a subscription. Call when the adapter receives an unsubscribe message.
	/// </summary>
	/// <param name="originalTransactionId">Original transaction ID of the subscription to remove.</param>
	/// <returns><see langword="true"/> if the subscription was found and removed.</returns>
	public bool Untrack(long originalTransactionId)
		=> _subscriptions.Remove(originalTransactionId);

	/// <summary>
	/// Get all active subscriptions for replay after an internal reconnect.
	/// Returns clones with <see cref="ISubscriptionMessage.From"/> = null under the
	/// original transaction IDs so the reconnect stays transparent to code above
	/// the adapter — external subscribers keep receiving data under the txId they
	/// originally subscribed with.
	/// </summary>
	/// <returns>Cloned subscription messages ready to be re-sent.</returns>
	public IEnumerable<ISubscriptionMessage> GetSubscriptionsForReplay()
	{
		var snapshot = _subscriptions.SyncGet(d => d.Values.ToArray());

		foreach (var subscription in snapshot)
		{
			var clone = subscription.TypedClone();

			clone.From = null;
			clone.To = null;

			yield return clone;
		}
	}

	/// <summary>
	/// Number of currently tracked subscriptions.
	/// </summary>
	public int Count => _subscriptions.Count;

	/// <summary>
	/// Clear all tracked subscriptions. Call on full reset/disconnect.
	/// </summary>
	public void Clear() => _subscriptions.Clear();
}
