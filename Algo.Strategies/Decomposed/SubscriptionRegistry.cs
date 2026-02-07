namespace StockSharp.Algo.Strategies.Decomposed;

/// <summary>
/// Subscription tracking. Manages subscribe/unsubscribe, suspend/resume for rules.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SubscriptionRegistry"/>.
/// </remarks>
/// <param name="host">Strategy host.</param>
public class SubscriptionRegistry(IStrategyHost host)
{
	private readonly CachedSynchronizedDictionary<Subscription, bool> _subscriptions = [];
	private readonly SynchronizedDictionary<long, Subscription> _subscriptionsById = [];
	private readonly CachedSynchronizedSet<Subscription> _suspendSubscriptions = [];
	private readonly IStrategyHost _host = host ?? throw new ArgumentNullException(nameof(host));
	private int _rulesSuspendCount;

	/// <summary>
	/// Whether rules are currently suspended.
	/// </summary>
	public bool IsRulesSuspended => _rulesSuspendCount > 0;

	/// <summary>
	/// All tracked subscriptions.
	/// </summary>
	public IEnumerable<Subscription> Subscriptions => _subscriptions.CachedKeys;

	/// <summary>
	/// Fires when a subscription should be sent to connector.
	/// </summary>
	public event Action<Subscription> SubscriptionRequested;

	/// <summary>
	/// Fires when an unsubscription should be sent to connector.
	/// </summary>
	public event Action<Subscription> UnsubscriptionRequested;

	/// <summary>
	/// Subscribe and track. Auto-assigns TransactionId if needed.
	/// If rules are suspended, queues for later.
	/// </summary>
	public void Subscribe(Subscription subscription, bool isGlobal = false)
	{
		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		_subscriptions.Add(subscription, isGlobal);

		if (subscription.TransactionId == default)
			subscription.TransactionId = _host.GetNextTransactionId();

		_subscriptionsById.Add(subscription.TransactionId, subscription);

		if (_rulesSuspendCount > 0)
		{
			_suspendSubscriptions.Add(subscription);
			return;
		}

		SubscriptionRequested?.Invoke(subscription);
	}

	/// <summary>
	/// Unsubscribe. If suspended and not yet sent, just remove from queue.
	/// </summary>
	public void UnSubscribe(Subscription subscription)
	{
		ArgumentNullException.ThrowIfNull(subscription);

		if (subscription.TransactionId == 0)
			return;

		if (_rulesSuspendCount > 0 && _suspendSubscriptions.Remove(subscription))
		{
			_subscriptions.Remove(subscription);
			_subscriptionsById.Remove(subscription.TransactionId);
			return;
		}

		UnsubscriptionRequested?.Invoke(subscription);
	}

	/// <summary>
	/// Suspend subscription processing (for rules).
	/// </summary>
	public void SuspendRules()
	{
		_rulesSuspendCount++;
	}

	/// <summary>
	/// Resume subscription processing. Sends queued subscriptions.
	/// </summary>
	public void ResumeRules()
	{
		if (_rulesSuspendCount > 0)
		{
			_rulesSuspendCount--;

			if (_rulesSuspendCount == 0)
			{
				foreach (var subscription in _suspendSubscriptions.CopyAndClear())
					SubscriptionRequested?.Invoke(subscription);
			}
		}
	}

	/// <summary>
	/// Check if a subscription is tracked.
	/// </summary>
	public bool CanProcess(Subscription subscription)
		=> _subscriptions.ContainsKey(subscription);

	/// <summary>
	/// Try to find subscription by transaction ID.
	/// </summary>
	public Subscription TryGetById(long transactionId)
		=> _subscriptionsById.TryGetValue(transactionId);

	/// <summary>
	/// Clear all subscriptions.
	/// </summary>
	public void Reset()
	{
		_subscriptions.Clear();
		_subscriptionsById.Clear();
		_suspendSubscriptions.Clear();
		_rulesSuspendCount = 0;
	}
}
