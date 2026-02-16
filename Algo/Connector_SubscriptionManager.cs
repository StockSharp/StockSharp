namespace StockSharp.Algo;

/// <summary>
/// Manages subscriptions and their lifecycle.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnectorSubscriptionManager"/>.
/// </remarks>
/// <param name="logReceiver">Log receiver.</param>
/// <param name="transactionIdGenerator">Transaction id generator.</param>
/// <param name="sendUnsubscribeWhenDisconnected">Indicates whether to send unsubscribe requests while disconnected.</param>
public class ConnectorSubscriptionManager(ILogReceiver logReceiver, IdGenerator transactionIdGenerator, bool sendUnsubscribeWhenDisconnected)
{
	/// <summary>
	/// Actions produced by <see cref="ConnectorSubscriptionManager"/>.
	/// </summary>
	public sealed class Actions
	{
		/// <summary>
		/// Action produced by <see cref="ConnectorSubscriptionManager"/>.
		/// </summary>
		public readonly struct Item
		{
			/// <summary>
			/// Types of actions produced by <see cref="ConnectorSubscriptionManager"/>.
			/// </summary>
			public enum Types
			{
				/// <summary>
				/// Send a message into the adapter chain.
				/// </summary>
				SendInMessage,

				/// <summary>
				/// Register order status transaction id.
				/// </summary>
				AddOrderStatus,

				/// <summary>
				/// Remove order status transaction id.
				/// </summary>
				RemoveOrderStatus,
			}

			/// <summary>
			/// Action type.
			/// </summary>
			public Types Type { get; }

			/// <summary>
			/// Message to send.
			/// </summary>
			public Message Message { get; }

			/// <summary>
			/// Transaction id.
			/// </summary>
			public long TransactionId { get; }

			private Item(Types type, Message message, long transactionId)
			{
				Type = type;
				Message = message;
				TransactionId = transactionId;
			}

			/// <summary>
			/// Create a message send action.
			/// </summary>
			/// <param name="message">Message to send.</param>
			/// <returns>Action instance.</returns>
			public static Item SendInMessage(Message message)
			{
				if (message is null)
					throw new ArgumentNullException(nameof(message));

				return new(Types.SendInMessage, message, default);
			}

			/// <summary>
			/// Create an order status add action.
			/// </summary>
			/// <param name="transactionId">Transaction id.</param>
			/// <returns>Action instance.</returns>
			public static Item AddOrderStatusTransactionId(long transactionId)
				=> new(Types.AddOrderStatus, null, transactionId);

			/// <summary>
			/// Create an order status remove action.
			/// </summary>
			/// <param name="transactionId">Transaction id.</param>
			/// <returns>Action instance.</returns>
			public static Item RemoveOrderStatusTransactionId(long transactionId)
				=> new(Types.RemoveOrderStatus, null, transactionId);
		}

		/// <summary>
		/// Empty actions.
		/// </summary>
		public static readonly Actions Empty = new();

		/// <summary>
		/// Action list.
		/// </summary>
		public Item[] Items { get; init; } = [];
	}

	/// <summary>
	/// Subscription state holder.
	/// </summary>
	private sealed class SubscriptionInfo
	{
		private DateTime? _last;
		private ICandleMessage _currentCandle;

		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionInfo"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		public SubscriptionInfo(Subscription subscription)
		{
			Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));

			_last = subscription.From;

			var type = subscription.DataType;

			if (type == DataType.PositionChanges ||
				type == DataType.Securities ||
				type == DataType.Board ||
				type == DataType.DataTypeInfo)
			{
				LookupItems = [];
			}
		}

		/// <summary>
		/// Subscription instance.
		/// </summary>
		public Subscription Subscription { get; }

		/// <summary>
		/// Indicates that an unsubscribe request has been sent but response not yet received.
		/// </summary>
		public bool IsUnsubscribing { get; set; }

		/// <summary>
		/// Indicates that lookup response has been received.
		/// </summary>
		public bool HasResult { get; set; }

		/// <summary>
		/// Collected lookup items.
		/// </summary>
		public List<object> LookupItems { get; }

		/// <summary>
		/// Create a subscription request that continues from the last time.
		/// </summary>
		/// <returns>Subscription message.</returns>
		public ISubscriptionMessage CreateSubscriptionContinue()
		{
			var subscrMsg = Subscription.SubscriptionMessage.TypedClone();

			if (_last != null)
				subscrMsg.From = _last.Value;

			return subscrMsg;
		}

		/// <summary>
		/// Update last processed time.
		/// </summary>
		/// <param name="time">New time.</param>
		/// <returns><see langword="true"/> if updated.</returns>
		public bool UpdateLastTime(DateTime time)
		{
			if (_last == null || _last.Value <= time)
			{
				_last = time;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Update the current candle state.
		/// </summary>
		/// <param name="message">Candle message.</param>
		/// <param name="candle">Updated candle.</param>
		/// <returns><see langword="true"/> if updated.</returns>
		public bool UpdateCandle(CandleMessage message, out ICandleMessage candle)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			candle = null;

			if (_currentCandle != null && _currentCandle.OpenTime == message.OpenTime)
			{
				if (_currentCandle.State == CandleStates.Finished)
					return false;

				_currentCandle = message;
			}
			else
				_currentCandle = message;

			candle = _currentCandle;
			return true;
		}

		/// <inheritdoc />
		public override string ToString() => Subscription.ToString();
	}

	private sealed class ActionCollector
	{
		private readonly List<Actions.Item> _items = [];

		public void Add(Actions.Item action) => _items.Add(action);

		public Actions ToResult()
		{
			return _items.Count == 0
				? Actions.Empty
				: new Actions { Items = [.. _items] };
		}
	}

	private readonly Lock _syncObject = new();

	private readonly Dictionary<long, SubscriptionInfo> _subscriptions = [];
	private readonly Dictionary<long, (ISubscriptionMessage request, Subscription subscription)> _requests = [];
	private readonly List<SubscriptionInfo> _keeped = [];
	private readonly HashSet<long> _notFound = [];
	private readonly CachedSynchronizedSet<Subscription> _subscriptionsOnConnect = [];
	private IdGenerator _transactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));

	private bool _wasConnected;

	/// <summary>
	/// Transaction id generator.
	/// </summary>
	public IdGenerator TransactionIdGenerator
	{
		get => _transactionIdGenerator;
		set => _transactionIdGenerator = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Indicates whether to send unsubscribe requests while disconnected.
	/// </summary>
	public bool SendUnsubscribeWhenDisconnected { get; } = sendUnsubscribeWhenDisconnected;

	/// <summary>
	/// Current connection state.
	/// </summary>
	public ConnectionStates ConnectionState { get; set; } = ConnectionStates.Disconnected;

	/// <summary>
	/// Restore subscription on reconnect.
	/// </summary>
	/// <remarks>
	/// Normal case connect/disconnect.
	/// </remarks>
	public bool IsRestoreSubscriptionOnNormalReconnect { get; set; } = true;

	/// <summary>
	/// Send subscriptions on connect.
	/// </summary>
	public CachedSynchronizedSet<Subscription> SubscriptionsOnConnect => _subscriptionsOnConnect;

	/// <summary>
	/// Current subscriptions snapshot.
	/// </summary>
	public IEnumerable<Subscription> Subscriptions
	{
		get
		{
			using (_syncObject.EnterScope())
			{
				return [.. _subscriptions.Select(p => p.Value.Subscription)];
			}
		}
	}

	/// <summary>
	/// Clear internal state.
	/// </summary>
	public void ClearCache()
	{
		using (_syncObject.EnterScope())
		{
			_wasConnected = default;
			_subscriptions.Clear();
			_requests.Clear();
			_keeped.Clear();
			_notFound.Clear();
		}
	}

	/// <summary>
	/// Get securities that have active subscriptions for the specified data type.
	/// </summary>
	/// <param name="dataType">Data type.</param>
	/// <returns>Securities with active subscriptions.</returns>
	public IEnumerable<SecurityId> GetSubscribers(DataType dataType)
	{
		return Subscriptions
				.Where(s => s.DataType == dataType && s.State.IsActive())
				.Select(s => s.SecurityId)
				.WhereNotNull();
	}

	private void TryWriteLog(long id)
	{
		if (_notFound.Add(id))
			logReceiver.AddWarningLog(LocalizedStrings.SubscriptionNonExist, id);
	}

	private void Remove(long id)
	{
		_subscriptions.Remove(id);
		logReceiver.AddInfoLog(LocalizedStrings.SubscriptionRemoved, id);
	}

	private SubscriptionInfo TryGetInfo(long id, bool remove, DateTime? time, bool addLog)
	{
		using (_syncObject.EnterScope())
		{
			if (_subscriptions.TryGetValue(id, out var info))
			{
				if (remove)
					Remove(id);
				else if (time != null)
				{
					if (!info.UpdateLastTime(time.Value))
					{
						//return null;
					}
				}

				return info;
			}
		}

		if (addLog)
			TryWriteLog(id);

		return null;
	}

	/// <summary>
	/// Resolve subscriptions for the specified message.
	/// </summary>
	/// <param name="message">Message with subscription ids.</param>
	/// <returns>Subscriptions.</returns>
	public IEnumerable<Subscription> GetSubscriptions(ISubscriptionIdMessage message)
	{
		var time = message is IServerTimeMessage timeMsg ? timeMsg.ServerTime : (DateTime?)null;

		var processed = new HashSet<SubscriptionInfo>();

		foreach (var id in message.GetSubscriptionIds())
		{
			var info = TryGetInfo(id, false, time, true);

			if (info == null || info.IsUnsubscribing)
				continue;

			if (!processed.Add(info))
				continue;

			yield return info.Subscription;
		}
	}

	/// <summary>
	/// Try get subscription by id.
	/// </summary>
	/// <param name="id">Subscription id.</param>
	/// <param name="ignoreAll">Ignore "all securities" mapping.</param>
	/// <param name="remove">Remove from cache.</param>
	/// <param name="time">Optional server time.</param>
	/// <returns>Subscription or <see langword="null"/>.</returns>
	public Subscription TryGetSubscription(long id, bool ignoreAll, bool remove, DateTime? time)
	{
		return TryGetInfo(id, remove, time, true)?.Subscription;
	}

	private void ChangeState(SubscriptionInfo info, SubscriptionStates state)
	{
		var subscription = info.Subscription;
		subscription.State = subscription.State.ChangeSubscriptionState(state, subscription.TransactionId, logReceiver);
	}

	/// <summary>
	/// Process a subscription response message.
	/// </summary>
	/// <param name="response">Response message.</param>
	/// <param name="originalMsg">Original subscription request.</param>
	/// <param name="unexpectedCancelled">Indicates that active subscription was canceled due to error.</param>
	/// <param name="items">Collected lookup items.</param>
	/// <returns>Subscription instance.</returns>
	public Subscription ProcessResponse(SubscriptionResponseMessage response, out ISubscriptionMessage originalMsg, out bool unexpectedCancelled, out object[] items)
	{
		originalMsg = null;

		SubscriptionInfo info = null;

		items = [];

		try
		{
			using (_syncObject.EnterScope())
			{
				unexpectedCancelled = false;

				if (!_requests.TryGetValue(response.OriginalTransactionId, out var tuple))
				{
					originalMsg = null;
					return null;
				}

				// do not remove cause subscription can be interrupted after successful response
				//_requests.Remove(response.OriginalTransactionId);

				originalMsg = tuple.request;

				info = originalMsg.IsSubscribe
					? TryGetInfo(originalMsg.TransactionId, false, null, false)
					: TryGetInfo(originalMsg.OriginalTransactionId, true, null, false);

				if (info == null)
				{
					originalMsg = null;
					return null;
				}

				var subscription = info.Subscription;

				if (originalMsg.IsSubscribe)
				{
					if (response.IsOk())
					{
						ChangeState(info, SubscriptionStates.Active);
					}
					else
					{
						var wasActive = subscription.State.IsActive();

						ChangeState(info, SubscriptionStates.Error);

						Remove(subscription.TransactionId);

						unexpectedCancelled = wasActive;

						_requests.Remove(response.OriginalTransactionId);

						items = info.LookupItems?.CopyAndClear() ?? [];
					}
				}
				else
				{
					ChangeState(info, SubscriptionStates.Stopped);

					Remove(subscription.TransactionId);

					// remove subscribe and unsubscribe requests
					_requests.Remove(subscription.TransactionId);
					_requests.Remove(response.OriginalTransactionId);
				}

				return subscription;
			}
		}
		finally
		{
			if (info == null)
				TryWriteLog(response.OriginalTransactionId);
		}
	}

	private void AddSubscription(Subscription subscription, ActionCollector actions)
	{
		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		if (actions == null)
			throw new ArgumentNullException(nameof(actions));

		using (_syncObject.EnterScope())
		{
			if (subscription.TransactionId != 0)
			{
				_subscriptions.Remove(subscription.TransactionId);
				_requests.Remove(subscription.TransactionId);
			}

			subscription.TransactionId = TransactionIdGenerator.GetNextId();

			var info = new SubscriptionInfo(subscription);

			if (subscription.SubscriptionMessage is OrderStatusMessage)
				actions.Add(Actions.Item.AddOrderStatusTransactionId(subscription.TransactionId));

			_subscriptions.Add(subscription.TransactionId, info);
		}
	}

	/// <summary>
	/// Send a subscription request.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="isAllExtension">Indicates "all securities" extension.</param>
	/// <returns>Actions to apply.</returns>
	public Actions Subscribe(Subscription subscription, bool isAllExtension = false)
	{
		var actions = new ActionCollector();
		Subscribe(subscription, isAllExtension, actions);
		return actions.ToResult();
	}

	private void Subscribe(Subscription subscription, bool isAllExtension, ActionCollector actions)
	{
		AddSubscription(subscription, actions);

		var subscrMsg = subscription.SubscriptionMessage;
		var clone = subscrMsg.TypedClone();
		clone.Adapter = subscrMsg.Adapter;

		if (isAllExtension)
			clone.BackMode = subscrMsg.BackMode;

		SendRequest(clone, subscription, isAllExtension, actions);
	}

	/// <summary>
	/// Send an unsubscribe request.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <returns>Actions to apply.</returns>
	public Actions UnSubscribe(Subscription subscription)
	{
		var actions = new ActionCollector();
		UnSubscribe(subscription, actions);
		return actions.ToResult();
	}

	private void UnSubscribe(Subscription subscription, ActionCollector actions)
	{
		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		if (actions == null)
			throw new ArgumentNullException(nameof(actions));

		//if (!subscription.State.IsActive())
		//{
		//	_host.AddWarningLog(LocalizedStrings.SubscriptionInvalidState, subscription.TransactionId, subscription.State);
		//	return;
		//}

		var unsubscribe = subscription.SubscriptionMessage.TypedClone();

		unsubscribe.IsSubscribe = false;

		// some subscription can be only for subscribe
		if (unsubscribe.IsSubscribe)
			return;

		unsubscribe.OriginalTransactionId = subscription.TransactionId;

		if (subscription.SubscriptionMessage is OrderStatusMessage)
			actions.Add(Actions.Item.RemoveOrderStatusTransactionId(subscription.TransactionId));

		if (!SendUnsubscribeWhenDisconnected && ConnectionState != ConnectionStates.Connected)
		{
			_subscriptions.Remove(subscription.TransactionId);
			_requests.Remove(subscription.TransactionId);
		}
		else
		{
			using (_syncObject.EnterScope())
			{
				if (_subscriptions.TryGetValue(subscription.TransactionId, out var info))
					info.IsUnsubscribing = true;
			}

			unsubscribe.TransactionId = TransactionIdGenerator.GetNextId();
			SendRequest(unsubscribe, subscription, false, actions);
		}
	}

	private void SendRequest(ISubscriptionMessage request, Subscription subscription, bool isAllExtension, ActionCollector actions)
	{
		using (_syncObject.EnterScope())
			_requests.Add(request.TransactionId, (request, subscription));

		if (isAllExtension)
			logReceiver.AddVerboseLog("(ALL+) " + (request.IsSubscribe ? LocalizedStrings.SubscriptionSent : LocalizedStrings.UnSubscriptionSent), subscription.SecurityId, request);
		else
			logReceiver.AddDebugLog(request.IsSubscribe ? LocalizedStrings.SubscriptionSent : LocalizedStrings.UnSubscriptionSent, subscription.SecurityId, request);

		actions.Add(Actions.Item.SendInMessage((Message)request));
	}

	/// <summary>
	/// Handle connection event and restore subscriptions as needed.
	/// </summary>
	/// <param name="subscriptionFilter">Filter for <see cref="SubscriptionsOnConnect"/>.</param>
	/// <returns>Actions to apply.</returns>
	public Actions HandleConnected(Func<Subscription, bool> subscriptionFilter)
	{
		if (subscriptionFilter is null)
			throw new ArgumentNullException(nameof(subscriptionFilter));

		return HandleConnected([.. SubscriptionsOnConnect.Cache.Where(subscriptionFilter)]);
	}

	/// <summary>
	/// Handle connection event and restore subscriptions as needed.
	/// </summary>
	/// <param name="subscriptions">Default subscriptions.</param>
	/// <returns>Actions to apply.</returns>
	public Actions HandleConnected(Subscription[] subscriptions)
	{
		if (subscriptions is null)
			throw new ArgumentNullException(nameof(subscriptions));

		var actions = new ActionCollector();

		Subscription[] missingSubscriptions;

		using (_syncObject.EnterScope())
			missingSubscriptions = [.. subscriptions.Where(sub => (!_wasConnected || !SubscriptionsOnConnect.Contains(sub)) && !_subscriptions.ContainsKey(sub.TransactionId))];

		if (_wasConnected)
		{
			if (!IsRestoreSubscriptionOnNormalReconnect)
				return Actions.Empty;

			missingSubscriptions.ForEach(sub =>
			{
				logReceiver.AddVerboseLog($"adding default subscription {sub.DataType}");
				AddSubscription(sub, actions);
			});

			ReSubscribeAll(actions);
		}
		else
		{
			_wasConnected = true;

			missingSubscriptions.ForEach(sub =>
			{
				logReceiver.AddVerboseLog($"subscribing default subscription {sub.DataType}");
				Subscribe(sub, false, actions);
			});
		}

		return actions.ToResult();
	}

	private void ReSubscribeAll(ActionCollector actions)
	{
		if (actions == null)
			throw new ArgumentNullException(nameof(actions));

		logReceiver.AddInfoLog(nameof(ReSubscribeAll));

		var requests = new Dictionary<ISubscriptionMessage, SubscriptionInfo>();

		using (_syncObject.EnterScope())
		{
			_requests.Clear();

			foreach (var info in _subscriptions.Values.Concat(_keeped).Distinct())
			{
				var newId = TransactionIdGenerator.GetNextId();

				if (info.Subscription.SubscriptionMessage is OrderStatusMessage)
				{
					actions.Add(Actions.Item.RemoveOrderStatusTransactionId(info.Subscription.TransactionId));
					actions.Add(Actions.Item.AddOrderStatusTransactionId(newId));
				}

				info.HasResult = false;
				info.Subscription.TransactionId = newId;
				requests.Add(info.CreateSubscriptionContinue(), info);
			}

			_keeped.Clear();
			_subscriptions.Clear();

			foreach (var (_, info) in requests)
				_subscriptions.Add(info.Subscription.TransactionId, info);
		}

		foreach (var (subMsg, info) in requests)
		{
			SendRequest(subMsg, info.Subscription, false, actions);
		}
	}

	/// <summary>
	/// Unsubscribe all active subscriptions.
	/// </summary>
	/// <returns>Actions to apply.</returns>
	public Actions UnSubscribeAll()
	{
		logReceiver.AddInfoLog(nameof(UnSubscribeAll));

		var actions = new ActionCollector();

		var subscriptions = new List<Subscription>();

		using (_syncObject.EnterScope())
		{
			_keeped.Clear();
			_keeped.AddRange(_subscriptions.Values);

			subscriptions.AddRange(Subscriptions.Where(s => s.State.IsActive()));
		}

		foreach (var subscription in subscriptions)
		{
			UnSubscribe(subscription, actions);
		}

		return actions.ToResult();
	}

	/// <summary>
	/// Process a lookup response item.
	/// </summary>
	/// <typeparam name="T">Item type.</typeparam>
	/// <param name="message">Lookup message.</param>
	/// <param name="item">Lookup item.</param>
	/// <returns>Subscriptions that received the item.</returns>
	public IEnumerable<Subscription> ProcessLookupResponse<T>(ISubscriptionIdMessage message, T item)
	{
		var subscriptions = new List<Subscription>();

		foreach (var id in message.GetSubscriptionIds())
		{
			var info = TryGetInfo(id, false, null, true);

			if (info == null || info.HasResult)
				continue;

			if (info.LookupItems == null)
			{
				logReceiver.AddWarningLog(LocalizedStrings.UnknownType, info.Subscription.SubscriptionMessage);
				continue;
			}

			info.LookupItems.Add(item);
			subscriptions.Add(info.Subscription);
		}

		return subscriptions;
	}

	/// <summary>
	/// Process a subscription finished message.
	/// </summary>
	/// <param name="message">Finished message.</param>
	/// <param name="items">Collected lookup items.</param>
	/// <returns>Subscription instance.</returns>
	public Subscription ProcessSubscriptionFinishedMessage(SubscriptionFinishedMessage message, out object[] items)
	{
		using (_syncObject.EnterScope())
		{
			var info = TryGetInfo(message.OriginalTransactionId, true, null, true);

			if (info == null)
			{
				items = [];
				return null;
			}

			items = info.LookupItems?.CopyAndClear() ?? [];

			ChangeState(info, SubscriptionStates.Finished);
			_requests.Remove(message.OriginalTransactionId);

			return info.Subscription;
		}
	}

	/// <summary>
	/// Process a subscription online message.
	/// </summary>
	/// <param name="message">Online message.</param>
	/// <param name="items">Collected lookup items.</param>
	/// <returns>Subscription instance.</returns>
	public Subscription ProcessSubscriptionOnlineMessage(SubscriptionOnlineMessage message, out object[] items)
	{
		using (_syncObject.EnterScope())
		{
			var info = TryGetInfo(message.OriginalTransactionId, false, null, true);

			if (info == null)
			{
				items = [];
				return null;
			}

			items = info.LookupItems?.CopyAndClear() ?? [];

			ChangeState(info, SubscriptionStates.Online);

			return info.Subscription;
		}
	}

	/// <summary>
	/// Update candles for subscriptions and return updated results.
	/// </summary>
	/// <param name="message">Candle message.</param>
	/// <returns>Updated subscriptions with candles.</returns>
	public IEnumerable<(Subscription subscription, ICandleMessage candle)> UpdateCandles(CandleMessage message)
	{
		var results = new List<(Subscription, ICandleMessage)>();

		foreach (var subscriptionId in message.GetSubscriptionIds())
		{
			using (_syncObject.EnterScope())
			{
				if (!_subscriptions.TryGetValue(subscriptionId, out var info))
				{
					TryWriteLog(subscriptionId);
					continue;
				}

				if (!info.UpdateLastTime(message.OpenTime))
					continue;

				if (!info.UpdateCandle(message, out var candle))
					continue;

				results.Add((info.Subscription, candle));
			}
		}

		return results;
	}

}
