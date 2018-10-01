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
				_connector = connector ?? throw new ArgumentNullException(nameof(connector));
			}

			public void ClearCache()
			{
				_subscribers.Clear();
				_registeredPortfolios.Clear();
				_pendingSubscriptions.Clear();
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
				{
					if (message.DataType != MarketDataTypes.News)
						throw new ArgumentNullException(nameof(security));
				}

				if (message == null)
					throw new ArgumentNullException(nameof(message));

				if (message.TransactionId == 0)
					message.TransactionId = _connector.TransactionIdGenerator.GetNextId();

				if (security != null)
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

			public Security TryGetSecurity(long originalTransactionId)
			{
				return _pendingSubscriptions.TryGetValue(originalTransactionId)?.Item2;
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

				if (message.DataType != MarketDataTypes.News)
				{
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
		public virtual void SubscribeMarketData(Security security, MarketDataMessage message)
		{
			var msg = LocalizedStrings.SubscriptionSent.Put(security?.Id,
				message.DataType + (message.DataType.IsCandleDataType() ? " " + message.Arg : string.Empty));

			if (message.From != null && message.To != null)
				msg += LocalizedStrings.Str691Params.Put(message.From.Value, message.To.Value);

			this.AddDebugLog(msg + ".");

			_subscriptionManager.ProcessRequest(security, message, false);
		}

		/// <inheritdoc />
		public virtual void UnSubscribeMarketData(Security security, MarketDataMessage message)
		{
			var msg = LocalizedStrings.UnSubscriptionSent.Put(security?.Id,
				message.DataType + (message.DataType.IsCandleDataType() ? " " + message.Arg : string.Empty));

			if (message.From != null && message.To != null)
				msg += LocalizedStrings.Str691Params.Put(message.From.Value, message.To.Value);

			this.AddDebugLog(msg + ".");

			_subscriptionManager.ProcessRequest(security, message, false);
		}

		private void SubscribeMarketData(Security security, MarketDataTypes type, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, Level1Fields? buildField = null, int? maxDepth = null)
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
		public void RegisterSecurity(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null)
		{
			SubscribeMarketData(security, MarketDataTypes.Level1, from, to, count, buildMode, buildFrom);
		}

		/// <inheritdoc />
		public void UnRegisterSecurity(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.Level1);
		}

		/// <inheritdoc />
		public void RegisterMarketDepth(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, int? maxDepth = null)
		{
			SubscribeMarketData(security, MarketDataTypes.MarketDepth, from, to, count, buildMode, buildFrom, null, maxDepth);
		}

		/// <inheritdoc />
		public void UnRegisterMarketDepth(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.MarketDepth);
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
		public void RegisterTrades(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null)
		{
			SubscribeMarketData(security, MarketDataTypes.Trades, from, to, count, buildMode, buildFrom);
		}

		/// <inheritdoc />
		public void UnRegisterTrades(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.Trades);
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

		/// <inheritdoc />
		public void RegisterOrderLog(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null)
		{
			SubscribeMarketData(security, MarketDataTypes.OrderLog, from, to, count);
		}

		/// <inheritdoc />
		public void UnRegisterOrderLog(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.OrderLog);
		}

		/// <inheritdoc />
		public void RegisterNews()
		{
			OnRegisterNews();
		}

		/// <summary>
		/// Subscribe on news.
		/// </summary>
		protected virtual void OnRegisterNews()
		{
			SubscribeMarketData(null, MarketDataTypes.News);
		}

		/// <inheritdoc />
		public void UnRegisterNews()
		{
			OnUnRegisterNews();
		}

		/// <inheritdoc />
		public void SubscribeBoard(ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			SendInMessage(new BoardRequestMessage
			{
				IsSubscribe = true,
				BoardCode = board.Code,
				TransactionId = TransactionIdGenerator.GetNextId(),
			});
		}

		/// <inheritdoc />
		public void UnSubscribeBoard(ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			SendInMessage(new BoardRequestMessage
			{
				IsSubscribe = false,
				BoardCode = board.Code,
				TransactionId = TransactionIdGenerator.GetNextId(),
			});
		}

		/// <inheritdoc />
		public virtual void RequestNewsStory(News news)
		{
			if (news == null)
				throw new ArgumentNullException(nameof(news));

			SubscribeMarketData(null, new MarketDataMessage
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
			UnSubscribeMarketData(null, MarketDataTypes.News);
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
			var originalTransId = _entityCache.TryGetTransactionId(series);

			if (originalTransId == 0)
				return;

			var mdMsg = series.ToMarketDataMessage(false);
			mdMsg.TransactionId = TransactionIdGenerator.GetNextId();
			mdMsg.OriginalTransactionId = originalTransId;
			UnSubscribeMarketData(series.Security, mdMsg);
		}
	}
}