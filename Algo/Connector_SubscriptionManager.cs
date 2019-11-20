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
				public Subscription Subscription { get; }

				public SubscriptionInfo(Subscription subscription)
				{
					Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));

					if (Subscription.CandleSeries != null)
						Holder = new CandlesSeriesHolder(subscription.CandleSeries);
				}

				public LookupInfo Lookup { get; set; }
				public bool Active { get; set; }
				public CandlesSeriesHolder Holder { get; }
			}

			private readonly SyncObject _syncObject = new SyncObject();

			private readonly Dictionary<long, SubscriptionInfo> _subscriptions = new Dictionary<long, SubscriptionInfo>();
			//private readonly Dictionary<long, LookupInfo> _lookups = new Dictionary<long, LookupInfo>();
			private readonly Dictionary<long, Tuple<ISubscriptionMessage, Subscription>> _requests = new Dictionary<long, Tuple<ISubscriptionMessage, Subscription>>();
			//private readonly Dictionary<Subscription, CandlesSeriesHolder> _candlesHolder = new Dictionary<Subscription, CandlesSeriesHolder>();
			//private readonly HashSet<Subscription> _activeSubscriptions = new HashSet<Subscription>();

			private readonly Connector _connector;

			public SubscriptionManager(Connector connector)
			{
				_connector = connector ?? throw new ArgumentNullException(nameof(connector));
			}

			private EntityCache EntityCache => _connector._entityCache;

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
					//_lookups.Clear();
					_requests.Clear();
					//_candlesHolder.Clear();
					//_activeSubscriptions.Clear();
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

			private SubscriptionInfo TryGetInfo(long id, bool remove)
			{
				lock (_syncObject)
				{
					if (_subscriptions.TryGetValue(id, out var info))
					{
						if (remove)
						{
							_subscriptions.Remove(id);
							//_candlesHolder.Remove(subscription);
						}

						return info;
					}
				}

				_connector.AddWarningLog(LocalizedStrings.SubscriptionNonExist, id);
				return null;
			}

			public Subscription TryGetSubscription(long id, bool remove = false)
			{
				return TryGetInfo(id, remove)?.Subscription;
			}

			public Subscription TryFindSubscription(long id, DataType dataType, Security security = null)
			{
				var subscription = id > 0
					? TryGetSubscription(id)
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
				unexpectedCancelled = false;

				if (!_requests.TryGetValue(response.OriginalTransactionId, out var tuple))
				{
					originalMsg = null;
					return null;
				}

				//_requests.Remove(response.OriginalTransactionId);

				originalMsg = (MarketDataMessage)tuple.Item1;

				var info = originalMsg.IsSubscribe
					? TryGetInfo(originalMsg.TransactionId, false)
					: TryGetInfo(originalMsg.OriginalTransactionId, true);

				lock (_syncObject)
				{
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
				}
				
				return info.Subscription;
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

					if (subscription.DataType.IsMarketData)
					{
					}
					else if (subscription.DataType == DataType.Transactions)
					{
						EntityCache.AddOrderStatusTransactionId(subscription.TransactionId);
					}
					else if (subscription.DataType == DataType.PositionChanges)
					{
						info.Lookup = new LookupInfo(subscrMsg);
					}
					else if (subscription.DataType == DataType.Securities)
					{
						info.Lookup = new LookupInfo(subscrMsg);
					}
					else if (subscription.DataType == DataType.Board)
					{
						info.Lookup = new LookupInfo(subscrMsg);
					}
					else if (subscrMsg is TimeFrameLookupMessage)
					{
						info.Lookup = new LookupInfo(subscrMsg);
					}
					else if (subscription.DataType.IsPortfolio)
					{
					
					}
					else
						throw new ArgumentOutOfRangeException(nameof(subscription), subscription.DataType, LocalizedStrings.Str1219);

					_subscriptions.Add(subscription.TransactionId, info);
				}

				subscrMsg = (ISubscriptionMessage)subscrMsg.Clone();
				_requests.Add(subscrMsg.TransactionId, Tuple.Create(subscrMsg, subscription));

				_connector.AddInfoLog(subscrMsg.IsSubscribe ? LocalizedStrings.SubscriptionSent : LocalizedStrings.UnSubscriptionSent, subscription.Security?.Id, subscription);
				_connector.SendInMessage((Message)subscrMsg);
			}

			public void UnSubscribe(Subscription subscription)
			{
				if (subscription == null)
					throw new ArgumentNullException(nameof(subscription));

				ISubscriptionMessage unsubscribe;

				if (subscription.DataType.IsMarketData)
				{
					unsubscribe = new MarketDataMessage();
				}
				else if (subscription.DataType == DataType.Transactions)
				{
					unsubscribe = new OrderStatusMessage();
				}
				else if (subscription.DataType == DataType.PositionChanges)
				{
					unsubscribe = new PortfolioLookupMessage();
				}
				else if (subscription.DataType.IsPortfolio)
				{
					unsubscribe = new PortfolioMessage();
				}
				else
					throw new ArgumentOutOfRangeException(nameof(subscription), subscription.DataType, LocalizedStrings.Str1219);

				//// "immediate" unsubscribe
				//if (unsubscribe == null)
				//{
				//	lock (_syncObject)
				//	{
				//		_subscriptions.Remove(subscription.TransactionId);
				//	}

				//	return;
				//}

				unsubscribe.TransactionId = _connector.TransactionIdGenerator.GetNextId();
				unsubscribe.OriginalTransactionId = subscription.TransactionId;
				unsubscribe.IsSubscribe = false;

				_requests.Add(unsubscribe.TransactionId, Tuple.Create(unsubscribe, subscription));
				_connector.SendInMessage((Message)unsubscribe);
			}

			public void ProcessLookupResponse<TCriteria>(IOriginalTransactionIdMessage message, object item)
				where TCriteria : Message, ISubscriptionMessage, new()
			{
				lock (_syncObject)
				{
					_subscriptions.SafeAdd(message.OriginalTransactionId, key => new SubscriptionInfo(new Subscription(new TCriteria()))
					{
						Lookup = new LookupInfo(new TCriteria())
					}).Lookup.Items.Add(item);
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