namespace StockSharp.Algo;

/// <summary>
/// Default implementation of <see cref="IOrderBookIncrementManagerState"/>.
/// </summary>
public class OrderBookIncrementManagerState : IOrderBookIncrementManagerState
{
	private class BookInfo(SecurityId securityId, ILogReceiver parent)
	{
		public readonly OrderBookIncrementBuilder Builder = new(securityId) { Parent = parent };
		public readonly CachedSynchronizedSet<long> SubscriptionIds = [];
	}

	private readonly Lock _sync = new();
	private readonly Dictionary<long, BookInfo> _byId = [];
	private readonly Dictionary<SecurityId, BookInfo> _online = [];
	private readonly HashSet<long> _passThrough = [];
	private readonly CachedSynchronizedSet<long> _allSecSubscriptions = [];
	private readonly CachedSynchronizedSet<long> _allSecSubscriptionsPassThrough = [];

	/// <inheritdoc />
	public void AddSubscription(long transactionId, SecurityId securityId, ILogReceiver builderParent)
	{
		using (_sync.EnterScope())
		{
			var info = new BookInfo(securityId, builderParent);
			info.SubscriptionIds.Add(transactionId);
			_byId.Add(transactionId, info);
		}
	}

	/// <inheritdoc />
	public void AddPassThrough(long transactionId)
	{
		using (_sync.EnterScope())
			_passThrough.Add(transactionId);
	}

	/// <inheritdoc />
	public void AddAllSecSubscription(long transactionId)
	{
		_allSecSubscriptions.Add(transactionId);
	}

	/// <inheritdoc />
	public void AddAllSecPassThrough(long transactionId)
	{
		_allSecSubscriptionsPassThrough.Add(transactionId);
	}

	/// <inheritdoc />
	public void OnSubscriptionOnline(long transactionId)
	{
		using (_sync.EnterScope())
		{
			if (!_byId.TryGetValue(transactionId, out var info))
				return;

			var secId = info.Builder.SecurityId;

			if (_online.TryGetValue(secId, out var online))
			{
				online.SubscriptionIds.Add(transactionId);
				_byId[transactionId] = online;
			}
			else
			{
				_online.Add(secId, info);
			}
		}
	}

	/// <inheritdoc />
	public QuoteChangeMessage TryApply(long subscriptionId, QuoteChangeMessage quoteMsg, out long[] subscriptionIds)
	{
		subscriptionIds = null;

		using (_sync.EnterScope())
		{
			if (!_byId.TryGetValue(subscriptionId, out var info))
				return null;

			var newQuoteMsg = info.Builder.TryApply(quoteMsg, subscriptionId);

			if (newQuoteMsg == null)
				return null;

			subscriptionIds = info.SubscriptionIds.Cache;
		}

		if (_allSecSubscriptions.Count > 0)
			subscriptionIds = subscriptionIds.Concat(_allSecSubscriptions.Cache);

		return quoteMsg;
	}

	/// <inheritdoc />
	public bool IsPassThrough(long subscriptionId)
	{
		using (_sync.EnterScope())
			return _passThrough.Contains(subscriptionId);
	}

	/// <inheritdoc />
	public bool IsAllSecPassThrough(long subscriptionId)
	{
		return _allSecSubscriptionsPassThrough.Contains(subscriptionId);
	}

	/// <inheritdoc />
	public bool HasAnySubscriptions
	{
		get
		{
			using (_sync.EnterScope())
				return _allSecSubscriptions.Count > 0 ||
				       _allSecSubscriptionsPassThrough.Count > 0 ||
				       _byId.Count > 0 ||
				       _passThrough.Count > 0 ||
				       _online.Count > 0;
		}
	}

	/// <inheritdoc />
	public long[] GetAllSecSubscriptionIds()
	{
		return _allSecSubscriptions.Cache;
	}

	/// <inheritdoc />
	public bool ContainsSubscription(long subscriptionId)
	{
		using (_sync.EnterScope())
			return _byId.ContainsKey(subscriptionId);
	}

	/// <inheritdoc />
	public void RemoveSubscription(long id)
	{
		using (_sync.EnterScope())
		{
			var changeId = true;

			if (!_byId.TryGetAndRemove(id, out var info))
			{
				changeId = false;

				info = _online.FirstOrDefault(p => p.Value.SubscriptionIds.Contains(id)).Value;

				if (info == null)
				{
					_passThrough.Remove(id);
					_allSecSubscriptions.Remove(id);
					_allSecSubscriptionsPassThrough.Remove(id);
					return;
				}
			}

			var secId = info.Builder.SecurityId;

			if (info != _online.TryGetValue(secId))
				return;

			info.SubscriptionIds.Remove(id);

			var ids = info.SubscriptionIds.Cache;

			if (ids.Length == 0)
				_online.Remove(secId);
			else if (changeId && !_byId.ContainsKey(ids[0]))
				_byId.Add(ids[0], info);
		}
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_sync.EnterScope())
		{
			_byId.Clear();
			_online.Clear();
			_passThrough.Clear();
			_allSecSubscriptions.Clear();
			_allSecSubscriptionsPassThrough.Clear();
		}
	}
}
