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

		public SubscriptionInfo(ISubscriptionMessage subscription, CachedSynchronizedDictionary<long, ISubscriptionMessage> subscribers)
		{
			Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
			Subscribers = subscribers ?? throw new ArgumentNullException(nameof(subscribers));
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

		/// <summary>
		/// The registry's own holder collection, handed over when the subscription was opened. There
		/// is one list of subscribers, not one here and one there.
		/// </summary>
		public CachedSynchronizedDictionary<long, ISubscriptionMessage> Subscribers { get; }

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

		public bool IsLinked => _main != null;

		/// <summary>The registry record this info is the payload of. A linked view has none.</summary>
		public SharedSubscriptionRegistry<(DataType, SecurityId), long, ISubscriptionMessage, SubscriptionInfo>.Entry Entry { get; set; }

		public override string ToString() => (_main != null ? "Linked: " : string.Empty) + Subscription.ToString();
	}

	// Who holds each shared subscription. The subscribers of one subscription, the lookup from a
	// subscriber to what it holds, and the subscription itself all live here together.
	private readonly SharedSubscriptionRegistry<(DataType, SecurityId), long, ISubscriptionMessage, SubscriptionInfo> _shared = new();

	// An order's own transaction pointing at the order-status subscription that reports it. Not a
	// holder of anything - it is an alias used to route replies, and it is removed with the
	// subscription that owns it.
	private readonly Dictionary<long, SubscriptionInfo> _aliases = [];

	private readonly HashSet<long> _skipSubscriptions = [];
	private readonly HashSet<long> _unsubscribeRequests = [];

	/// <inheritdoc />
	public ISubscriptionOnlineInfo CreateLinkedSubscriptionInfo(ISubscriptionOnlineInfo main)
		=> new SubscriptionInfo((SubscriptionInfo)main);

	/// <inheritdoc />
	public bool TryGetSubscriptionByKey((DataType dataType, SecurityId securityId) key, out ISubscriptionOnlineInfo info)
	{
		if (_shared.TryGetByKey(key, out var entry))
		{
			info = entry.Payload;
			return true;
		}

		info = null;
		return false;
	}

	/// <inheritdoc />
	public ISubscriptionOnlineInfo AddSubscriber((DataType dataType, SecurityId securityId) key, long subscriberId, ISubscriptionMessage request, Func<ISubscriptionMessage> createSubscription, out bool isOpener)
	{
		if (createSubscription is null)
			throw new ArgumentNullException(nameof(createSubscription));

		var entry = _shared.Add(key, subscriberId, request,
			subscribers => new SubscriptionInfo(createSubscription(), subscribers), out isOpener);

		if (entry is null)
			return null;

		entry.Payload.Entry = entry;

		return entry.Payload;
	}

	/// <inheritdoc />
	public bool RemoveSubscriber(long subscriberId, out ISubscriptionOnlineInfo info, out bool wasLast)
	{
		wasLast = _shared.Remove(subscriberId, out var entry);
		info = entry?.Payload;

		return info is not null;
	}

	/// <inheritdoc />
	public void StopSubscription(ISubscriptionOnlineInfo info)
	{
		if (((SubscriptionInfo)info)?.Entry is { } entry)
			_shared.Unkey(entry);
	}

	/// <inheritdoc />
	public void DiscardSubscription(ISubscriptionOnlineInfo info)
	{
		if (((SubscriptionInfo)info)?.Entry is not { } entry)
			return;

		foreach (var alias in entry.Payload.IsLinked ? [] : entry.Payload.Linked)
			_aliases.Remove(alias);

		_shared.Drop(entry);
	}

	/// <inheritdoc />
	public bool TryGetSubscriptionById(long id, out ISubscriptionOnlineInfo info)
	{
		if (_shared.TryGetByHolder(id, out var entry))
		{
			info = entry.Payload;
			return true;
		}

		if (_aliases.TryGetValue(id, out var alias))
		{
			info = alias;
			return true;
		}

		info = null;
		return false;
	}

	/// <inheritdoc />
	public bool ContainsSubscriptionById(long id)
		=> _shared.TryGetByHolder(id, out _) || _aliases.ContainsKey(id);

	/// <inheritdoc />
	public bool TryGetAndRemoveSubscriber(long id, out ISubscriptionOnlineInfo info)
	{
		if (_shared.Remove(id, out var entry))
		{
			info = entry.Payload;
			return true;
		}

		info = entry?.Payload;
		return info is not null;
	}

	/// <inheritdoc />
	public void AddAlias(long id, ISubscriptionOnlineInfo info)
		=> _aliases.Add(id, (SubscriptionInfo)info);

	/// <inheritdoc />
	public void RemoveAlias(long id)
		=> _aliases.Remove(id);

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
		_shared.Clear();
		_aliases.Clear();
		_skipSubscriptions.Clear();
		_unsubscribeRequests.Clear();
	}
}
