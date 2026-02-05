namespace StockSharp.Algo;

/// <summary>
/// State storage for <see cref="SubscriptionOnlineManager"/>.
/// </summary>
public interface ISubscriptionOnlineManagerState
{
	/// <summary>
	/// Create new subscription info.
	/// </summary>
	ISubscriptionOnlineInfo CreateSubscriptionInfo(ISubscriptionMessage subscription);

	/// <summary>
	/// Create linked subscription info.
	/// </summary>
	ISubscriptionOnlineInfo CreateLinkedSubscriptionInfo(ISubscriptionOnlineInfo main);

	/// <summary>
	/// Try get subscription by key.
	/// </summary>
	bool TryGetSubscriptionByKey((DataType dataType, SecurityId securityId) key, out ISubscriptionOnlineInfo info);

	/// <summary>
	/// Add subscription by key.
	/// </summary>
	void AddSubscriptionByKey((DataType dataType, SecurityId securityId) key, ISubscriptionOnlineInfo info);

	/// <summary>
	/// Remove subscription by key value.
	/// </summary>
	void RemoveSubscriptionByKeyValue(ISubscriptionOnlineInfo info);

	/// <summary>
	/// Try get subscription by ID.
	/// </summary>
	bool TryGetSubscriptionById(long id, out ISubscriptionOnlineInfo info);

	/// <summary>
	/// Try get and remove subscription by ID.
	/// </summary>
	bool TryGetAndRemoveSubscriptionById(long id, out ISubscriptionOnlineInfo info);

	/// <summary>
	/// Check if contains subscription by ID.
	/// </summary>
	bool ContainsSubscriptionById(long id);

	/// <summary>
	/// Add subscription by ID.
	/// </summary>
	void AddSubscriptionById(long id, ISubscriptionOnlineInfo info);

	/// <summary>
	/// Remove subscription by ID.
	/// </summary>
	void RemoveSubscriptionById(long id);

	/// <summary>
	/// Add skip subscription.
	/// </summary>
	void AddSkipSubscription(long id);

	/// <summary>
	/// Remove skip subscription.
	/// </summary>
	bool RemoveSkipSubscription(long id);

	/// <summary>
	/// Check if contains skip subscription.
	/// </summary>
	bool ContainsSkipSubscription(long id);

	/// <summary>
	/// Add unsubscribe request.
	/// </summary>
	void AddUnsubscribeRequest(long id);

	/// <summary>
	/// Check if contains unsubscribe request.
	/// </summary>
	bool ContainsUnsubscribeRequest(long id);

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}

/// <summary>
/// Subscription info for online manager.
/// </summary>
public interface ISubscriptionOnlineInfo
{
	/// <summary>
	/// Subscription message.
	/// </summary>
	ISubscriptionMessage Subscription { get; }

	/// <summary>
	/// Subscription state.
	/// </summary>
	SubscriptionStates State { get; set; }

	/// <summary>
	/// Is market data subscription.
	/// </summary>
	bool IsMarketData { get; }

	/// <summary>
	/// Extra filters set.
	/// </summary>
	HashSet<long> ExtraFilters { get; }

	/// <summary>
	/// Subscribers dictionary.
	/// </summary>
	CachedSynchronizedDictionary<long, ISubscriptionMessage> Subscribers { get; }

	/// <summary>
	/// Online subscribers set.
	/// </summary>
	CachedSynchronizedSet<long> OnlineSubscribers { get; }

	/// <summary>
	/// History + live subscriptions.
	/// </summary>
	SynchronizedSet<long> HistLive { get; }

	/// <summary>
	/// Linked subscription IDs.
	/// </summary>
	List<long> Linked { get; }
}