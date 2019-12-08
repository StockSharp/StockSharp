namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;

	partial class Connector
	{
		/// <inheritdoc />
		public IEnumerable<Security> RegisteredSecurities => _subscriptionManager.GetSubscribers(DataType.Level1);

		/// <inheritdoc />
		public IEnumerable<Security> RegisteredMarketDepths => _subscriptionManager.GetSubscribers(DataType.MarketDepth);

		/// <inheritdoc />
		public IEnumerable<Security> RegisteredTrades => _subscriptionManager.GetSubscribers(DataType.Ticks);

		/// <inheritdoc />
		public IEnumerable<Security> RegisteredOrderLogs => _subscriptionManager.GetSubscribers(DataType.OrderLog);

		/// <inheritdoc />
		public IEnumerable<Portfolio> RegisteredPortfolios => _subscriptionManager.SubscribedPortfolios;

		/// <summary>
		/// List of all candles series, subscribed via <see cref="SubscribeCandles"/>.
		/// </summary>
		public IEnumerable<CandleSeries> SubscribedCandleSeries => _subscriptionManager.SubscribedCandleSeries;

		/// <inheritdoc />
		public long SubscribeMarketData(MarketDataMessage message)
		{
			return SubscribeMarketData(null, message);
		}

		/// <inheritdoc />
		public long SubscribeMarketData(Security security, MarketDataMessage message)
		{
			var subscription = new Subscription(message, security);
			Subscribe(subscription);
			return subscription.TransactionId;
		}

		/// <inheritdoc />
		public void UnSubscribeMarketData(MarketDataMessage message)
		{
			UnSubscribeMarketData(null, message);
		}

		/// <inheritdoc />
		public void UnSubscribeMarketData(Security security, MarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var subscription = _subscriptionManager.TryFindSubscription(0, message.ToDataType(), security);

			if (subscription != null)
				UnSubscribe(subscription);
		}

		private long SubscribeMarketData(Security security, MarketDataTypes type, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, Level1Fields? buildField = null, int? maxDepth = null, IMessageAdapter adapter = null)
		{
			return SubscribeMarketData(security, new MarketDataMessage
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
		public long RegisterFilteredMarketDepth(Security security)
		{
			var quotes = GetMarketDepth(security).ToMessage();
			var executions = _entityCache
				.GetOrders(security, OrderStates.Active)
				.Select(o => o.ToMessage())
				.ToArray();

			return SubscribeMarketData(security, new FilteredMarketDepthMessage
			{
				DataType = MarketDataTypes.MarketDepth,
				IsSubscribe = true,
				Arg = Tuple.Create(quotes, executions)
			});
		}

		/// <inheritdoc />
		public void UnRegisterFilteredMarketDepth(Security security)
		{
			var subscription = _subscriptionManager.TryFindFilteredMarketDepth(security);

			if (subscription == null)
				return;

			UnSubscribe(subscription);
		}

		/// <inheritdoc />
		public long SubscribeMarketDepth(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, int? maxDepth = null, IMessageAdapter adapter = null)
		{
			return SubscribeMarketData(security, MarketDataTypes.MarketDepth, from, to, count, buildMode, buildFrom, null, maxDepth, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeMarketDepth(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.MarketDepth);
		}

		/// <inheritdoc />
		public long SubscribeTrades(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, IMessageAdapter adapter = null)
		{
			return SubscribeMarketData(security, MarketDataTypes.Trades, from, to, count, buildMode, buildFrom, adapter: adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeTrades(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.Trades);
		}

		/// <inheritdoc />
		public long SubscribeLevel1(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, IMessageAdapter adapter = null)
		{
			return SubscribeMarketData(security, MarketDataTypes.Level1, from, to, count, buildMode, buildFrom, adapter: adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeLevel1(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.Level1);
		}

		/// <inheritdoc />
		public long SubscribeOrderLog(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			return SubscribeMarketData(security, MarketDataTypes.OrderLog, from, to, count, adapter: adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeOrderLog(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.OrderLog);
		}

		/// <inheritdoc />
		public long SubscribeNews(Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			return SubscribeMarketData(security, MarketDataTypes.News, from, to, count, adapter: adapter);
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
		public void LookupSecurities(Security criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var boardCode = criteria.Board?.Code;
			var securityCode = criteria.Code;

			if (!criteria.Id.IsEmpty())
			{
				var id = SecurityIdGenerator.Split(criteria.Id);

				if (boardCode.IsEmpty())
					boardCode = GetBoardCode(id.BoardCode);

				if (securityCode.IsEmpty())
					securityCode = id.SecurityCode;
			}

			var msg = criteria.ToLookupMessage(criteria.ExternalId.ToSecurityId(securityCode, boardCode));
			
			msg.Adapter = adapter;
			msg.OfflineMode = offlineMode;

			LookupSecurities(msg);
		}

		/// <inheritdoc />
		public void LookupSecurities(SecurityLookupMessage criteria)
		{
			Subscribe(new Subscription(criteria));
		}

		/// <inheritdoc />
		public void LookupTimeFrames(TimeFrameLookupMessage criteria)
		{
			Subscribe(new Subscription(criteria));
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribeOrders method.")]
		public void LookupOrders(Order criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
		{
			var msg = criteria.ToLookupCriteria(null, null);

			msg.Adapter = adapter;
			msg.OfflineMode = offlineMode;

			LookupOrders(msg);
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribeOrders method.")]
		public void LookupOrders(OrderStatusMessage criteria)
		{
			SubscribeOrders(criteria);
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribePositions method.")]
		public void LookupPortfolios(Portfolio criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var msg = criteria.ToLookupCriteria();

			msg.Adapter = adapter;
			msg.OfflineMode = offlineMode;

			LookupPortfolios(msg);
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribePositions method.")]
		public void LookupPortfolios(PortfolioLookupMessage criteria)
		{
			SubscribePositions(criteria);
		}

		/// <inheritdoc />
		public void LookupBoards(ExchangeBoard criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var msg = new BoardLookupMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				Like = criteria.Code,
				Adapter = adapter,
				OfflineMode = offlineMode,
			};

			LookupBoards(msg);
		}

		/// <inheritdoc />
		public void LookupBoards(BoardLookupMessage criteria)
		{
			Subscribe(new Subscription(criteria));
		}

		/// <inheritdoc />
		public long SubscribeBoard(ExchangeBoard board, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			return SubscribeMarketData(null, MarketDataTypes.Board, from, to, count, adapter: adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeBoard(ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			UnSubscribeMarketData(null, MarketDataTypes.Board);
		}

		/// <inheritdoc />
		public void UnSubscribe(long subscriptionId)
		{
			var subscription = TryGetSubscriptionById(subscriptionId);

			if (subscription == null)
				return;

			UnSubscribe(subscription);
		}

		/// <inheritdoc />
		public void SubscribeOrders(Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			SubscribeOrders(new OrderStatusMessage
			{
				IsSubscribe = true,
				SecurityId = security?.ToSecurityId() ?? default,
				From = from,
				To = to,
				Adapter = adapter,
			});
		}

		/// <inheritdoc />
		public void SubscribeOrders(OrderStatusMessage criteria)
		{
			Subscribe(new Subscription(criteria));
		}

		/// <inheritdoc />
		public void UnSubscribeOrders(long originalTransactionId = 0)
		{
			var security = _subscriptionManager.TryFindSubscription(originalTransactionId, DataType.Transactions);

			if (security != null)
				UnSubscribe(security);
		}

		/// <inheritdoc />
		public void RegisterPortfolio(Portfolio portfolio)
		{
			_subscriptionManager.RegisterPortfolio(portfolio);
		}

		/// <inheritdoc />
		public void UnRegisterPortfolio(Portfolio portfolio)
		{
			_subscriptionManager.UnRegisterPortfolio(portfolio);
		}

		/// <inheritdoc />
		public void RequestNewsStory(News news, IMessageAdapter adapter = null)
		{
			if (news == null)
				throw new ArgumentNullException(nameof(news));

			SubscribeMarketData(new MarketDataMessage
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
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		public void SubscribeCandles(CandleSeries series, DateTimeOffset? from = null, DateTimeOffset? to = null,
			long? count = null, long? transactionId = null, IMessageAdapter adapter = null)
		{
			this.AddInfoLog(nameof(SubscribeCandles));

			var subscription = new Subscription(series);

			var mdMsg = (MarketDataMessage)subscription.SubscriptionMessage;

			if (from != null)
				mdMsg.From = from.Value;

			if (to != null)
				mdMsg.To = to.Value;

			if (count != null)
				mdMsg.Count = count.Value;

			mdMsg.Adapter = adapter;

			if (transactionId != null)
				subscription.TransactionId = transactionId.Value;

			Subscribe(subscription);
		}

		/// <summary>
		/// To stop the candles receiving subscription, previously created by <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
			var subscription = _subscriptionManager.TryFindSubscription(series);

			if (subscription == null)
				return;

			UnSubscribe(subscription);
		}

		/// <inheritdoc />
		public void SubscribePositions(Security security = null, Portfolio portfolio = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			SubscribePositions(new PortfolioLookupMessage
			{
				Adapter = adapter,
				IsSubscribe = true,
				SecurityId = security?.ToSecurityId(),
				PortfolioName = portfolio?.Name,
				From = from,
				To = to,
			});
		}

		/// <inheritdoc />
		public void SubscribePositions(PortfolioLookupMessage criteria)
		{
			Subscribe(new Subscription(criteria));
		}

		/// <inheritdoc />
		public void UnSubscribePositions(long originalTransactionId = 0)
		{
			var security = _subscriptionManager.TryFindSubscription(originalTransactionId, DataType.PositionChanges);

			if (security != null)
				UnSubscribe(security);
		}

		/// <inheritdoc />
		public IEnumerable<Subscription> Subscriptions => _subscriptionManager.Subscriptions;

		/// <inheritdoc />
		public void Subscribe(Subscription subscription)
		{
			_subscriptionManager.Subscribe(subscription);
		}

		/// <inheritdoc />
		public void UnSubscribe(Subscription subscription)
		{
			_subscriptionManager.UnSubscribe(subscription);
		}

		/// <summary>
		/// Try get subscription by id.
		/// </summary>
		/// <param name="subscriptionId">Subscription id.</param>
		/// <returns>Subscription.</returns>
		public Subscription TryGetSubscriptionById(long subscriptionId)
		{
			return _subscriptionManager.TryGetSubscription(subscriptionId, false);
		}
	}
}