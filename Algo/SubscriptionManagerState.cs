namespace StockSharp.Algo;

/// <summary>
/// Default implementation of <see cref="ISubscriptionManagerState"/>.
/// </summary>
public class SubscriptionManagerState : ISubscriptionManagerState
{
	private class SubscriptionInfo(ISubscriptionMessage subscription)
	{
		public ISubscriptionMessage Subscription { get; } = subscription;
		public SubscriptionStates State { get; set; } = SubscriptionStates.Stopped;
	}

	private readonly Lock _sync = new();
	private readonly Dictionary<long, ISubscriptionMessage> _historicalRequests = [];
	private readonly Dictionary<long, SubscriptionInfo> _subscriptionsById = [];
	private readonly PairSet<long, long> _replaceId = [];
	private readonly HashSet<long> _allSecIdChilds = [];
	private readonly List<Message> _reMapSubscriptions = [];

	/// <inheritdoc />
	public int ReplaceIdCount
	{
		get
		{
			using (_sync.EnterScope())
				return _replaceId.Count;
		}
	}

	/// <inheritdoc />
	public int ReMapSubscriptionCount
	{
		get
		{
			using (_sync.EnterScope())
				return _reMapSubscriptions.Count;
		}
	}

	/// <inheritdoc />
	public void AddHistoricalRequest(long transactionId, ISubscriptionMessage subscription)
	{
		using (_sync.EnterScope())
			_historicalRequests.Add(transactionId, subscription);
	}

	/// <inheritdoc />
	public bool TryGetAndRemoveHistoricalRequest(long transactionId, out ISubscriptionMessage subscription)
	{
		using (_sync.EnterScope())
			return _historicalRequests.TryGetAndRemove(transactionId, out subscription);
	}

	/// <inheritdoc />
	public bool ContainsHistoricalRequest(long transactionId)
	{
		using (_sync.EnterScope())
			return _historicalRequests.ContainsKey(transactionId);
	}

	/// <inheritdoc />
	public bool TryGetHistoricalRequest(long transactionId, out ISubscriptionMessage subscription)
	{
		using (_sync.EnterScope())
			return _historicalRequests.TryGetValue(transactionId, out subscription);
	}

	/// <inheritdoc />
	public bool RemoveHistoricalRequest(long transactionId)
	{
		using (_sync.EnterScope())
			return _historicalRequests.Remove(transactionId);
	}

	/// <inheritdoc />
	public void AddSubscription(long transactionId, ISubscriptionMessage subscription, SubscriptionStates state)
	{
		using (_sync.EnterScope())
			_subscriptionsById.Add(transactionId, new SubscriptionInfo(subscription) { State = state });
	}

	/// <inheritdoc />
	public bool TryGetSubscription(long transactionId, out ISubscriptionMessage subscription, out SubscriptionStates state)
	{
		using (_sync.EnterScope())
		{
			if (_subscriptionsById.TryGetValue(transactionId, out var info))
			{
				subscription = info.Subscription;
				state = info.State;
				return true;
			}

			subscription = null;
			state = default;
			return false;
		}
	}

	/// <inheritdoc />
	public void UpdateSubscriptionState(long transactionId, SubscriptionStates newState)
	{
		using (_sync.EnterScope())
		{
			if (_subscriptionsById.TryGetValue(transactionId, out var info))
				info.State = newState;
		}
	}

	/// <inheritdoc />
	public bool RemoveSubscription(long transactionId)
	{
		using (_sync.EnterScope())
			return _subscriptionsById.Remove(transactionId);
	}

	/// <inheritdoc />
	public IEnumerable<(long transactionId, ISubscriptionMessage subscription)> GetActiveSubscriptions()
	{
		using (_sync.EnterScope())
		{
			return _subscriptionsById
				.Where(kvp => kvp.Value.State.IsActive())
				.Select(kvp => (kvp.Key, kvp.Value.Subscription))
				.ToArray();
		}
	}

	/// <inheritdoc />
	public void AddReplaceId(long newId, long originalId)
	{
		using (_sync.EnterScope())
			_replaceId.Add(newId, originalId);
	}

	/// <inheritdoc />
	public bool TryGetOriginalId(long newId, out long originalId)
	{
		using (_sync.EnterScope())
			return _replaceId.TryGetValue(newId, out originalId);
	}

	/// <inheritdoc />
	public bool TryGetNewId(long originalId, out long newId)
	{
		using (_sync.EnterScope())
			return _replaceId.TryGetKey(originalId, out newId);
	}

	/// <inheritdoc />
	public void RemoveReplaceId(long newId)
	{
		using (_sync.EnterScope())
			_replaceId.Remove(newId);
	}

	/// <inheritdoc />
	public bool ContainsReplaceId(long newId)
	{
		using (_sync.EnterScope())
			return _replaceId.ContainsKey(newId);
	}

	/// <inheritdoc />
	public void ClearReplaceIds()
	{
		using (_sync.EnterScope())
			_replaceId.Clear();
	}

	/// <inheritdoc />
	public void AddAllSecIdChild(long transactionId)
	{
		using (_sync.EnterScope())
			_allSecIdChilds.Add(transactionId);
	}

	/// <inheritdoc />
	public void AddReMapSubscription(Message message)
	{
		using (_sync.EnterScope())
			_reMapSubscriptions.Add(message);
	}

	/// <inheritdoc />
	public Message[] GetAndClearReMapSubscriptions()
	{
		using (_sync.EnterScope())
			return _reMapSubscriptions.CopyAndClear();
	}

	/// <inheritdoc />
	public void ClearReMapSubscriptions()
	{
		using (_sync.EnterScope())
			_reMapSubscriptions.Clear();
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_sync.EnterScope())
		{
			_historicalRequests.Clear();
			_subscriptionsById.Clear();
			_replaceId.Clear();
			_allSecIdChilds.Clear();
			_reMapSubscriptions.Clear();
		}
	}
}
