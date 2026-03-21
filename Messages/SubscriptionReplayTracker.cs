namespace StockSharp.Messages;

/// <summary>
/// Tracks active subscriptions for connectors that handle reconnection internally.
/// After reconnection, provides cloned subscriptions with <see cref="ISubscriptionMessage.From"/> = null
/// (online-only, no history re-request).
/// </summary>
/// <remarks>
/// Designed for connectors like FIX and IMEX that reconnect TCP internally
/// without going through the pipeline's OfflineMessageAdapter.
/// Supports optional channel tags so multi-channel adapters (e.g. IMEX Trade/Risk)
/// can replay only the subscriptions for the reconnected channel.
/// </remarks>
public class SubscriptionReplayTracker
{
	private readonly SynchronizedDictionary<long, (ISubscriptionMessage subscription, string channel)> _subscriptions = [];

	/// <summary>
	/// Track a subscription. Call when the adapter receives a subscribe message.
	/// </summary>
	/// <param name="message">Subscription message (<see cref="ISubscriptionMessage.IsSubscribe"/> must be true).</param>
	/// <param name="channel">Optional channel tag (e.g. "Trade", "Risk") for multi-channel adapters.</param>
	public void Track(ISubscriptionMessage message, string channel = null)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		if (!message.IsSubscribe)
			return;

		// History-only subscriptions don't need replay after reconnect
		if (message.IsHistoryOnly())
			return;

		_subscriptions[message.TransactionId] = (message.TypedClone(), channel);
	}

	/// <summary>
	/// Track or untrack a subscription based on <see cref="ISubscriptionMessage.IsSubscribe"/>.
	/// </summary>
	/// <param name="message">Subscription message.</param>
	/// <param name="channel">Optional channel tag (e.g. "Trade", "Risk") for multi-channel adapters.</param>
	public void Process(ISubscriptionMessage message, string channel = null)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		if (message.IsSubscribe)
			Track(message, channel);
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
	/// Get all active subscriptions for replay after reconnection.
	/// Returns clones with <see cref="ISubscriptionMessage.From"/> = null and new transaction IDs.
	/// </summary>
	/// <param name="idGenerator">Transaction ID generator for assigning new IDs to replayed subscriptions.</param>
	/// <param name="channel">Optional channel filter. If null, returns all subscriptions.</param>
	/// <returns>Cloned subscription messages ready to be re-sent.</returns>
	public IEnumerable<ISubscriptionMessage> GetSubscriptionsForReplay(IdGenerator idGenerator, string channel = null)
	{
		if (idGenerator is null)
			throw new ArgumentNullException(nameof(idGenerator));

		foreach (var (_, (subscription, ch)) in _subscriptions.SyncGet(d => d.ToArray()))
		{
			if (channel is not null && !StringComparer.OrdinalIgnoreCase.Equals(ch, channel))
				continue;

			var clone = subscription.TypedClone();

			clone.From = null;
			clone.To = null;
			clone.TransactionId = idGenerator.GetNextId();

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
