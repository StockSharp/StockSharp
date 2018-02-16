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
			private readonly SynchronizedDictionary<long, Tuple<MarketDataMessage, Security>> _pendingSubscriptions = new SynchronizedDictionary<long, Tuple<MarketDataMessage, Security>>();
			private readonly SynchronizedDictionary<MarketDataTypes, CachedSynchronizedSet<Security>> _subscribers = new SynchronizedDictionary<MarketDataTypes, CachedSynchronizedSet<Security>>();
			private readonly Connector _connector;

			public SubscriptionManager(Connector connector)
			{
				if (connector == null)
					throw new ArgumentNullException(nameof(connector));

				_connector = connector;
			}

			public void ClearCache()
			{
				_subscribers.Clear();
				_registeredPortfolios.Clear();
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
				if (security == null)
					throw new ArgumentNullException(nameof(security));

				if (message == null)
					throw new ArgumentNullException(nameof(message));

				if (message.TransactionId == 0)
					message.TransactionId = _connector.TransactionIdGenerator.GetNextId();

				message.FillSecurityInfo(_connector, security);

				var value = Tuple.Create((MarketDataMessage)message.Clone(), security);

				if (tryAdd)
				{
					// if the message was looped back via IsBack=true
					_pendingSubscriptions.TryAdd(message.TransactionId, value);
				}
				else
					_pendingSubscriptions.Add(message.TransactionId, value);

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

			public Security ProcessResponse(long originalTransactionId, out MarketDataMessage message)
			{
				var tuple = _pendingSubscriptions.TryGetValue(originalTransactionId);

				if (tuple == null)
				{
					message = null;
					return null;
				}

				_pendingSubscriptions.Remove(originalTransactionId);

				var subscriber = tuple.Item2;
				message = tuple.Item1;

				lock (_subscribers.SyncRoot)
				{
					if (message.IsSubscribe)
						_subscribers.SafeAdd(message.DataType).Add(subscriber);
					else
					{
						var dict = _subscribers.TryGetValue(message.DataType);

						if (dict != null)
						{
							dict.Remove(subscriber);

							if (dict.Count == 0)
								_subscribers.Remove(message.DataType);
						}
					}
				}

				return subscriber;
			}
		}

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterSecurity"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredSecurities => _subscriptionManager.RegisteredSecurities;

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterMarketDepth"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredMarketDepths => _subscriptionManager.RegisteredMarketDepths;

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterTrades"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredTrades => _subscriptionManager.RegisteredTrades;

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterOrderLog"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredOrderLogs => _subscriptionManager.RegisteredOrderLogs;

		/// <summary>
		/// List of all portfolios, subscribed via <see cref="RegisterPortfolio"/>.
		/// </summary>
		public IEnumerable<Portfolio> RegisteredPortfolios => _subscriptionManager.RegisteredPortfolios;

		/// <summary>
		/// To sign up to get market data by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started.</param>
		/// <param name="message">The message that contain subscribe info.</param>
		public virtual void SubscribeMarketData(Security security, MarketDataMessage message)
		{
			var msg = LocalizedStrings.SubscriptionSent.Put(security.Id,
				message.DataType + (message.DataType.IsCandleDataType() ? " " + message.Arg : string.Empty));

			if (message.From != null && message.To != null)
				msg += LocalizedStrings.Str691Params.Put(message.From.Value, message.To.Value);

			this.AddDebugLog(msg + ".");

			_subscriptionManager.ProcessRequest(security, message, false);
		}

		/// <summary>
		/// To unsubscribe from getting market data by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started.</param>
		/// <param name="message">The message that contain unsubscribe info.</param>
		public virtual void UnSubscribeMarketData(Security security, MarketDataMessage message)
		{
			var msg = LocalizedStrings.UnSubscriptionSent.Put(security.Id,
				message.DataType + (message.DataType.IsCandleDataType() ? " " + message.Arg : string.Empty));

			if (message.From != null && message.To != null)
				msg += LocalizedStrings.Str691Params.Put(message.From.Value, message.To.Value);

			this.AddDebugLog(msg + ".");

			_subscriptionManager.ProcessRequest(security, message, false);
		}

		private void SubscribeMarketData(Security security, MarketDataTypes type)
		{
			SubscribeMarketData(security, new MarketDataMessage
			{
				DataType = type,
				IsSubscribe = true,
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

		/// <summary>
		/// To start getting new information (for example, <see cref="Security.LastTrade"/> or <see cref="Security.BestBid"/>) by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started.</param>
		public void RegisterSecurity(Security security)
		{
			SubscribeMarketData(security, MarketDataTypes.Level1);
		}

		/// <summary>
		/// To stop getting new information.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be stopped.</param>
		public void UnRegisterSecurity(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.Level1);
		}

		/// <summary>
		/// To start getting quotes (order book) by the instrument. Quotes values are available through the event <see cref="Connector.MarketDepthsChanged"/>.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be started.</param>
		public void RegisterMarketDepth(Security security)
		{
			SubscribeMarketData(security, MarketDataTypes.MarketDepth);
		}

		/// <summary>
		/// To stop getting quotes by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be stopped.</param>
		public void UnRegisterMarketDepth(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.MarketDepth);
		}

		/// <summary>
		/// To start getting filtered quotes (order book) by the instrument. Quotes values are available through the event <see cref="IConnector.GetFilteredMarketDepth"/>.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be started.</param>
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

		/// <summary>
		/// To stop getting filtered quotes by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be stopped.</param>
		public void UnRegisterFilteredMarketDepth(Security security)
		{
			UnSubscribeMarketData(security, FilteredMarketDepthAdapter.FilteredMarketDepth);
		}

		/// <summary>
		/// To start getting trades (tick data) by the instrument. New trades will come through the event <see cref="IConnector.NewTrades"/>.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be started.</param>
		public void RegisterTrades(Security security)
		{
			SubscribeMarketData(security, MarketDataTypes.Trades);
		}

		/// <summary>
		/// To stop getting trades (tick data) by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be stopped.</param>
		public void UnRegisterTrades(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.Trades);
		}

		/// <summary>
		/// Subscribe on the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for subscription.</param>
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

		/// <summary>
		/// Unsubscribe from the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for unsubscription.</param>
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

		/// <summary>
		/// Subscribe on order log for the security.
		/// </summary>
		/// <param name="security">Security for subscription.</param>
		public void RegisterOrderLog(Security security)
		{
			SubscribeMarketData(security, MarketDataTypes.OrderLog);
		}

		/// <summary>
		/// Unsubscribe from order log for the security.
		/// </summary>
		/// <param name="security">Security for unsubscription.</param>
		public void UnRegisterOrderLog(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.OrderLog);
		}

		/// <summary>
		/// Subscribe on news.
		/// </summary>
		public void RegisterNews()
		{
			OnRegisterNews();
		}

		/// <summary>
		/// Subscribe on news.
		/// </summary>
		protected virtual void OnRegisterNews()
		{
			SendInMessage(new MarketDataMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.News,
				IsSubscribe = true
			});
		}

		/// <summary>
		/// Unsubscribe from news.
		/// </summary>
		public void UnRegisterNews()
		{
			OnUnRegisterNews();
		}

		/// <summary>
		/// Request news <see cref="BusinessEntities.News.Story"/> body. After receiving the event <see cref="Connector.NewsChanged"/> will be triggered.
		/// </summary>
		/// <param name="news">News.</param>
		public virtual void RequestNewsStory(News news)
		{
			if (news == null)
				throw new ArgumentNullException(nameof(news));

			SendInMessage(new MarketDataMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.News,
				IsSubscribe = true,
				NewsId = news.Id.To<string>(),
			});
		}

		/// <summary>
		/// Unsubscribe from news.
		/// </summary>
		protected virtual void OnUnRegisterNews()
		{
			SendInMessage(new MarketDataMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.News,
				IsSubscribe = false
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
		public virtual void SubscribeCandles(CandleSeries series, DateTimeOffset? from = null, DateTimeOffset? to = null,
			long? count = null, long? transactionId = null, IDictionary<string, object> extensionInfo = null)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var mdMsg = series.ToMarketDataMessage(true, from, to, count);
			mdMsg.TransactionId = transactionId ?? TransactionIdGenerator.GetNextId();
			mdMsg.ExtensionInfo = extensionInfo;

			_entityCache.CreateCandleSeries(mdMsg, series);

			SubscribeMarketData(series.Security, mdMsg);
		}

		/// <summary>
		/// To stop the candles receiving subscription, previously created by <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public virtual void UnSubscribeCandles(CandleSeries series)
		{
			var mdMsg = _entityCache.TryGetCandleSeriesMarketDataMessage(series, TransactionIdGenerator.GetNextId);

			if (mdMsg == null)
				return;

			UnSubscribeMarketData(series.Security, mdMsg);
		}
	}
}