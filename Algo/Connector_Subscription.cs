#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: Connector_Subscription.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		private sealed class SubscriptionManager
		{
			private readonly SynchronizedDictionary<long, Tuple<MarketDataMessage, Security>> _requests = new SynchronizedDictionary<long, Tuple<MarketDataMessage, Security>>();
			private readonly SynchronizedDictionary<MarketDataTypes, CachedSynchronizedSet<Security>> _subscribers = new SynchronizedDictionary<MarketDataTypes, CachedSynchronizedSet<Security>>();
			private readonly Connector _connector;

			public SubscriptionManager(Connector connector)
			{
				_connector = connector ?? throw new ArgumentNullException(nameof(connector));
			}

			public void ClearCache()
			{
				_subscribers.Clear();
				_registeredPortfolios.Clear();
				_requests.Clear();
			}

			private IEnumerable<Security> GetSubscribers(MarketDataTypes type)
			{
				return _subscribers.TryGetValue(type)?.Cache ?? ArrayHelper.Empty<Security>();
			}

			public IEnumerable<Security> RegisteredSecurities => GetSubscribers(MarketDataTypes.Level1);

			public IEnumerable<Security> RegisteredMarketDepths => GetSubscribers(MarketDataTypes.MarketDepth);

			public IEnumerable<Security> RegisteredTrades => GetSubscribers(MarketDataTypes.Trades);

			public IEnumerable<Security> RegisteredOrderLogs => GetSubscribers(MarketDataTypes.OrderLog);

			private readonly CachedSynchronizedSet<Portfolio> _registeredPortfolios = new CachedSynchronizedSet<Portfolio>();

			public IEnumerable<Portfolio> RegisteredPortfolios => _registeredPortfolios.Cache;

			public void ProcessRequest(Security security, MarketDataMessage message, bool tryAdd)
			{
				if (message == null)
					throw new ArgumentNullException(nameof(message));

				if (!tryAdd)
				{
					var msg = (message.IsSubscribe ? LocalizedStrings.SubscriptionSent : LocalizedStrings.UnSubscriptionSent)
						.Put(security?.Id, message.ToDataTypeString());

					if (message.From != null && message.To != null)
						msg += LocalizedStrings.Str691Params.Put(message.From.Value, message.To.Value);

					_connector.AddDebugLog(msg + ".");
				}

				if (security == null)
				{
					if (!message.IsSubscribe)
					{
						if (message.OriginalTransactionId != 0)
							security = TryGetSecurity(message.OriginalTransactionId);
					}
				}

				if (security == null)
				{
					//if (message.DataType != MarketDataTypes.News)
					//{
						
					//}

					if (message.SecurityId != default)
					{
						security = _connector.LookupById(message.SecurityId);

						if (security == null)
							throw new ArgumentException(LocalizedStrings.Str704Params.Put(message.SecurityId));
					}
				}

				if (message.TransactionId == 0)
					message.TransactionId = _connector.TransactionIdGenerator.GetNextId();

				if (security != null)
					message.FillSecurityInfo(_connector, security);

				var value = Tuple.Create((MarketDataMessage)message.Clone(), security);

				if (tryAdd)
				{
					// if the message was looped back via IsBack=true
					_requests.TryAdd(message.TransactionId, value);
				}
				else
					_requests.Add(message.TransactionId, value);

				_connector.SendInMessage(message);
			}

			public void RegisterPortfolio(Portfolio portfolio)
			{
				if (portfolio == null)
					throw new ArgumentNullException(nameof(portfolio));

				if (portfolio is BasketPortfolio basketPortfolio)
					basketPortfolio.InnerPortfolios.ForEach(_connector.RegisterPortfolio);
				else
				{
					_registeredPortfolios.Add(portfolio);
					_connector.OnRegisterPortfolio(portfolio);
				}
			}

			public void UnRegisterPortfolio(Portfolio portfolio)
			{
				if (portfolio == null)
					throw new ArgumentNullException(nameof(portfolio));

				if (portfolio is BasketPortfolio basketPortfolio)
					basketPortfolio.InnerPortfolios.ForEach(_connector.UnRegisterPortfolio);
				else
				{
					_registeredPortfolios.Remove(portfolio);
					_connector.OnUnRegisterPortfolio(portfolio);
				}
			}

			public Security TryGetSecurity(long originalTransactionId)
			{
				return _requests.TryGetValue(originalTransactionId)?.Item2;
			}

			public Security ProcessResponse(MarketDataMessage response, out MarketDataMessage originalMsg, out bool unexpectedCancelled)
			{
				unexpectedCancelled = false;

				if (!_requests.TryGetValue(response.OriginalTransactionId, out var tuple))
				{
					originalMsg = null;
					return null;
				}

				//_requests.Remove(response.OriginalTransactionId);

				var subscriber = tuple.Item2;
				originalMsg = tuple.Item1;

				if (originalMsg.DataType.IsSecurityRequired())
				{
					lock (_subscribers.SyncRoot)
					{
						if (originalMsg.IsSubscribe)
						{
							if (response.IsOk())
								_subscribers.SafeAdd(originalMsg.DataType).Add(subscriber);
							else
							{
								var set = _subscribers.TryGetValue(originalMsg.DataType);

								if (set != null && set.Remove(subscriber))
								{
									unexpectedCancelled = true;
								}
							}
						}
						else
						{
							var dict = _subscribers.TryGetValue(originalMsg.DataType);

							if (dict != null)
							{
								dict.Remove(subscriber);

								if (dict.Count == 0)
									_subscribers.Remove(originalMsg.DataType);
							}
						}
					}
				}
				
				return subscriber;
			}
		}

		/// <inheritdoc />
		public IEnumerable<Security> RegisteredSecurities => _subscriptionManager.RegisteredSecurities;

		/// <inheritdoc />
		public IEnumerable<Security> RegisteredMarketDepths => _subscriptionManager.RegisteredMarketDepths;

		/// <inheritdoc />
		public IEnumerable<Security> RegisteredTrades => _subscriptionManager.RegisteredTrades;

		/// <inheritdoc />
		public IEnumerable<Security> RegisteredOrderLogs => _subscriptionManager.RegisteredOrderLogs;

		/// <inheritdoc />
		public IEnumerable<Portfolio> RegisteredPortfolios => _subscriptionManager.RegisteredPortfolios;

		/// <summary>
		/// List of all candles series, subscribed via <see cref="SubscribeCandles"/>.
		/// </summary>
		public IEnumerable<CandleSeries> SubscribedCandleSeries => _entityCache.AllCandleSeries;

		/// <inheritdoc />
		public virtual void SubscribeMarketData(MarketDataMessage message)
		{
			SubscribeMarketData(null, message);
		}

		/// <inheritdoc />
		public virtual void SubscribeMarketData(Security security, MarketDataMessage message)
		{
			_subscriptionManager.ProcessRequest(security, message, false);
		}

		/// <inheritdoc />
		public virtual void UnSubscribeMarketData(MarketDataMessage message)
		{
			UnSubscribeMarketData(null, message);
		}

		/// <inheritdoc />
		public virtual void UnSubscribeMarketData(Security security, MarketDataMessage message)
		{
			_subscriptionManager.ProcessRequest(security, message, false);
		}

		private void SubscribeMarketData(Security security, MarketDataTypes type, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, Level1Fields? buildField = null, int? maxDepth = null, IMessageAdapter adapter = null)
		{
			SubscribeMarketData(security, new MarketDataMessage
			{
				DataType = type,
				IsSubscribe = true,
				From = from,
				To = to,
				Count = count,
				BuildMode = buildMode,
				BuildFrom = buildFrom,
				BuildField = buildField,
				MaxDepth = maxDepth,
				Adapter = adapter
			});
		}

		private void UnSubscribeMarketData(Security security, MarketDataTypes type)
		{
			UnSubscribeMarketData(security, new MarketDataMessage
			{
				DataType = type,
				IsSubscribe = false,
			});
		}

		/// <inheritdoc />
		public void RegisterFilteredMarketDepth(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var quotes = GetMarketDepth(security).ToMessage();
			var executions = _entityCache
				.GetOrders(security, OrderStates.Active)
				.Select(o => o.ToMessage())
				.ToArray();

			SubscribeMarketData(security, new MarketDataMessage
			{
				DataType = FilteredMarketDepthAdapter.FilteredMarketDepth,
				IsSubscribe = true,
				Arg = Tuple.Create(quotes, executions)
			});
		}

		/// <inheritdoc />
		public void UnRegisterFilteredMarketDepth(Security security)
		{
			UnSubscribeMarketData(security, FilteredMarketDepthAdapter.FilteredMarketDepth);
		}

		/// <inheritdoc />
		public void SubscribeMarketDepth(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, int? maxDepth = null, IMessageAdapter adapter = null)
		{
			SubscribeMarketData(security, MarketDataTypes.MarketDepth, from, to, count, buildMode, buildFrom, null, maxDepth, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeMarketDepth(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.MarketDepth);
		}

		/// <inheritdoc />
		public void SubscribeTrades(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, IMessageAdapter adapter = null)
		{
			SubscribeMarketData(security, MarketDataTypes.Trades, from, to, count, buildMode, buildFrom, adapter: adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeTrades(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.Trades);
		}

		/// <inheritdoc />
		public void SubscribeLevel1(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, IMessageAdapter adapter = null)
		{
			SubscribeMarketData(security, MarketDataTypes.Level1, from, to, count, buildMode, buildFrom, adapter: adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeLevel1(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.Level1);
		}

		/// <inheritdoc />
		public void SubscribeOrderLog(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			SubscribeMarketData(security, MarketDataTypes.OrderLog, from, to, count, adapter: adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeOrderLog(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.OrderLog);
		}

		/// <inheritdoc />
		public void SubscribeNews(Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			SubscribeMarketData(security, MarketDataTypes.News, from, to, count, adapter: adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeNews(Security security = null)
		{
			UnSubscribeMarketData(security, MarketDataTypes.News);
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribeLevel1 method instead.")]
		public void RegisterSecurity(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, IMessageAdapter adapter = null)
		{
			SubscribeLevel1(security, from, to, count, buildMode, buildFrom, adapter);
		}

		/// <inheritdoc />
		[Obsolete("Use UnSubscribeLevel1 method instead.")]
		public void UnRegisterSecurity(Security security)
		{
			UnSubscribeLevel1(security);
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribeMarketDepth method instead.")]
		public void RegisterMarketDepth(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, int? maxDepth = null, IMessageAdapter adapter = null)
		{
			SubscribeMarketDepth(security, from, to, count, buildMode, buildFrom, maxDepth, adapter);
		}

		/// <inheritdoc />
		[Obsolete("Use UnSubscribeMarketDepth method instead.")]
		public void UnRegisterMarketDepth(Security security)
		{
			UnSubscribeMarketDepth(security);
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribeTrades method instead.")]
		public void RegisterTrades(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, IMessageAdapter adapter = null)
		{
			SubscribeTrades(security, from, to, count, buildMode, buildFrom, adapter);
		}

		/// <inheritdoc />
		[Obsolete("Use UnSubscribeTrades method instead.")]
		public void UnRegisterTrades(Security security)
		{
			UnSubscribeTrades(security);
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribeOrderLog method instead.")]
		public void RegisterOrderLog(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			SubscribeOrderLog(security, from, to, count, adapter);
		}

		/// <inheritdoc />
		[Obsolete("Use UnSubscribeOrderLog method instead.")]
		public void UnRegisterOrderLog(Security security)
		{
			UnSubscribeOrderLog(security);
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribeNews method instead.")]
		public void RegisterNews(Security security = null, IMessageAdapter adapter = null)
		{
			SubscribeNews(security, adapter: adapter);
		}

		/// <inheritdoc />
		[Obsolete("Use UnSubscribeNews method instead.")]
		public void UnRegisterNews(Security security = null)
		{
			UnSubscribeNews(security);
		}

		/// <inheritdoc />
		public void SubscribeBoard(ExchangeBoard board, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			SubscribeMarketData(null, MarketDataTypes.Board, from, to, count, adapter: adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeBoard(ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			UnSubscribeMarketData(null, MarketDataTypes.Board);
		}

		/// <inheritdoc />
		public void SubscribeOrders(Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			var lookupMsg = new OrderStatusMessage
			{
				IsSubscribe = true,
				TransactionId = TransactionIdGenerator.GetNextId(),
				SecurityId = security?.ToSecurityId() ?? default,
				From = from,
				To = to,
				Adapter = adapter,
			};
			_entityCache.AddOrderStatusTransactionId(lookupMsg.TransactionId);
			
			this.AddInfoLog("{0} '{1}' for '{2}'.", nameof(SubscribeOrders), lookupMsg, lookupMsg.Adapter);
			SendInMessage(lookupMsg);
		}

		/// <inheritdoc />
		public void UnSubscribeOrders()
		{
			var lookupMsg = new OrderStatusMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				IsSubscribe = false,
			};

			this.AddInfoLog(nameof(UnSubscribeOrders));
			SendInMessage(lookupMsg);
		}

		/// <inheritdoc />
		public void RegisterPortfolio(Portfolio portfolio)
		{
			_subscriptionManager.RegisterPortfolio(portfolio);
		}

		/// <summary>
		/// Subscribe on the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for subscription.</param>
		protected virtual void OnRegisterPortfolio(Portfolio portfolio)
		{
			SendInMessage(new PortfolioMessage
			{
				PortfolioName = portfolio.Name,
				TransactionId = TransactionIdGenerator.GetNextId(),
				IsSubscribe = true
			});
		}

		/// <inheritdoc />
		public void UnRegisterPortfolio(Portfolio portfolio)
		{
			_subscriptionManager.UnRegisterPortfolio(portfolio);
		}

		/// <summary>
		/// Unsubscribe from the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for unsubscription.</param>
		protected virtual void OnUnRegisterPortfolio(Portfolio portfolio)
		{
			SendInMessage(new PortfolioMessage
			{
				PortfolioName = portfolio.Name,
				TransactionId = TransactionIdGenerator.GetNextId(),
				IsSubscribe = false
			});
		}

		/// <inheritdoc />
		public IEnumerable<Subscription> Subscriptions => _subscriptions.CachedValues;

		/// <inheritdoc />
		public void Subscribe(Subscription subscription)
		{
			if (subscription == null)
				throw new ArgumentNullException(nameof(subscription));

			if (subscription.TransactionId == 0)
				subscription.TransactionId = TransactionIdGenerator.GetNextId();

			_subscriptions.Add(subscription.TransactionId, subscription);

			if (subscription.DataType.IsMarketData)
			{
				if (subscription.DataType.IsCandles)
					SubscribeCandles(subscription.CandleSeries, transactionId: subscription.TransactionId);
				else
					SubscribeMarketData(subscription.Security, subscription.MarketDataMessage);
			}
			else if (subscription.DataType == DataType.Transactions)
			{
				SubscribeOrders();
			}
			else if (subscription.DataType == DataType.PositionChanges)
			{
				SubscribePositions();
			}
			else
				throw new ArgumentOutOfRangeException(nameof(subscription), subscription.DataType, LocalizedStrings.Str1219);
		}

		/// <inheritdoc />
		public void UnSubscribe(Subscription subscription)
		{
			if (subscription == null)
				throw new ArgumentNullException(nameof(subscription));

			var transId = subscription.TransactionId;

			if (subscription.DataType.IsMarketData)
			{
				if (subscription.DataType.IsCandles)
					UnSubscribeCandles(subscription.CandleSeries);
				else
					UnSubscribeMarketData(subscription.Security, new MarketDataMessage { OriginalTransactionId = transId });
			}
			else if (subscription.DataType == DataType.Transactions)
			{
				UnSubscribeOrders();
			}
			else if (subscription.DataType == DataType.PositionChanges)
			{
				UnSubscribePositions();
			}
			else
				throw new ArgumentOutOfRangeException(nameof(subscription), subscription.DataType, LocalizedStrings.Str1219);
		}

		/// <inheritdoc />
		public virtual void RequestNewsStory(News news, IMessageAdapter adapter = null)
		{
			if (news == null)
				throw new ArgumentNullException(nameof(news));

			SubscribeMarketData(null, new MarketDataMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.News,
				IsSubscribe = true,
				NewsId = news.Id.To<string>(),
				Adapter = adapter,
			});
		}

		/// <summary>
		/// Subscribe to receive new candles.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Candles count.</param>
		/// <param name="transactionId">Transaction ID.</param>
		/// <param name="extensionInfo">Extended information.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		public virtual void SubscribeCandles(CandleSeries series, DateTimeOffset? from = null, DateTimeOffset? to = null,
			long? count = null, long? transactionId = null, IDictionary<string, object> extensionInfo = null, IMessageAdapter adapter = null)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var mdMsg = series.ToMarketDataMessage(true, from, to, count);
			mdMsg.TransactionId = transactionId ?? TransactionIdGenerator.GetNextId();
			mdMsg.ExtensionInfo = extensionInfo;
			mdMsg.Adapter = adapter;

			_entityCache.CreateCandleSeries(mdMsg, series);

			SubscribeMarketData(series.Security, mdMsg);
		}

		/// <summary>
		/// To stop the candles receiving subscription, previously created by <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public virtual void UnSubscribeCandles(CandleSeries series)
		{
			var originalTransId = _entityCache.TryGetTransactionId(series);

			if (originalTransId == 0)
				return;

			var mdMsg = series.ToMarketDataMessage(false);
			mdMsg.TransactionId = TransactionIdGenerator.GetNextId();
			mdMsg.OriginalTransactionId = originalTransId;
			UnSubscribeMarketData(series.Security, mdMsg);
		}

		/// <inheritdoc />
		public void SubscribePositions(Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			var msg = new PortfolioLookupMessage
			{
				IsSubscribe = true,
				TransactionId = TransactionIdGenerator.GetNextId(),
				Adapter = adapter,
			};
			
			_portfolioLookups.Add(msg.TransactionId, new LookupInfo<PortfolioLookupMessage, Portfolio>(msg));

			this.AddInfoLog(nameof(SubscribePositions));
			SendInMessage(msg);
		}

		/// <inheritdoc />
		public void UnSubscribePositions()
		{
			var msg = new PortfolioLookupMessage
			{
				IsSubscribe = false,
				TransactionId = TransactionIdGenerator.GetNextId(),
			};

			this.AddInfoLog(nameof(UnSubscribePositions));
			SendInMessage(msg);
		}
	}
}