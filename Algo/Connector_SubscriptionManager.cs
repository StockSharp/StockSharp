namespace StockSharp.Algo;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Testing;

partial class Connector
{
	private class SubscriptionInfo
	{
		private DateTimeOffset? _last;
		private ICandleMessage _currentCandle;

		public SubscriptionInfo(Subscription subscription, SubscriptionInfo parent)
		{
			Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
			Parent = parent;

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

		public SubscriptionInfo Parent { get; }
		public Subscription Subscription { get; }
		public bool HasResult { get; set; }
		public List<object> LookupItems { get; }

		public Security Security { get; set; }
		public bool SecurityNotFound { get; set; }

		public ISubscriptionMessage CreateSubscriptionContinue()
		{
			var subscrMsg = Subscription.SubscriptionMessage.TypedClone();

			if (_last != null)
				subscrMsg.From = _last.Value;

			return subscrMsg;
		}

		public bool UpdateLastTime(DateTimeOffset time)
		{
			if (_last == null || _last.Value <= time)
			{
				_last = time;
				return true;
			}

			return false;
		}

		public bool UpdateCandle(CandleMessage message, out ICandleMessage candle)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			candle = null;

			if (_currentCandle != null && _currentCandle.OpenTime == message.OpenTime)
			{
				if (_currentCandle.State == CandleStates.Finished)
					return false;

#pragma warning disable CS0618 // Type or member is obsolete
				if (_currentCandle is Candle entity) // Subscription.DisableEntity is false
					entity.Update(message);
				else
#pragma warning restore CS0618 // Type or member is obsolete
					_currentCandle = message; // Subscription.DisableEntity is true
			}
			else
				_currentCandle = message;

			candle = _currentCandle;
			return true;
		}

		public override string ToString() => Subscription.ToString();
	}

	private class SubscriptionManager(Connector connector)
	{
		private readonly SyncObject _syncObject = new();

		private readonly Dictionary<long, SubscriptionInfo> _subscriptions = [];
		private readonly Dictionary<long, (ISubscriptionMessage request, Subscription subscription)> _requests = [];
		private readonly List<SubscriptionInfo> _keeped = [];
		private readonly HashSet<long> _notFound = [];
		private readonly Dictionary<long, long> _subscriptionAllMap = [];

		private readonly Connector _connector = connector ?? throw new ArgumentNullException(nameof(connector));
		private bool _wasConnected;

		public IEnumerable<Subscription> Subscriptions
		{
			get
			{
				lock (_syncObject)
				{
					return [.. _subscriptions.Select(p => p.Value.Subscription)];
				}
			}
		}

		public void ClearCache()
		{
			lock (_syncObject)
			{
				_wasConnected = default;
				_subscriptions.Clear();
				_requests.Clear();
				_keeped.Clear();
				_notFound.Clear();
				_subscriptionAllMap.Clear();
			}
		}

		public IEnumerable<Security> GetSubscribers(DataType dataType)
		{
			return Subscriptions
			       .Where(s => s.DataType == dataType && s.State.IsActive())
			       .Select(s => _connector.TryGetSecurity(s.SecurityId))
				   .WhereNotNull();
		}

		private void TryWriteLog(long id)
		{
			if (_notFound.Add(id))
				_connector.LogWarning(LocalizedStrings.SubscriptionNonExist, id);
		}

		private void Remove(long id)
		{
			_subscriptions.Remove(id);
			_connector.LogInfo(LocalizedStrings.SubscriptionRemoved, id);
		}

		private SubscriptionInfo TryGetInfo(long id, bool ignoreAll, bool remove, DateTimeOffset? time, bool addLog)
		{
			lock (_syncObject)
			{
				if (_subscriptionAllMap.ContainsKey(id))
				{
					if (ignoreAll)
						return null;

					if (remove)
						_subscriptionAllMap.Remove(id);

					//id = parentId;
				}

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

		public IEnumerable<Subscription> GetSubscriptions(ISubscriptionIdMessage message)
		{
			var time = message is IServerTimeMessage timeMsg ? timeMsg.ServerTime : (DateTimeOffset?)null;

			var processed = new HashSet<SubscriptionInfo>();

			foreach (var id in message.GetSubscriptionIds())
			{
				var info = TryGetSubscription(id, false, false, time);

				if (info == null)
					continue;

				if (!processed.Add(info))
					continue;

				if (info.Parent == null)
				{
					yield return info.Subscription;
				}
				else
				{
					if (!processed.Add(info.Parent))
						continue;

					yield return info.Parent.Subscription;
				}
			}
		}

		public SubscriptionInfo TryGetSubscription(long id, bool ignoreAll, bool remove, DateTimeOffset? time)
		{
			return TryGetInfo(id, ignoreAll, remove, time, true);
		}

		private void ChangeState(SubscriptionInfo info, SubscriptionStates state)
		{
			var subscription = info.Subscription;
			subscription.State = subscription.State.ChangeSubscriptionState(state, subscription.TransactionId, _connector);
		}

		public Subscription ProcessResponse(SubscriptionResponseMessage response, out ISubscriptionMessage originalMsg, out bool unexpectedCancelled, out object[] items)
		{
			originalMsg = null;

			SubscriptionInfo info = null;

			items = [];

			try
			{
				lock (_syncObject)
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
						? TryGetInfo(originalMsg.TransactionId, false, false, null, false)
						: TryGetInfo(originalMsg.OriginalTransactionId, false, true, null, false);

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
							ChangeState(info, SubscriptionStates.Error);

							Remove(subscription.TransactionId);

							unexpectedCancelled = subscription.State.IsActive();

							_requests.Remove(response.OriginalTransactionId);

							if (info.Parent == null)
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

					if (info.Parent != null)
					{
						originalMsg = null;
						return null;
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

		private void AddSubscription(Subscription subscription)
		{
			if (subscription == null)
				throw new ArgumentNullException(nameof(subscription));

			if (subscription.TransactionId == 0)
				subscription.TransactionId = _connector.TransactionIdGenerator.GetNextId();

			lock (_syncObject)
			{
				var info = new SubscriptionInfo(subscription, _subscriptionAllMap.TryGetValue(subscription.TransactionId, out var parentId) ? _subscriptions.TryGetValue(parentId) : null);

				if (subscription.SubscriptionMessage is OrderStatusMessage)
					_connector._entityCache.AddOrderStatusTransactionId(subscription.TransactionId);

				_subscriptions.Add(subscription.TransactionId, info);
			}
		}

		public void Subscribe(Subscription subscription, bool isAllExtension = false)
		{
			AddSubscription(subscription);

			var subscrMsg = subscription.SubscriptionMessage;
			var clone = subscrMsg.TypedClone();
			clone.Adapter = subscrMsg.Adapter;

			if (isAllExtension)
				clone.BackMode = subscrMsg.BackMode;

			SendRequest(clone, subscription, isAllExtension);
		}

		public void UnSubscribe(Subscription subscription)
		{
			if (subscription == null)
				throw new ArgumentNullException(nameof(subscription));

			//if (!subscription.State.IsActive())
			//{
			//	_connector.AddWarningLog(LocalizedStrings.SubscriptionInvalidState, subscription.TransactionId, subscription.State);
			//	return;
			//}

			var unsubscribe = subscription.SubscriptionMessage.TypedClone();

			unsubscribe.TransactionId = _connector.TransactionIdGenerator.GetNextId();
			unsubscribe.OriginalTransactionId = subscription.TransactionId;
			unsubscribe.IsSubscribe = false;

			// some subscription can be only for subscribe
			if (unsubscribe.IsSubscribe)
				return;

			if (subscription.SubscriptionMessage is OrderStatusMessage)
				_connector._entityCache.RemoveOrderStatusTransactionId(subscription.TransactionId);

			if (_connector is HistoryEmulationConnector && _connector.ConnectionState != ConnectionStates.Connected)
			{
				_subscriptions.Remove(subscription.TransactionId);
				_requests.Remove(subscription.TransactionId);
			}
			else
				SendRequest(unsubscribe, subscription, false);
		}

		private void SendRequest(ISubscriptionMessage request, Subscription subscription, bool isAllExtension)
		{
			lock (_syncObject)
				_requests.Add(request.TransactionId, (request, subscription));

			if(isAllExtension)
				_connector.LogVerbose("(ALL+) " + (request.IsSubscribe ? LocalizedStrings.SubscriptionSent : LocalizedStrings.UnSubscriptionSent), subscription.SecurityId, request);
			else
				_connector.LogDebug(request.IsSubscribe ? LocalizedStrings.SubscriptionSent : LocalizedStrings.UnSubscriptionSent, subscription.SecurityId, request);

			_connector.SendInMessage((Message)request);
		}

		public void HandleConnected(Subscription[] subscriptions)
		{
			if (subscriptions is null)
				throw new ArgumentNullException(nameof(subscriptions));

			Subscription[] missingSubscriptions;

			lock (_syncObject)
				missingSubscriptions = [.. subscriptions.Where(sub => (!_wasConnected || !_connector.SubscriptionsOnConnect.Contains(sub)) && !_subscriptions.ContainsKey(sub.TransactionId))];

			if (_wasConnected)
			{
				if (!_connector.IsRestoreSubscriptionOnNormalReconnect)
					return;

				missingSubscriptions.ForEach(sub =>
				{
					_connector.LogVerbose($"adding default subscription {sub.DataType}");
					AddSubscription(sub);
				});

				ReSubscribeAll();
			}
			else
			{
				_wasConnected = true;

				missingSubscriptions.ForEach(sub =>
				{
					_connector.LogVerbose($"subscribing default subscription {sub.DataType}");
					Subscribe(sub);
				});
			}
		}

		private void ReSubscribeAll()
		{
			_connector.LogInfo(nameof(ReSubscribeAll));

			var requests = new Dictionary<ISubscriptionMessage, SubscriptionInfo>();

			lock (_syncObject)
			{
				_requests.Clear();

				foreach (var info in _subscriptions.Values.Concat(_keeped).Distinct())
				{
					var newId = _connector.TransactionIdGenerator.GetNextId();

					if (info.Subscription.SubscriptionMessage is OrderStatusMessage)
					{
						_connector._entityCache.RemoveOrderStatusTransactionId(info.Subscription.TransactionId);
						_connector._entityCache.AddOrderStatusTransactionId(newId);
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
				SendRequest(subMsg, info.Subscription, false);
			}
		}

		public void UnSubscribeAll()
		{
			_connector.LogInfo(nameof(UnSubscribeAll));

			var subscriptions = new List<Subscription>();

			lock (_syncObject)
			{
				_keeped.Clear();
				_keeped.AddRange(_subscriptions.Values);

				subscriptions.AddRange(Subscriptions.Where(s => s.State.IsActive()));
			}

			foreach (var subscription in subscriptions)
			{
				UnSubscribe(subscription);
			}
		}

		public IEnumerable<Subscription> ProcessLookupResponse<T>(ISubscriptionIdMessage message, T item)
		{
			var subscriptions = new List<Subscription>();

			foreach (var id in message.GetSubscriptionIds())
			{
				var info = TryGetInfo(id, true, false, null, true);

				if (info == null || info.HasResult)
					continue;

				if (info.LookupItems == null)
				{
					_connector.AddWarningLog(LocalizedStrings.UnknownType, info.Subscription.SubscriptionMessage);
					continue;
				}

				info.LookupItems.Add(item);
				subscriptions.Add(info.Subscription);
			}

			return subscriptions;
		}

		public Subscription ProcessSubscriptionFinishedMessage(SubscriptionFinishedMessage message, out object[] items)
		{
			lock (_syncObject)
			{
				var info = TryGetSubscription(message.OriginalTransactionId, false, true, null);

				if (info == null)
				{
					items = [];
					return null;
				}

				if (info.Parent == null)
					items = info.LookupItems?.CopyAndClear() ?? [];
				else
					items = [];

				ChangeState(info, SubscriptionStates.Finished);
				_requests.Remove(message.OriginalTransactionId);

				if (info.Parent != null)
					return null;

				return info.Subscription;
			}
		}

		public Subscription ProcessSubscriptionOnlineMessage(SubscriptionOnlineMessage message, out object[] items)
		{
			lock (_syncObject)
			{
				var info = TryGetSubscription(message.OriginalTransactionId, false, false, null);

				if (info == null)
				{
					items = [];
					return null;
				}

				if (info.Parent == null)
					items = info.LookupItems?.CopyAndClear() ?? [];
				else
					items = [];

				ChangeState(info, SubscriptionStates.Online);

				if (info.Parent != null)
					return null;

				return info.Subscription;
			}
		}

		public IEnumerable<(Subscription subscription, ICandleMessage candle)> UpdateCandles(CandleMessage message)
		{
			foreach (var subscriptionId in message.GetSubscriptionIds())
			{
				SubscriptionInfo info;

				lock (_syncObject)
				{
					if (!_subscriptions.TryGetValue(subscriptionId, out info))
					{
						TryWriteLog(subscriptionId);
						continue;
					}

					if (info.Security == null)
					{
						if (info.SecurityNotFound)
							continue;

						var security = _connector.TryGetSecurity(info.Subscription.SecurityId);

						if (security == null)
						{
							info.SecurityNotFound = true;
							_connector.AddWarningLog(LocalizedStrings.SecurityNoFound.Put(info.Subscription.SecurityId));
							continue;
						}

						info.Security = security;
					}
				}

				if (!info.UpdateLastTime(message.OpenTime))
					continue;

				if (!info.UpdateCandle(message, out var candle))
					continue;

				yield return (info.Subscription, candle);
			}
		}

		public void SubscribeAll(SubscriptionSecurityAllMessage allMsg)
		{
			lock (_syncObject)
				_subscriptionAllMap.Add(allMsg.TransactionId, allMsg.ParentTransactionId);

			var mdMsg = new MarketDataMessage
			{
				Adapter = allMsg.Adapter,
				BackMode = allMsg.BackMode,
			};
			allMsg.CopyTo(mdMsg);
			Subscribe(new Subscription(mdMsg), true);
		}
	}
}
