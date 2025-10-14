namespace StockSharp.Messages;

/// <summary>
/// Describes a subscription associated with a particular message-listener session.
/// </summary>
/// <typeparam name="TSession">The session type the subscription belongs to.</typeparam>
public interface ISubscription<TSession>
{
	/// <summary>
	/// Unique subscription/request identifier.
	/// </summary>
	long Id { get; }

	/// <summary>
	/// Current subscription state.
	/// </summary>
	SubscriptionStates State { get; set; }

	/// <summary>
	/// Indicates whether data delivery for the subscription is temporarily paused.
	/// </summary>
	/// <remarks>
	/// When <see langword="true"/>, messages for this subscription are not delivered to the receiver.
	/// </remarks>
	bool Suspend { get; }

	/// <summary>
	/// The set of message types that are expected/produced as responses for this subscription.
	/// </summary>
	IEnumerable<MessageTypes> Responses { get; }

	/// <summary>
	/// The session in which the subscription was created.
	/// </summary>
	TSession Session { get; }
}

/// <summary>
/// Subscription holder.
/// </summary>
/// <typeparam name="TSubscription">Subscription type.</typeparam>
/// <typeparam name="TSession">Session type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="SubscriptionHolder{TSubscription,TSession}"/>.
/// </remarks>
/// <param name="logs">Logs.</param>
public class SubscriptionHolder<TSubscription, TSession>(ILogReceiver logs)
	where TSubscription : class, ISecurityIdMessage, IDataTypeMessage, ISubscription<TSession>
	where TSession : class
{
	private readonly ReaderWriterLockSlim _rw = new(LockRecursionPolicy.NoRecursion);
	private readonly Dictionary<DataType, HashSet<TSubscription>> _subscriptionsByAllSec = [];
	private readonly Dictionary<(DataType dt, SecurityId secId), HashSet<TSubscription>> _subscriptionsBySec = [];
	private readonly Dictionary<long, TSubscription> _subscriptionsById = [];
	private readonly Dictionary<MessageTypes, HashSet<TSubscription>> _subscriptionsByType = [];
	private readonly Dictionary<TSession, HashSet<TSubscription>> _subscriptionsBySession = [];
	private readonly Dictionary<long, long> _unsubscribeRequests = [];
	private readonly HashSet<long> _nonFoundSubscriptions = [];
		
	private readonly ILogReceiver _logs = logs ?? throw new ArgumentNullException(nameof(logs));

	/// <summary>
	/// Get subscriptions for the specified session.
	/// </summary>
	/// <param name="session">Session.</param>
	/// <returns>Subscriptions.</returns>
	public IEnumerable<TSubscription> GetSubscriptions(TSession session)
	{
		if (session is null)
			throw new ArgumentNullException(nameof(session));

		_rw.EnterReadLock();

		try
		{
			return _subscriptionsBySession.TryGetValue(session, out var set)
				? set.ToArray()
				: [];
		}
		finally
		{
			_rw.ExitReadLock();
		}
	}

	/// <summary>
	/// Subscription changed event.
	/// </summary>
	public event Action<TSubscription> SubscriptionChanged;

	/// <summary>
	/// Add new subscription.
	/// </summary>
	/// <param name="info">Subscription.</param>
	public void Add(TSubscription info)
	{
		if (info is null)
			throw new ArgumentNullException(nameof(info));

		_rw.EnterWriteLock();

		try
		{
			// Reserve id first to avoid partial indexing on duplicate ids in concurrent scenarios
			if (info.Id != 0)
				_subscriptionsById.Add(info.Id, info);

			var secId = info.SecurityId;
			var dataType = info.DataType;

			if (dataType != null)
			{
				if (secId.IsAllSecurity())
					_subscriptionsByAllSec.SafeAdd(dataType).Add(info);
				else
					_subscriptionsBySec.SafeAdd((dataType, secId)).Add(info);
			}

			foreach (var type in info.Responses)
				_subscriptionsByType.SafeAdd(type).Add(info);

			// index by session as well (covers subscriptions with Id == 0)
			if (info.Session is not null)
				_subscriptionsBySession.SafeAdd(info.Session).Add(info);
		}
		finally
		{
			_rw.ExitWriteLock();
		}

		SubscriptionChanged?.Invoke(info);
	}

	/// <summary>
	/// Add unsubscribe request identifier.
	/// </summary>
	/// <param name="transactionId">Request identifier.</param>
	/// <param name="originalTransactionId">ID of the original message <see cref="ITransactionIdMessage.TransactionId"/> for which this message is a response.</param>
	public void AddUnsubscribeRequest(long transactionId, long originalTransactionId)
	{
		_rw.EnterWriteLock();

		try
		{
			_unsubscribeRequests.Add(transactionId, originalTransactionId);
		}
		finally
		{
			_rw.ExitWriteLock();
		}
	}

	/// <summary>
	/// Remove all subscriptions for the specified session.
	/// </summary>
	/// <param name="session">Session.</param>
	/// <returns>Subscriptions.</returns>
	public IEnumerable<TSubscription> Remove(TSession session)
	{
		if (session is null)
			throw new ArgumentNullException(nameof(session));

		var subscriptions = new HashSet<TSubscription>();

		_rw.EnterWriteLock();

		try
		{
			void tryRemove<TKey>(Dictionary<TKey, HashSet<TSubscription>> dict)
			{
				foreach (var pair in dict.ToArray())
				{
					var set = pair.Value;
					var removed = set.Where(r => r.Session == session).ToArray();

					foreach (var r in removed)
						set.Remove(r);

					subscriptions.AddRange(removed);

					if (set.Count == 0)
						dict.Remove(pair.Key);
				}
			}

			tryRemove(_subscriptionsByAllSec);
			tryRemove(_subscriptionsBySec);
			tryRemove(_subscriptionsByType);
			tryRemove(_subscriptionsBySession);

			var removeIds = _subscriptionsById.Where(p => p.Value.Session == session).Select(p => p.Key).ToArray();
			foreach (var key in removeIds)
			{
				if (_subscriptionsById.TryGetValue(key, out var sub))
				{
					_subscriptionsById.Remove(key);
					subscriptions.Add(sub);
				}
			}
		}
		finally
		{
			_rw.ExitWriteLock();
		}

		foreach (var subscription in subscriptions)
		{
			subscription.State = subscription.State.ChangeSubscriptionState(SubscriptionStates.Stopped, subscription.Id, _logs);
			SubscriptionChanged?.Invoke(subscription);
		}

		return subscriptions;
	}

	/// <summary>
	/// Remove subscription.
	/// </summary>
	/// <param name="info">Subscription.</param>
	public void Remove(TSubscription info)
	{
		if (info is null)
			throw new ArgumentNullException(nameof(info));

		_rw.EnterWriteLock();

		try
		{
			void tryRemove<TKey>(Dictionary<TKey, HashSet<TSubscription>> dict)
			{
				foreach (var pair in dict.ToArray())
				{
					var set = pair.Value;
					if (set.Remove(info) && set.Count == 0)
						dict.Remove(pair.Key);
				}
			}

			tryRemove(_subscriptionsByAllSec);
			tryRemove(_subscriptionsBySec);
			tryRemove(_subscriptionsByType);
			tryRemove(_subscriptionsBySession);

			_subscriptionsById.Remove(info.Id);
		}
		finally
		{
			_rw.ExitWriteLock();
		}
	}

	/// <summary>
	/// Try to get a subscription by the specified identifier.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <param name="info">The found subscription, if any.</param>
	/// <returns><see langword="true"/> if a subscription with the specified identifier exists; otherwise, <see langword="false"/>.</returns>
	public bool TryGetById(long id, out TSubscription info)
	{
		_rw.EnterReadLock();

		try
		{
			return _subscriptionsById.TryGetValue(id, out info);
		}
		finally
		{
			_rw.ExitReadLock();
		}
	}

	/// <summary>
	/// Clear state.
	/// </summary>
	public void Clear()
	{
		_rw.EnterWriteLock();

		try
		{
			_subscriptionsByType.Clear();
			_subscriptionsById.Clear();
			_subscriptionsBySec.Clear();
			_subscriptionsByAllSec.Clear();
			_subscriptionsBySession.Clear();
			_unsubscribeRequests.Clear();
			_nonFoundSubscriptions.Clear();
		}
		finally
		{
			_rw.ExitWriteLock();
		}
	}

	/// <summary>
	/// Determines whether any subscription exists for the specified data type and security.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <param name="securityId">Security ID.</param>
	/// <returns><see langword="true"/> if any subscription exists; otherwise, <see langword="false"/>.</returns>
	public bool HasSubscriptions(DataType dataType, SecurityId securityId)
	{
		_rw.EnterReadLock();

		try
		{
			var receivers = _subscriptionsByAllSec.TryGetValue(dataType) ?? _subscriptionsBySec.TryGetValue((dataType, securityId));
			return receivers != null && receivers.Count > 0;
		}
		finally
		{
			_rw.ExitReadLock();
		}
	}

	/// <summary>
	/// Try to get the subscription by the specified identifier and set its state to <see cref="SubscriptionStates.Stopped"/>.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <param name="info">The found subscription, if any.</param>
	/// <returns><see langword="true"/> if the subscription was found; otherwise, <see langword="false"/>.</returns>
	public bool TryGetSubscriptionAndStop(long id, out TSubscription info)
		=> TryGetSubscription(id, SubscriptionStates.Stopped, out info);

	/// <summary>
	/// Try to get the subscription by the specified identifier and switch to a new state if specified.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <param name="state">The state to set for the subscription, or <see langword="null"/> to leave unchanged.</param>
	/// <param name="info">The found subscription, if any.</param>
	/// <returns><see langword="true"/> if the subscription was found; otherwise, <see langword="false"/>.</returns>
	public bool TryGetSubscription(long id, SubscriptionStates? state, out TSubscription info)
	{
		SubscriptionStates? newState = null;

		_rw.EnterUpgradeableReadLock();

		try
		{
			if (!_subscriptionsById.TryGetValue(id, out var localInfo))
			{
				_rw.EnterWriteLock();

				try
				{
					if (_nonFoundSubscriptions.Add(id))
						_logs.AddWarningLog(LocalizedStrings.SubscriptionNonExist, id);
				}
				finally
				{
					_rw.ExitWriteLock();
				}

				info = null;
				return false;
			}

			if (state != null)
			{
				_rw.EnterWriteLock();

				try
				{
					localInfo.State = localInfo.State.ChangeSubscriptionState(state.Value, id, _logs);
					newState = state;
				}
				finally
				{
					_rw.ExitWriteLock();
				}
			}

			if (state?.IsActive() == false)
			{
				// remove under write lock
				Remove(localInfo);
			}

			info = localInfo;
		}
		finally
		{
			_rw.ExitUpgradeableReadLock();
		}

		if (newState != null)
			SubscriptionChanged?.Invoke(info);

		return true;
	}

	private IEnumerable<TSubscription> GetSubscriptions(MessageTypes type, long id)
	{
		if (id != 0)
		{
			SubscriptionStates? state = type switch
			{
				MessageTypes.SubscriptionOnline => SubscriptionStates.Online,
				MessageTypes.SubscriptionFinished or MessageTypes.ChangePassword or MessageTypes.Error => SubscriptionStates.Finished,
				_ => null,
			};

			return TryGetSubscription(id, state, out var sub)
				? ToSet(sub)
				: [];
		}

		_rw.EnterReadLock();

		try
		{
			return _subscriptionsByType.TryGetValue(type, out var set)
				? set.Where(i => !i.Suspend).ToArray()
				: [];
		}
		finally
		{
			_rw.ExitReadLock();
		}
	}

	private static IEnumerable<TSubscription> ToSet(TSubscription info, bool checkSuspend = true)
		=> info == null || (checkSuspend && info.Suspend) ? [] : [info];

	private IEnumerable<TSubscription> GetSubscriptions(DataType dataType, SecurityId securityId, long id)
	{
		if (id != 0)
			return TryGetSubscription(id, null, out var sub)
				? ToSet(sub)
				: [];

		if (dataType is null)
			return [];

		IEnumerable<TSubscription> subscriptions;

		_rw.EnterReadLock();

		try
		{
			subscriptions = _subscriptionsByAllSec.TryGetValue(dataType, out var setAll)
				? setAll.ToArray()
				: [];

			if (_subscriptionsBySec.TryGetValue((dataType, securityId), out var mdSubscriptions))
				subscriptions = [.. subscriptions, .. mdSubscriptions];
		}
		finally
		{
			_rw.ExitReadLock();
		}

		return subscriptions
			.Where(i => i.State == SubscriptionStates.Online && !i.Suspend) // non id messages only for online
			.Distinct();
	}

	/// <summary>
	/// Get subscription for the specified message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>Subscriptions.</returns>
	public IEnumerable<TSubscription> GetSubscriptions(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;
				var originTransId = execMsg.OriginalTransactionId;

				if (execMsg.DataType == DataType.Ticks || execMsg.DataType == DataType.OrderLog)
					return GetSubscriptions(execMsg.DataType, execMsg.SecurityId, originTransId);
				else if (execMsg.DataType == DataType.Transactions)
				{
					if (!TryGetById(originTransId, out var subscription))
						return [];

					if (execMsg.TransactionId != 0)
					{
						// TODO Many clients can subscribe on the same order id
						_rw.EnterWriteLock();

						try
						{
							_subscriptionsById[execMsg.TransactionId] = subscription;
						}
						finally
						{
							_rw.ExitWriteLock();
						}
					}

					return ToSet(subscription);
				}
				else
					throw new ArgumentOutOfRangeException(execMsg.DataType.To<string>());
			}

			case MessageTypes.QuoteChange:
				return GetSubscriptions(DataType.MarketDepth, ((QuoteChangeMessage)message).SecurityId, ((QuoteChangeMessage)message).OriginalTransactionId);

			case MessageTypes.Level1Change:
				return GetSubscriptions(DataType.Level1, ((Level1ChangeMessage)message).SecurityId, ((Level1ChangeMessage)message).OriginalTransactionId);

			case MessageTypes.SubscriptionResponse:
			{
				var responseMsg = (SubscriptionResponseMessage)message;
				var originId = responseMsg.OriginalTransactionId;

				TSubscription info;

				bool removed;
				long subscriptionId;

				_rw.EnterWriteLock();

				try
				{
					removed = _unsubscribeRequests.TryGetValue(originId, out subscriptionId);
					if (removed)
						_unsubscribeRequests.Remove(originId);
				}
				finally
				{
					_rw.ExitWriteLock();
				}

				if (removed)
				{
					return TryGetSubscription(subscriptionId, SubscriptionStates.Stopped, out info)
						? ToSet(info, false)
						: [];
				}
				else
				{
					return TryGetSubscription(originId, responseMsg.IsOk() ? SubscriptionStates.Active : SubscriptionStates.Error, out info)
						? ToSet(info, false)
						: [];
				}
			}

			default:
			{
				if (message is IOriginalTransactionIdMessage originTransId)
				{
					if (originTransId.OriginalTransactionId == 0)
					{
						if (message is ISubscriptionIdMessage subscrIdMsg && message is ISecurityIdMessage secIdMsg)
							return GetSubscriptions(subscrIdMsg.DataType, secIdMsg.SecurityId, 0);
					}
					else
						return GetSubscriptions(message.Type, originTransId.OriginalTransactionId);
				}

				return [];
			}
		}
	}
}