namespace StockSharp.Algo;

/// <summary>
/// Default implementation of <see cref="ISubscriptionOnlineManagerState"/>.
/// </summary>
public class SubscriptionOnlineManagerState : ISubscriptionOnlineManagerState
{
	private sealed class SubscriptionInfo : ISubscriptionOnlineInfo
	{
		private readonly SubscriptionInfo _main;

		public ISubscriptionMessage Subscription { get; }

		public SubscriptionInfo(ISubscriptionMessage subscription)
		{
			Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
			IsMarketData = subscription.DataType.IsMarketData;
		}

		public SubscriptionInfo(SubscriptionInfo main)
		{
			_main = main ?? throw new ArgumentNullException(nameof(main));
			Subscription = main.Subscription;
			Subscribers = main.Subscribers;
			IsMarketData = main.IsMarketData;
		}

		private void CheckOnLinked()
		{
			if (_main != null)
				throw new InvalidOperationException();
		}

		private SubscriptionStates _state = SubscriptionStates.Stopped;

		public SubscriptionStates State
		{
			get => _main?.State ?? _state;
			set
			{
				CheckOnLinked();
				_state = value;
			}
		}

		public HashSet<long> ExtraFilters { get; } = [];
		public CachedSynchronizedDictionary<long, ISubscriptionMessage> Subscribers { get; private init; } = [];
		public CachedSynchronizedSet<long> OnlineSubscribers { get; } = [];
		public SynchronizedSet<long> HistLive { get; } = [];
		public bool IsMarketData { get; }

		private readonly List<long> _linked = [];

		public List<long> Linked
		{
			get
			{
				CheckOnLinked();
				return _linked;
			}
		}

		public override string ToString() => (_main != null ? "Linked: " : string.Empty) + Subscription.ToString();
	}

	private readonly AsyncLock _sync = new();
	private readonly PairSet<(DataType, SecurityId), SubscriptionInfo> _subscriptionsByKey = [];
	private readonly Dictionary<long, SubscriptionInfo> _subscriptionsById = [];
	private readonly HashSet<long> _skipSubscriptions = [];
	private readonly HashSet<long> _unsubscribeRequests = [];

	/// <inheritdoc />
	public ISubscriptionOnlineInfo CreateSubscriptionInfo(ISubscriptionMessage subscription)
		=> new SubscriptionInfo(subscription);

	/// <inheritdoc />
	public ISubscriptionOnlineInfo CreateLinkedSubscriptionInfo(ISubscriptionOnlineInfo main)
		=> new SubscriptionInfo((SubscriptionInfo)main);

	/// <inheritdoc />
	public bool TryGetSubscriptionByKey((DataType dataType, SecurityId securityId) key, out ISubscriptionOnlineInfo info)
	{
		if (_subscriptionsByKey.TryGetValue(key, out var result))
		{
			info = result;
			return true;
		}

		info = null;
		return false;
	}

	/// <inheritdoc />
	public void AddSubscriptionByKey((DataType dataType, SecurityId securityId) key, ISubscriptionOnlineInfo info)
		=> _subscriptionsByKey.Add(key, (SubscriptionInfo)info);

	/// <inheritdoc />
	public void RemoveSubscriptionByKeyValue(ISubscriptionOnlineInfo info)
		=> _subscriptionsByKey.RemoveByValue((SubscriptionInfo)info);

	/// <inheritdoc />
	public bool TryGetSubscriptionById(long id, out ISubscriptionOnlineInfo info)
	{
		if (_subscriptionsById.TryGetValue(id, out var result))
		{
			info = result;
			return true;
		}

		info = null;
		return false;
	}

	/// <inheritdoc />
	public bool TryGetAndRemoveSubscriptionById(long id, out ISubscriptionOnlineInfo info)
	{
		if (_subscriptionsById.TryGetAndRemove(id, out var result))
		{
			info = result;
			return true;
		}

		info = null;
		return false;
	}

	/// <inheritdoc />
	public bool ContainsSubscriptionById(long id)
		=> _subscriptionsById.ContainsKey(id);

	/// <inheritdoc />
	public void AddSubscriptionById(long id, ISubscriptionOnlineInfo info)
		=> _subscriptionsById.Add(id, (SubscriptionInfo)info);

	/// <inheritdoc />
	public void RemoveSubscriptionById(long id)
		=> _subscriptionsById.Remove(id);

	/// <inheritdoc />
	public void AddSkipSubscription(long id)
		=> _skipSubscriptions.Add(id);

	/// <inheritdoc />
	public bool RemoveSkipSubscription(long id)
		=> _skipSubscriptions.Remove(id);

	/// <inheritdoc />
	public bool ContainsSkipSubscription(long id)
		=> _skipSubscriptions.Contains(id);

	/// <inheritdoc />
	public void AddUnsubscribeRequest(long id)
		=> _unsubscribeRequests.Add(id);

	/// <inheritdoc />
	public bool ContainsUnsubscribeRequest(long id)
		=> _unsubscribeRequests.Contains(id);

	/// <inheritdoc />
	public void Clear()
	{
		_subscriptionsByKey.Clear();
		_subscriptionsById.Clear();
		_skipSubscriptions.Clear();
		_unsubscribeRequests.Clear();
	}
}
