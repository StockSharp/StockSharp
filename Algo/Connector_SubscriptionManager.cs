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
		private class SubscriptionManager
		{
			private class LookupInfo
			{
				public ISubscriptionMessage Criteria { get; }
				public IList<object> Items { get; } = new List<object>();

				public LookupInfo(ISubscriptionMessage criteria)
				{
					if (criteria == null)
						throw new ArgumentNullException(nameof(criteria));

					Criteria = (ISubscriptionMessage)criteria.Clone();
				}
			}

			private class SubscriptionInfo
			{
				private DateTimeOffset? _last;

				public SubscriptionInfo(Subscription subscription)
				{
					Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));

					if (Subscription.CandleSeries != null)
						Holder = new CandlesSeriesHolder(subscription.CandleSeries);

					_last = subscription.SubscriptionMessage.From;

					var type = subscription.DataType;

					if (type == DataType.PositionChanges ||
					    type == DataType.Securities ||
					    type == DataType.Board ||
					    type == DataType.TimeFrames)
					{
						Lookup = new LookupInfo(subscription.SubscriptionMessage);
					}
				}

				public Subscription Subscription { get; }
				public bool HasResult { get; set; }
				public LookupInfo Lookup { get; }
				public CandlesSeriesHolder Holder { get; }

				public ISubscriptionMessage CreateSubscriptionContinue()
				{
					var subscrMsg = (ISubscriptionMessage)Subscription.SubscriptionMessage.Clone();

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

				public override string ToString() => Subscription.ToString();
			}

			private readonly SyncObject _syncObject = new SyncObject();

			private readonly Dictionary<long, SubscriptionInfo> _subscriptions = new Dictionary<long, SubscriptionInfo>();
			private readonly Dictionary<long, Tuple<ISubscriptionMessage, Subscription>> _requests = new Dictionary<long, Tuple<ISubscriptionMessage, Subscription>>();
			private readonly List<SubscriptionInfo> _keeped = new List<SubscriptionInfo>();
			private readonly HashSet<long> _notFound = new HashSet<long>();

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
				}
			}

			public IEnumerable<Security> GetSubscribers(DataType dataType)
			{
				return Subscriptions
				       .Where(s => s.DataType == dataType && s.Security != null)
				       .Select(s => s.Security);
			}

			public IEnumerable<Portfolio> SubscribedPortfolios => Subscriptions.Select(s => s.Portfolio).Where(p => p != null);

			public IEnumerable<CandleSeries> SubscribedCandleSeries => Subscriptions.Select(s => s.CandleSeries).Where(p => p != null);

			private void TryWriteLog(long id)
			{
				if (_notFound.Add(id))
					_connector.AddWarningLog(LocalizedStrings.SubscriptionNonExist, id);
			}

			private SubscriptionInfo TryGetInfo(long id, bool remove, DateTimeOffset? time = null, bool addLog = true)
			{
				lock (_syncObject)
				{
					if (_subscriptions.TryGetValue(id, out var info))
					{
						if (remove)
							_subscriptions.Remove(id);
						else if (time != null)
						{
							if (!info.UpdateLastTime(time.Value))
								return null;
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

				foreach (var id in message.GetSubscriptionIds())
				{
					var subscription = TryGetSubscription(id, false, time);

					if (subscription != null)
						yield return subscription;
				}
			}

			public Subscription TryGetSubscription(long id, bool remove, DateTimeOffset? time = null)
			{
				return TryGetInfo(id, remove, time)?.Subscription;
			}

			public Subscription TryFindSubscription(long id, DataType dataType, Security security = null)
			{
				var subscription = id > 0
					? TryGetSubscription(id, false)
					: Subscriptions.FirstOrDefault(s => s.DataType == dataType && s.Security == security);

				if (subscription == null && id == 0)
					_connector.AddWarningLog(LocalizedStrings.SubscriptionNonExist, Tuple.Create(dataType, security));

				return subscription;
			}

			public Subscription TryFindSubscription(CandleSeries series)
			{
				if (series == null)
					throw new ArgumentNullException(nameof(series));

				var subscription = Subscriptions.FirstOrDefault(s => s.CandleSeries == series);

				if (subscription == null)
					_connector.AddWarningLog(LocalizedStrings.SubscriptionNonExist, series);

				return subscription;
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

			private void ChangeState(Subscription subscription, SubscriptionStates state)
			{
				const string text = "Subscription {0} {1}->{2}.";

				if (subscription.State.IsOk(state))
					_connector.AddInfoLog(text, subscription.TransactionId, subscription.State, state);
				else
					_connector.AddWarningLog(text, subscription.TransactionId, subscription.State, state);

				subscription.State = state;
			}

			public Subscription ProcessResponse(MarketDataMessage response, out MarketDataMessage originalMsg, out bool unexpectedCancelled)
			{
				originalMsg = null;

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

						originalMsg = (MarketDataMessage)tuple.Item1;

						var info = originalMsg.IsSubscribe
							? TryGetInfo(originalMsg.TransactionId, false, addLog: false)
							: TryGetInfo(originalMsg.OriginalTransactionId, true, addLog: false);

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
								ChangeState(subscription, SubscriptionStates.Active);
							}
							else
							{
								ChangeState(subscription, SubscriptionStates.Error);

								_subscriptions.Remove(subscription.TransactionId);

								unexpectedCancelled = subscription.State.IsActive();

								_requests.Remove(response.OriginalTransactionId);
							}
						}
						else
						{
							ChangeState(subscription, SubscriptionStates.Stopped);

							_subscriptions.Remove(subscription.TransactionId);

							// remove subscribe and unsubscribe requests
							_requests.Remove(subscription.TransactionId);
							_requests.Remove(response.OriginalTransactionId);
						}

						return subscription;
					}
				}
				finally
				{
					if (originalMsg == null)
						TryWriteLog(response.OriginalTransactionId);
				}
			}

			public void Subscribe(Subscription subscription)
			{
				if (subscription == null)
					throw new ArgumentNullException(nameof(subscription));

				if (subscription.TransactionId == 0)
					subscription.TransactionId = _connector.TransactionIdGenerator.GetNextId();

				var subscrMsg = subscription.SubscriptionMessage;

				lock (_syncObject)
				{
					var info = new SubscriptionInfo(subscription);

					if (subscrMsg is OrderStatusMessage)
						_connector._entityCache.AddOrderStatusTransactionId(subscription.TransactionId);

					_subscriptions.Add(subscription.TransactionId, info);
				}

				SendRequest((ISubscriptionMessage)subscrMsg.Clone(), subscription);
			}

			public void UnSubscribe(Subscription subscription)
			{
				if (subscription == null)
					throw new ArgumentNullException(nameof(subscription));

				if (!subscription.State.IsActive())
				{
					_connector.AddWarningLog("Subscription {0} has state {1}. Cannot unsubscribe.", subscription.TransactionId, subscription.State);
					return;
				}

				var unsubscribe = subscription.DataType.ToSubscriptionMessage();

				unsubscribe.TransactionId = _connector.TransactionIdGenerator.GetNextId();
				unsubscribe.OriginalTransactionId = subscription.TransactionId;
				unsubscribe.IsSubscribe = false;

				// some subscription can be only for subscribe
				if (unsubscribe.IsSubscribe)
					return;

				SendRequest(unsubscribe, subscription);
			}

			private void SendRequest(ISubscriptionMessage request, Subscription subscription)
			{
				lock (_syncObject)
					_requests.Add(request.TransactionId, Tuple.Create(request, subscription));

				_connector.AddInfoLog(request.IsSubscribe ? LocalizedStrings.SubscriptionSent : LocalizedStrings.UnSubscriptionSent, subscription.Security?.Id, subscription);
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
					SendRequest(pair.Key, pair.Value.Subscription);
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

			public void ProcessLookupResponse(ISubscriptionIdMessage message, object item)
			{
				foreach (var id in message.GetSubscriptionIds())
				{
					var info = TryGetInfo(id, false);

					if (info == null || info.HasResult)
						continue;

					if (info.Lookup == null)
					{
						_connector.AddWarningLog(LocalizedStrings.Str2142Params, info.Subscription.SubscriptionMessage);
						continue;
					}

					info.Lookup.Items.Add(item);	
				}
			}

			public bool TryGetAndRemoveLookup<TMessage, TItem>(IOriginalTransactionIdMessage result, out TMessage criteria, out TItem[] items)
				where TMessage : Message, ISubscriptionMessage
			{
				criteria = null;
				items = null;

				lock (_syncObject)
				{
					var info = _subscriptions.TryGetValue(result.OriginalTransactionId);

					if (info == null)
						return false;

					info.HasResult = true;

					var lookup = info.Lookup;
					if (lookup == null)
						return false;

					criteria = (TMessage)lookup.Criteria;
					items = lookup.Items.CopyAndClear().Cast<TItem>().ToArray();
					return true;
				}
			}

			public Subscription ProcessSubscriptionFinishedMessage(SubscriptionFinishedMessage message)
			{
				var subscription = TryGetSubscription(message.OriginalTransactionId, true);

				if (subscription != null)
				{
					ChangeState(subscription, SubscriptionStates.Finished);
					_requests.Remove(message.OriginalTransactionId);
				}

				return subscription;
			}

			public Subscription ProcessSubscriptionOnlineMessage(SubscriptionOnlineMessage message)
			{
				var subscription = TryGetSubscription(message.OriginalTransactionId, false);

				if (subscription != null)
					ChangeState(subscription, SubscriptionStates.Online);

				return subscription;
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
					}

					if (info.Holder == null)
						continue;

					if (!info.UpdateLastTime(message.OpenTime))
						continue;
					
					if (!info.Holder.UpdateCandle(message, out var candle))
						continue;

					yield return Tuple.Create(info.Subscription, candle);
				}
			}

			public Subscription TryFindFilteredMarketDepth(Security security)
			{
				var subscription = Subscriptions.FirstOrDefault(s => s.SubscriptionMessage is FilteredMarketDepthMessage && s.Security == security);

				if (subscription == null)
					_connector.AddWarningLog(LocalizedStrings.SubscriptionNonExist, $"Filtered({security?.Id})");

				return subscription;
			}

			public Subscription TryGetSubscription(Portfolio portfolio)
			{
				if (portfolio == null)
					throw new ArgumentNullException(nameof(portfolio));

				return Subscriptions.FirstOrDefault(s => s.Portfolio == portfolio);
			}

			public Subscription ProcessResponse(PortfolioMessage response)
			{
				var originId = response.OriginalTransactionId;

				Subscription subscription;

				lock (_syncObject)
				{
					if (response.Error != null)
					{
						subscription = TryGetSubscription(originId, true);
				
						if (subscription != null)
							ChangeState(subscription, SubscriptionStates.Error);
					}
					else
					{
						if (originId == 0)
							return null;

						subscription = TryGetSubscription(originId, false);

						if (subscription?.Portfolio != null && subscription.State != SubscriptionStates.Active)
							ChangeState(subscription, SubscriptionStates.Active);
					}
				}

				return subscription;
			}
		}
	}
}