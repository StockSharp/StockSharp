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
/// <typeparam name="TSubcription">Subscription type.</typeparam>
/// <typeparam name="TSession">Session type.</typeparam>
/// <typeparam name="TRequestId">Request identifier type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="SubscriptionHolder{TSubcription,TSession,TRequestId}"/>.
/// </remarks>
/// <param name="logs">Logs.</param>
public class SubscriptionHolder<TSubcription, TSession, TRequestId>(ILogReceiver logs)
	where TSubcription : class, ISecurityIdMessage, IDataTypeMessage, ISubscription<TSession>
	where TSession : class
{
	private readonly SynchronizedDictionary<DataType, CachedSynchronizedSet<TSubcription>> _subscriptionsByAllSec = [];
	private readonly SynchronizedDictionary<(DataType dt, SecurityId secId), CachedSynchronizedSet<TSubcription>> _subscriptionsBySec = [];
	private readonly SynchronizedDictionary<long, TSubcription> _subscriptionsById = [];
	private readonly SynchronizedDictionary<MessageTypes, CachedSynchronizedSet<TSubcription>> _subscriptionsByType = [];
	private readonly SynchronizedDictionary<TSession, CachedSynchronizedSet<TSubcription>> _subscriptionsBySession = [];
	private readonly SynchronizedDictionary<long, long> _unsubscribeRequests = [];
	private readonly SynchronizedSet<long> _nonFoundSubscriptions = [];
		
	private readonly ILogReceiver _logs = logs ?? throw new ArgumentNullException(nameof(logs));

	/// <summary>
	/// Get subscriptions for the specified session.
	/// </summary>
	/// <param name="session">Session.</param>
	/// <returns>Subscriptions.</returns>
	public IEnumerable<TSubcription> GetSubscriptions(TSession session)
	{
		if (session is null)
			throw new ArgumentNullException(nameof(session));

		lock (_subscriptionsBySession.SyncRoot)
		{
			return [.. _subscriptionsBySession.TryGetValue(session)?.Cache ?? Enumerable.Empty<TSubcription>()];
		}
	}

	/// <summary>
	/// Subscription changed event.
	/// </summary>
	public event Action<TSubcription> SubscriptionChanged;

	/// <summary>
	/// Add new subscription.
	/// </summary>
	/// <param name="info">Subscription.</param>
	public void Add(TSubcription info)
	{
		if (info is null)
			throw new ArgumentNullException(nameof(info));

		// Reserve id first to avoid partial indexing on duplicate ids in concurrent scenarios
		if (info.Id != 0)
		{
			lock (_subscriptionsById.SyncRoot)
				_subscriptionsById.Add(info.Id, info);
		}

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

		SubscriptionChanged?.Invoke(info);
	}

	/// <summary>
	/// Add unsubscribe request identifier.
	/// </summary>
	/// <param name="transactionId">Request identifier.</param>
	/// <param name="originalTransactionId">ID of the original message <see cref="ITransactionIdMessage.TransactionId"/> for which this message is a response.</param>
	public void AddUnsubscribeRequest(long transactionId, long originalTransactionId)
	{
		_unsubscribeRequests.Add(transactionId, originalTransactionId);
	}

	/// <summary>
	/// Remove all subscriptions for the specified session.
	/// </summary>
	/// <param name="session">Session.</param>
	/// <returns>Subscriptions.</returns>
	public IEnumerable<TSubcription> Remove(TSession session)
	{
		if (session is null)
			throw new ArgumentNullException(nameof(session));

		var subscriptions = new HashSet<TSubcription>();

		void TryRemoveSubscription<TKey>(SynchronizedDictionary<TKey, CachedSynchronizedSet<TSubcription>> dict)
		{
			lock (dict.SyncRoot)
			{
				foreach (var pair in dict.ToArray())
				{
					var set = pair.Value;

					subscriptions.AddRange(set.RemoveWhere(r => r.Session == session));

					if (set.Count == 0)
						dict.Remove(pair.Key);
				}
			}
		}

		TryRemoveSubscription(_subscriptionsByAllSec);
		TryRemoveSubscription(_subscriptionsBySec);
		TryRemoveSubscription(_subscriptionsByType);
		TryRemoveSubscription(_subscriptionsBySession);

		lock (_subscriptionsById.SyncRoot)
			subscriptions.AddRange(_subscriptionsById.RemoveWhere(p => p.Value.Session == session).Select(p => p.Value));

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
	public void Remove(TSubcription info)
	{
		if (info is null)
			throw new ArgumentNullException(nameof(info));

		void tryRemove<TKey>(SynchronizedDictionary<TKey, CachedSynchronizedSet<TSubcription>> dict)
		{
			lock (dict.SyncRoot)
			{
				foreach (var pair in dict.ToArray())
				{
					var set = pair.Value;

					if (set.Remove(info) && set.Count == 0)
						dict.Remove(pair.Key);
				}
			}
		}

		tryRemove(_subscriptionsByAllSec);
		tryRemove(_subscriptionsBySec);
		tryRemove(_subscriptionsByType);
		tryRemove(_subscriptionsBySession);

		_subscriptionsById.Remove(info.Id);
	}

	/// <summary>
	/// Try to get a subscription by the specified identifier.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <param name="info">The found subscription, if any.</param>
	/// <returns><see langword="true"/> if a subscription with the specified identifier exists; otherwise, <see langword="false"/>.</returns>
	public bool TryGetById(long id, out TSubcription info)
		=> _subscriptionsById.TryGetValue(id, out info);

	/// <summary>
	/// Clear state.
	/// </summary>
	public void Clear()
	{
		_subscriptionsByType.Clear();
		_subscriptionsById.Clear();
		_subscriptionsBySec.Clear();
		_subscriptionsByAllSec.Clear();
		_subscriptionsBySession.Clear();
		_unsubscribeRequests.Clear();
		_nonFoundSubscriptions.Clear();
	}

	/// <summary>
	/// Determines whether any subscription exists for the specified data type and security.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <param name="securityId">Security ID.</param>
	/// <returns><see langword="true"/> if any subscription exists; otherwise, <see langword="false"/>.</returns>
	public bool HasSubscriptions(DataType dataType, SecurityId securityId)
	{
		var receivers = _subscriptionsByAllSec.TryGetValue(dataType) ?? _subscriptionsBySec.TryGetValue((dataType, securityId));
		return receivers != null && receivers.Count > 0;
	}

	/// <summary>
	/// Try to get the subscription by the specified identifier and set its state to <see cref="SubscriptionStates.Stopped"/>.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <param name="info">The found subscription, if any.</param>
	/// <returns><see langword="true"/> if the subscription was found; otherwise, <see langword="false"/>.</returns>
	public bool TryGetSubscriptionAndStop(long id, out TSubcription info)
		=> TryGetSubscription(id, SubscriptionStates.Stopped, out info);

	/// <summary>
	/// Try to get the subscription by the specified identifier and switch to a new state if specified.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <param name="state">The state to set for the subscription, or <see langword="null"/> to leave unchanged.</param>
	/// <param name="info">The found subscription, if any.</param>
	/// <returns><see langword="true"/> if the subscription was found; otherwise, <see langword="false"/>.</returns>
	public bool TryGetSubscription(long id, SubscriptionStates? state, out TSubcription info)
	{
		if (!TryGetById(id, out info))
		{
			if (_nonFoundSubscriptions.TryAdd(id))
				_logs.AddWarningLog(LocalizedStrings.SubscriptionNonExist, id);

			return false;
		}

		if (state != null)
		{
			info.State = info.State.ChangeSubscriptionState(state.Value, id, _logs);
			SubscriptionChanged?.Invoke(info);
		}

		if (state?.IsActive() == false)
			Remove(info);

		return true;
	}

	private IEnumerable<TSubcription> GetSubscriptions(MessageTypes type, long id)
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

		return _subscriptionsByType.TryGetValue(type)?.Cache.Where(i => !i.Suspend) ?? [];
	}

	private static IEnumerable<TSubcription> ToSet(TSubcription info, bool checkSuspend = true)
		=> info == null || (checkSuspend && info.Suspend) ? [] : [info];

	private IEnumerable<TSubcription> GetSubscriptions(DataType dataType, SecurityId securityId, long id)
	{
		if (id != 0)
			return TryGetSubscription(id, null, out var sub)
				? ToSet(sub)
				: [];

		if (dataType is null)
			return [];

		var subscriptions = _subscriptionsByAllSec.TryGetValue(dataType)?.Cache ?? Enumerable.Empty<TSubcription>();

		if (_subscriptionsBySec.TryGetValue((dataType, securityId), out var mdSubscriptions))
			subscriptions = subscriptions.Concat(mdSubscriptions.Cache);

		return subscriptions
			.Where(i => i.State == SubscriptionStates.Online && !i.Suspend) // non id messages only for online
			.Distinct();
	}

	/// <summary>
	/// Get subscription for the specified message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>Subscriptions.</returns>
	public IEnumerable<TSubcription> GetSubscriptions(Message message)
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
						_subscriptionsById[execMsg.TransactionId] = subscription;
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

				TSubcription info;

				if (_unsubscribeRequests.TryGetAndRemove(originId, out var subscriptionId))
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