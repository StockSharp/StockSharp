namespace StockSharp.Algo;

/// <summary>
/// Default implementation of <see cref="ILookupTrackingManagerState"/>.
/// </summary>
public class LookupTrackingManagerState : ILookupTrackingManagerState
{
	private class LookupInfo(ISubscriptionMessage subscription, TimeSpan left)
	{
		private readonly TimeSpan _initLeft = left;
		private TimeSpan _left = left;

		public ISubscriptionMessage Subscription { get; } = subscription ?? throw new ArgumentNullException(nameof(subscription));

		public bool ProcessTime(TimeSpan diff)
		{
			try
			{
				if (diff <= TimeSpan.Zero)
					return false;

				var left = _left - diff;

				if (left <= TimeSpan.Zero)
					return true;

				_left = left;
				return false;
			}
			catch (OverflowException ex)
			{
				throw new InvalidOperationException($"Left='{_left}' Diff='{diff}'", ex);
			}
		}

		public void IncreaseTimeOut()
		{
			_left = _initLeft;
		}
	}

	private readonly CachedSynchronizedDictionary<long, LookupInfo> _lookups = [];
	private readonly Dictionary<MessageTypes, Dictionary<long, ISubscriptionMessage>> _queue = [];

	/// <inheritdoc />
	public DateTime PreviousTime { get; set; }

	/// <inheritdoc />
	public void AddLookup(long transactionId, ISubscriptionMessage subscription, TimeSpan timeout)
	{
		using (_lookups.EnterScope())
			_lookups.Add(transactionId, new LookupInfo(subscription, timeout));
	}

	/// <inheritdoc />
	public bool TryGetAndRemoveLookup(long transactionId, out ISubscriptionMessage subscription)
	{
		using (_lookups.EnterScope())
		{
			if (_lookups.TryGetAndRemove(transactionId, out var info))
			{
				subscription = info.Subscription;
				return true;
			}

			subscription = null;
			return false;
		}
	}

	/// <inheritdoc />
	public void IncreaseTimeOut(long[] subscriptionIds)
	{
		using (_lookups.EnterScope())
		{
			foreach (var id in subscriptionIds)
			{
				if (_lookups.TryGetValue(id, out var info))
					info.IncreaseTimeOut();
			}
		}
	}

	/// <inheritdoc />
	public void RemoveLookup(long transactionId)
	{
		_lookups.Remove(transactionId);
	}

	/// <inheritdoc />
	public bool TryEnqueue(MessageTypes type, long transactionId, ISubscriptionMessage message)
	{
		using (_lookups.EnterScope())
		{
			var queue = _queue.SafeAdd(type);

			if (queue.TryAdd2(transactionId, message))
			{
				if (queue.Count > 1)
					return true;
			}

			return false;
		}
	}

	/// <inheritdoc />
	public Message TryDequeueNext(MessageTypes type, long removingId)
	{
		using (_lookups.EnterScope())
		{
			if (!_queue.TryGetValue(type, out var queue) || !queue.Remove(removingId))
				return null;

			if (queue.Count == 0)
			{
				_queue.Remove(type);
				return null;
			}

			return (Message)queue.First().Value;
		}
	}

	/// <inheritdoc />
	public Message TryDequeueFromAnyType(long removingId)
	{
		using (_lookups.EnterScope())
		{
			foreach (var type in _queue.Keys.ToArray())
			{
				var next = TryDequeueNextInternal(type, removingId);

				if (next != null)
					return next;
			}

			return null;
		}
	}

	private Message TryDequeueNextInternal(MessageTypes type, long removingId)
	{
		if (!_queue.TryGetValue(type, out var queue) || !queue.Remove(removingId))
			return null;

		if (queue.Count == 0)
		{
			_queue.Remove(type);
			return null;
		}

		return (Message)queue.First().Value;
	}

	/// <inheritdoc />
	public IEnumerable<(ISubscriptionMessage subscription, Message nextInQueue)> ProcessTimeouts(TimeSpan diff, long[] ignoreIds)
	{
		List<(ISubscriptionMessage, Message)> result = null;

		foreach (var pair in _lookups.CachedPairs)
		{
			var info = pair.Value;
			var transId = info.Subscription.TransactionId;

			if (ignoreIds != null && ignoreIds.Contains(transId))
				continue;

			if (!info.ProcessTime(diff))
				continue;

			_lookups.Remove(transId);

			Message next;
			using (_lookups.EnterScope())
				next = TryDequeueNextInternal(info.Subscription.Type, info.Subscription.TransactionId);

			result ??= [];
			result.Add((info.Subscription, next));
		}

		return result ?? [];
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_lookups.EnterScope())
		{
			PreviousTime = default;
			_lookups.Clear();
			_queue.Clear();
		}
	}
}
