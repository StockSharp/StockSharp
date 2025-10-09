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
	private readonly SynchronizedDictionary<long, long> _unsubscribeRequests = [];
	private readonly SynchronizedSet<long> _nonFoundSubscriptions = [];
		
	private readonly ILogReceiver _logs = logs ?? throw new ArgumentNullException(nameof(logs));

	/// <summary>
	/// Get subscription for the specified session.
	/// </summary>
	/// <param name="session">Session.</param>
	/// <returns>Subscriptions.</returns>
	public IEnumerable<TSubcription> GetSubscriptions(TSession session)
	{
		if (session is null)
			throw new ArgumentNullException(nameof(session));

		lock (_subscriptionsById.SyncRoot)
		{
			return [.. _subscriptionsById
				.Values
				.Where(s => s.Session == session)];
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

		if (info.Id != 0)
			_subscriptionsById.Add(info.Id, info);

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
	/// Remove session.
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
		void TryRemoveSubscription<TKey>(SynchronizedDictionary<TKey, CachedSynchronizedSet<TSubcription>> dict)
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

		TryRemoveSubscription(_subscriptionsByAllSec);
		TryRemoveSubscription(_subscriptionsBySec);
		TryRemoveSubscription(_subscriptionsByType);

		_subscriptionsById.Remove(info.Id);
	}

	/// <summary>
	/// Try get subscription by the specified identifier.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <returns>Subscription.</returns>
	public TSubcription TryGetById(long id) => _subscriptionsById.TryGetValue(id);

	/// <summary>
	/// Clear state.
	/// </summary>
	public void Clear()
	{
		_subscriptionsByType.Clear();
		_subscriptionsById.Clear();
		_subscriptionsBySec.Clear();
		_subscriptionsByAllSec.Clear();
		_unsubscribeRequests.Clear();
		_nonFoundSubscriptions.Clear();
	}

	/// <summary>
	/// Determines has subscription for the specified data type and security.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <param name="securityId">Security ID.</param>
	/// <returns>Check result.</returns>
	public bool HasSubscriptions(DataType dataType, SecurityId securityId)
	{
		var receivers = _subscriptionsByAllSec.TryGetValue(dataType) ?? _subscriptionsBySec.TryGetValue((dataType, securityId));
		return receivers != null && receivers.Count > 0;
	}

	/// <summary>
	/// Try get and stop subscription by the specified identifier.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <returns>Subscription.</returns>
	public TSubcription TryGetSubscriptionAndStop(long id)
		=> TryGetSubscription(id, SubscriptionStates.Stopped);

	/// <summary>
	/// Try get subscription by the specified identifier and swith into new state.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <param name="state">State.</param>
	/// <returns>Subscription.</returns>
	public TSubcription TryGetSubscription(long id, SubscriptionStates? state)
	{
		if (id == 0)
			return null;

		var info = TryGetById(id);

		if (info == null)
		{
			if (_nonFoundSubscriptions.TryAdd(id))
				_logs.AddWarningLog(LocalizedStrings.SubscriptionNonExist, id);

			return null;
		}

		if (state != null)
		{
			info.State = info.State.ChangeSubscriptionState(state.Value, id, _logs);
			SubscriptionChanged?.Invoke(info);
		}

		if (state?.IsActive() == false)
			Remove(info);

		return info;
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

			return ToSet(TryGetSubscription(id, state));
		}

		return _subscriptionsByType.TryGetValue(type)?.Cache.Where(i => !i.Suspend) ?? [];
	}

	private IEnumerable<TSubcription> ToSet(TSubcription info, bool checkSuspend = true)
		=> info == null || (checkSuspend && info.Suspend) ? [] : [info];

	private IEnumerable<TSubcription> GetSubscriptions(DataType dataType, SecurityId securityId, long id)
	{
		if (id != 0)
			return ToSet(TryGetSubscription(id, null));

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
					var subscription = TryGetById(originTransId);

					if (subscription == null)
						return [];

					if (execMsg.TransactionId != 0)
					{
						// TODO Many clients can subscribe on the same order id
						_subscriptionsById[execMsg.TransactionId] = subscription;
					}

					return ToSet(subscription);
				}
				else
					throw new ArgumentOutOfRangeException();
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
					info = TryGetSubscription(subscriptionId, SubscriptionStates.Stopped);
				else
					info = TryGetSubscription(originId, responseMsg.IsOk() ? SubscriptionStates.Active : SubscriptionStates.Error);

				return ToSet(info, false);
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