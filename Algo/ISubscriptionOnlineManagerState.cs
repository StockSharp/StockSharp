namespace StockSharp.Algo;

/// <summary>
/// State storage for <see cref="SubscriptionOnlineManager"/>.
/// </summary>
/// <remarks>
/// Two things live here and they are not the same. One is who holds each shared subscription, which
/// is plain bookkeeping. The other is what a reply has to be routed by - an order's own transaction
/// aliased onto the order-status subscription that reports it, the requests passed straight through,
/// and the unsubscribes still in flight - which exists only because messages are rewritten.
/// </remarks>
public interface ISubscriptionOnlineManagerState
{
	/// <summary>
	/// Create a linked view onto a subscription, for routing replies that name something else.
	/// </summary>
	ISubscriptionOnlineInfo CreateLinkedSubscriptionInfo(ISubscriptionOnlineInfo main);

	/// <summary>
	/// Try get the shared subscription of a data type and security.
	/// </summary>
	bool TryGetSubscriptionByKey((DataType dataType, SecurityId securityId) key, out ISubscriptionOnlineInfo info);

	/// <summary>
	/// Record that a subscriber holds the shared subscription of a data type and security, opening it
	/// when it is the first to ask.
	/// </summary>
	/// <param name="key">Data type and security the subscription carries.</param>
	/// <param name="subscriberId">Transaction the subscriber asked under.</param>
	/// <param name="request">What this subscriber asked for.</param>
	/// <param name="createSubscription">Builds the subscription to send upstream, for a new one.</param>
	/// <param name="isOpener">
	/// <see langword="true"/> when this subscriber opened it and the request has to go upstream.
	/// </param>
	/// <returns>The subscription, or <see langword="null"/> when this subscriber already holds one.</returns>
	ISubscriptionOnlineInfo AddSubscriber((DataType dataType, SecurityId securityId) key, long subscriberId, ISubscriptionMessage request, Func<ISubscriptionMessage> createSubscription, out bool isOpener);

	/// <summary>
	/// Drop one subscriber.
	/// </summary>
	/// <param name="subscriberId">Transaction the subscriber asked under.</param>
	/// <param name="info">What it was holding.</param>
	/// <param name="wasLast">
	/// <see langword="true"/> when it was the last holder and the subscription has to be given up
	/// upstream.
	/// </param>
	/// <returns><see langword="true"/> when it held anything.</returns>
	bool RemoveSubscriber(long subscriberId, out ISubscriptionOnlineInfo info, out bool wasLast);

	/// <summary>
	/// Stop a subscription being joined, while the subscribers it still has keep being answered.
	/// </summary>
	void StopSubscription(ISubscriptionOnlineInfo info);

	/// <summary>
	/// Give a subscription up entirely, subscribers and aliases with it.
	/// </summary>
	void DiscardSubscription(ISubscriptionOnlineInfo info);

	/// <summary>
	/// Try get a subscription by a subscriber's transaction or by an alias.
	/// </summary>
	bool TryGetSubscriptionById(long id, out ISubscriptionOnlineInfo info);

	/// <summary>
	/// Check whether a transaction names a subscriber or an alias.
	/// </summary>
	bool ContainsSubscriptionById(long id);

	/// <summary>
	/// Drop a subscriber and report what it was holding, for one whose subscription was refused.
	/// </summary>
	bool TryGetAndRemoveSubscriber(long id, out ISubscriptionOnlineInfo info);

	/// <summary>
	/// Alias a transaction onto a subscription, for replies that name an order rather than the
	/// subscription reporting it.
	/// </summary>
	void AddAlias(long id, ISubscriptionOnlineInfo info);

	/// <summary>
	/// Drop an alias.
	/// </summary>
	void RemoveAlias(long id);

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

	/// <summary>
	/// True when this info is a linked view onto a main subscription
	/// (created via <see cref="ISubscriptionOnlineManagerState.CreateLinkedSubscriptionInfo"/>),
	/// not the main subscription itself. Mutating <see cref="State"/> or
	/// reading <see cref="Linked"/> on a linked view throws — callers that
	/// reach an info through a per-order TxId lookup must check this flag
	/// before treating the info as a main subscription.
	/// </summary>
	bool IsLinked { get; }
}
