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

		private sealed class SubscriptionManager
		{
			private class SubscriptionInfo
			{
				private DateTimeOffset? _last;

				public SubscriptionInfo(Subscription subscription)
				{
					Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));

					if (Subscription.CandleSeries != null)
						Holder = new CandlesSeriesHolder(subscription.CandleSeries);

					// TODO
					//_last = subscription.SubscriptionMessage.From;

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
				public LookupInfo Lookup { get; }
				public bool Active { get; set; }
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
					if (_last == null || _last.Value < time)
					{
						_last = time;
						return true;
					}

					return false;
				}
			}

			private readonly SyncObject _syncObject = new SyncObject();

			private readonly Dictionary<long, SubscriptionInfo> _subscriptions = new Dictionary<long, SubscriptionInfo>();
			private readonly Dictionary<long, Tuple<ISubscriptionMessage, Subscription>> _requests = new Dictionary<long, Tuple<ISubscriptionMessage, Subscription>>();

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

			private SubscriptionInfo TryGetInfo(long id, bool remove, DateTimeOffset? time = null)
			{
				lock (_syncObject)
				{
					if (_subscriptions.TryGetValue(id, out var info))
					{
						if (remove)
							_subscriptions.Remove(id);
						else if (time != null)
							info.UpdateLastTime(time.Value);

						return info;
					}
				}

				_connector.AddWarningLog(LocalizedStrings.SubscriptionNonExist, id);
				return null;
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

			public Subscription ProcessResponse(MarketDataMessage response, out MarketDataMessage originalMsg, out bool unexpectedCancelled)
			{
				var addLog = false;

				try
				{
					lock (_syncObject)
					{
						unexpectedCancelled = false;

						if (!_requests.TryGetValue(response.OriginalTransactionId, out var tuple))
						{
							addLog = true;
							originalMsg = null;
							return null;
						}

						_requests.Remove(response.OriginalTransactionId);

						originalMsg = (MarketDataMessage)tuple.Item1;

						var info = originalMsg.IsSubscribe
							? TryGetInfo(originalMsg.TransactionId, false)
							: TryGetInfo(originalMsg.OriginalTransactionId, true);

						if (originalMsg.IsSubscribe)
						{
							if (response.IsOk())
							{
								info.Active = true;
							}
							else
							{
								_subscriptions.Remove(info.Subscription.TransactionId);
								unexpectedCancelled = info.Active;
							}
						}
						else
						{
							_subscriptions.Remove(info.Subscription.TransactionId);
						}

						return info.Subscription;
					}
				}
				finally
				{
					if (addLog)
						_connector.AddWarningLog(LocalizedStrings.SubscriptionNonExist, response.OriginalTransactionId);
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

					if (subscription.DataType == DataType.Transactions)
						_connector._entityCache.AddOrderStatusTransactionId(subscription.TransactionId);

					_subscriptions.Add(subscription.TransactionId, info);
				}

				SendRequest((ISubscriptionMessage)subscrMsg.Clone(), subscription);
			}

			public void UnSubscribe(Subscription subscription)
			{
				if (subscription == null)
					throw new ArgumentNullException(nameof(subscription));

				var unsubscribe = subscription.DataType.ToSubscriptionMessage();

				unsubscribe.TransactionId = _connector.TransactionIdGenerator.GetNextId();
				unsubscribe.OriginalTransactionId = subscription.TransactionId;
				unsubscribe.IsSubscribe = false;

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
				var requests = new Dictionary<ISubscriptionMessage, SubscriptionInfo>();

				lock (_syncObject)
				{
					_requests.Clear();

					foreach (var pair in _subscriptions)
					{
						var info = pair.Value;
						var newId = _connector.TransactionIdGenerator.GetNextId();

						if (info.Subscription.DataType == DataType.Transactions)
						{
							_connector._entityCache.RemoveOrderStatusTransactionId(info.Subscription.TransactionId);
							_connector._entityCache.AddOrderStatusTransactionId(newId);
						}

						info.Subscription.TransactionId = newId;
						requests.Add(info.CreateSubscriptionContinue(), info);
					}

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
				foreach (var subscription in Subscriptions)
				{
					UnSubscribe(subscription);
				}
			}

			public void ProcessLookupResponse(ISubscriptionIdMessage message, object item)
			{
				foreach (var id in message.GetSubscriptionIds())
				{
					var info = TryGetInfo(id, false);

					if (info == null)
						continue;

					if (info.Lookup == null)
					{
						_connector.AddWarningLog(LocalizedStrings.Str2142Params, info.Subscription.SubscriptionMessage);
						continue;
					}

					info.Lookup.Items.Add(item);	
				}
			}

			public LookupInfo TryGetAndRemoveLookup(IOriginalTransactionIdMessage result)
			{
				lock (_syncObject)
					return _subscriptions.TryGetAndRemove(result.OriginalTransactionId)?.Lookup;
			}

			public Subscription ProcessMarketDataFinishedMessage(MarketDataFinishedMessage message)
			{
				return TryGetSubscription(message.OriginalTransactionId, true);
			}

			public IEnumerable<Tuple<Subscription, Candle>> UpdateCandles(CandleMessage message)
			{
				foreach (var subscriptionId in message.GetSubscriptionIds())
				{
					SubscriptionInfo info;

					lock (_syncObject)
					{
						if (!_subscriptions.TryGetValue(subscriptionId, out info))
							continue;
					}

					if (info.Holder == null)
						continue;
					
					if (!info.Holder.UpdateCandle(message, out var candle))
						continue;
				
					yield return Tuple.Create(info.Subscription, candle);
				}
			}
		}
	}
}