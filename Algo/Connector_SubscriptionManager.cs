namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	partial class Connector
	{
		private class SubscriptionInfo
		{
			private DateTimeOffset? _last;
			private Candle _currentCandle;

			public SubscriptionInfo(Subscription subscription, SubscriptionInfo parent)
			{
				Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
				Parent = parent;

				_last = subscription.SubscriptionMessage.From;

				var type = subscription.DataType;

				if (type == DataType.PositionChanges ||
					type == DataType.Securities ||
					type == DataType.Board ||
					type == DataType.TimeFrames)
				{
					LookupItems = new List<object>();
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

			public bool UpdateCandle(CandleMessage message, out Candle candle)
			{
				if (message == null)
					throw new ArgumentNullException(nameof(message));

				candle = null;

				if (_currentCandle != null && _currentCandle.OpenTime == message.OpenTime)
				{
					if (_currentCandle.State == CandleStates.Finished)
						return false;

					_currentCandle.Update(message);
				}
				else
					_currentCandle = message.ToCandle(Security);

				candle = _currentCandle;
				return true;
			}

			public override string ToString() => Subscription.ToString();
		}

		private class SubscriptionManager
		{
			private readonly SyncObject _syncObject = new SyncObject();

			private readonly Dictionary<long, SubscriptionInfo> _subscriptions = new Dictionary<long, SubscriptionInfo>();
			private readonly Dictionary<long, Tuple<ISubscriptionMessage, Subscription>> _requests = new Dictionary<long, Tuple<ISubscriptionMessage, Subscription>>();
			private readonly List<SubscriptionInfo> _keeped = new List<SubscriptionInfo>();
			private readonly HashSet<long> _notFound = new HashSet<long>();
			private readonly Dictionary<long, long> _subscriptionAllMap = new Dictionary<long, long>();
			
			private readonly Connector _connector;

			public SubscriptionManager(Connector connector)
			{
				_connector = connector ?? throw new ArgumentNullException(nameof(connector));
			}

			public IEnumerable<Subscription> Subscriptions
			{
				get
				{
					lock (_syncObject)
					{
						return _subscriptions
						       .Select(p => p.Value.Subscription)
						       .ToArray();
					}
				}
			}

			public void ClearCache()
			{
				lock (_syncObject)
				{
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
					   .Where(s => s != null);
			}

			public IEnumerable<Portfolio> SubscribedPortfolios => Subscriptions.Select(s => s.Portfolio).Where(p => p != null);

			public IEnumerable<CandleSeries> SubscribedCandleSeries => Subscriptions.Select(s => s.CandleSeries).Where(p => p != null);

			private void TryWriteLog(long id)
			{
				if (_notFound.Add(id))
					_connector.AddWarningLog(LocalizedStrings.SubscriptionNonExist, id);
			}

			private void Remove(long id)
			{
				// keed subscription instancies for tracing purpose
				//_subscriptions.Remove(id);
				_connector.AddInfoLog(LocalizedStrings.SubscriptionRemoved, id);
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

			public void RegisterPortfolio(Portfolio portfolio)
			{
				if (portfolio == null)
					throw new ArgumentNullException(nameof(portfolio));

				if (portfolio is BasketPortfolio basketPortfolio)
					basketPortfolio.InnerPortfolios.ForEach(_connector.RegisterPortfolio);
				else
					Subscribe(new Subscription(portfolio));
			}

			public void UnRegisterPortfolio(Portfolio portfolio)
			{
				if (portfolio == null)
					throw new ArgumentNullException(nameof(portfolio));

				if (portfolio is BasketPortfolio basketPortfolio)
					basketPortfolio.InnerPortfolios.ForEach(_connector.UnRegisterPortfolio);
				else
				{
					var subscription = Subscriptions.FirstOrDefault(s => s.Portfolio == portfolio);

					if (subscription == null)
						_connector.AddWarningLog(LocalizedStrings.SubscriptionNonExist, portfolio);
					else
						UnSubscribe(subscription);
				}
			}

			private void ChangeState(SubscriptionInfo info, SubscriptionStates state)
			{
				var subscription = info.Subscription;
				subscription.State = subscription.State.ChangeSubscriptionState(state, subscription.TransactionId, _connector, info.Parent == null);
			}

			public Subscription ProcessResponse(SubscriptionResponseMessage response, out ISubscriptionMessage originalMsg, out bool unexpectedCancelled)
			{
				originalMsg = null;

				SubscriptionInfo info = null;

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

						originalMsg = tuple.Item1;

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

			public void Subscribe(Subscription subscription, bool keepBackMode = false)
			{
				if (subscription == null)
					throw new ArgumentNullException(nameof(subscription));

				if (subscription.TransactionId == 0)
					subscription.TransactionId = _connector.TransactionIdGenerator.GetNextId();

				var subscrMsg = subscription.SubscriptionMessage;

				lock (_syncObject)
				{
					var info = new SubscriptionInfo(subscription, _subscriptionAllMap.TryGetValue(subscription.TransactionId, out var parentId) ? _subscriptions.TryGetValue(parentId) : null);

					if (subscrMsg is OrderStatusMessage)
						_connector._entityCache.AddOrderStatusTransactionId(subscription.TransactionId);

					_subscriptions.Add(subscription.TransactionId, info);
				}

				var clone = subscrMsg.TypedClone();
				clone.Adapter = subscrMsg.Adapter;

				if (keepBackMode)
					clone.BackMode = subscrMsg.BackMode;

				SendRequest(clone, subscription, !keepBackMode);
			}

			public void UnSubscribe(Subscription subscription)
			{
				if (subscription == null)
					throw new ArgumentNullException(nameof(subscription));

				if (!subscription.State.IsActive())
				{
					_connector.AddWarningLog(LocalizedStrings.SubscriptionInvalidState, subscription.TransactionId, subscription.State);
					return;
				}

				var unsubscribe = subscription.SubscriptionMessage.TypedClone();

				unsubscribe.TransactionId = _connector.TransactionIdGenerator.GetNextId();
				unsubscribe.OriginalTransactionId = subscription.TransactionId;
				unsubscribe.IsSubscribe = false;

				// some subscription can be only for subscribe
				if (unsubscribe.IsSubscribe)
					return;

				SendRequest(unsubscribe, subscription, true);
			}

			private void SendRequest(ISubscriptionMessage request, Subscription subscription, bool needLog)
			{
				lock (_syncObject)
					_requests.Add(request.TransactionId, Tuple.Create(request, subscription));

				if (needLog)
					_connector.AddInfoLog(request.IsSubscribe ? LocalizedStrings.SubscriptionSent : LocalizedStrings.UnSubscriptionSent, subscription.SecurityId, request);

				_connector.SendInMessage((Message)request);
			}

			public void ReSubscribeAll()
			{
				_connector.AddInfoLog(nameof(ReSubscribeAll));

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

					foreach (var pair in requests)
						_subscriptions.Add(pair.Value.Subscription.TransactionId, pair.Value);
				}

				foreach (var pair in requests)
				{
					SendRequest(pair.Key, pair.Value.Subscription, true);
				}
			}

			public void UnSubscribeAll()
			{
				_connector.AddInfoLog(nameof(UnSubscribeAll));

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
				return ProcessLookupResponse(message, new[] { item });
			}

			public IEnumerable<Subscription> ProcessLookupResponse<T>(ISubscriptionIdMessage message, T[] items)
			{
				var subscriptions = new List<Subscription>();

				foreach (var id in message.GetSubscriptionIds())
				{
					var info = TryGetInfo(id, true, false, null, true);

					if (info == null || info.HasResult)
						continue;

					if (info.LookupItems == null)
					{
						_connector.AddWarningLog(LocalizedStrings.Str2142Params, info.Subscription.SubscriptionMessage);
						continue;
					}

					info.LookupItems.AddRange(items.Cast<object>());
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
						items = ArrayHelper.Empty<object>();
						return null;
					}

					if (info.Parent == null)
						items = info.LookupItems?.CopyAndClear() ?? ArrayHelper.Empty<object>();
					else
						items = ArrayHelper.Empty<object>();

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
						items = ArrayHelper.Empty<object>();
						return null;
					}

					if (info.Parent == null)
						items = info.LookupItems?.CopyAndClear() ?? ArrayHelper.Empty<object>();
					else
						items = ArrayHelper.Empty<object>();

					ChangeState(info, SubscriptionStates.Online);

					if (info.Parent != null)
						return null;

					return info.Subscription;
				}
			}

			public IEnumerable<Tuple<Subscription, Candle>> UpdateCandles(CandleMessage message)
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
								_connector.AddWarningLog(LocalizedStrings.Str704Params.Put(info.Subscription.SecurityId));
								continue;
							}

							info.Security = security;
						}
					}

					if (!info.UpdateLastTime(message.OpenTime))
						continue;
					
					if (!info.UpdateCandle(message, out var candle))
						continue;

					yield return Tuple.Create(info.Subscription, candle);
				}
			}

			public Subscription TryGetSubscription(Portfolio portfolio)
			{
				if (portfolio == null)
					throw new ArgumentNullException(nameof(portfolio));

				return Subscriptions.FirstOrDefault(s => s.Portfolio == portfolio);
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
				Subscribe(new Subscription(mdMsg, (SecurityMessage)null), true);
			}
		}
	}
}