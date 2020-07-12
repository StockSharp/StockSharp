namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	partial class Connector
	{
		/// <summary>
		/// Find subscriptions for the specified security and data type.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="dataType">Data type info.</param>
		/// <returns>Subscriptions.</returns>
		public IEnumerable<Subscription> FindSubscriptions(Security security, DataType dataType)
		{
			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			var secId = security.ToSecurityId();
			return Subscriptions.Where(s => s.DataType == dataType && s.SecurityId == secId);
		}

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
		/// List of all candles series, subscribed via <see cref="ICandleSource{Candle}.Start"/>.
		/// </summary>
		public IEnumerable<CandleSeries> SubscribedCandleSeries => _subscriptionManager.SubscribedCandleSeries;

		/// <inheritdoc />
		[Obsolete("Use Subscribe method instead.")]
		public void RegisterSecurity(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, IMessageAdapter adapter = null)
			=> this.SubscribeLevel1(security, from, to, count, buildMode, buildFrom, adapter);

		/// <inheritdoc />
		[Obsolete("Use UnSubscribe method instead.")]
		public void UnRegisterSecurity(Security security) => this.UnSubscribeLevel1(security);

		/// <inheritdoc />
		[Obsolete("Use Subscribe method instead.")]
		public void RegisterMarketDepth(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, int? maxDepth = null, IMessageAdapter adapter = null)
			=> this.SubscribeMarketDepth(security, from, to, count, buildMode, buildFrom, maxDepth, null, null, false, adapter);

		/// <inheritdoc />
		[Obsolete("Use UnSubscribeMarketDepth method instead.")]
		public void UnRegisterMarketDepth(Security security) => this.UnSubscribeMarketDepth(security);

		/// <inheritdoc />
		[Obsolete("Use SubscribeTrades method instead.")]
		public void RegisterTrades(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, IMessageAdapter adapter = null)
			=> this.SubscribeTrades(security, from, to, count, buildMode, buildFrom, adapter);

		/// <inheritdoc />
		[Obsolete("Use UnSubscribeTrades method instead.")]
		public void UnRegisterTrades(Security security)	=> this.UnSubscribeTrades(security);

		/// <inheritdoc />
		[Obsolete("Use SubscribeOrderLog method instead.")]
		public void RegisterOrderLog(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
			=> this.SubscribeOrderLog(security, from, to, count, adapter);

		/// <inheritdoc />
		[Obsolete("Use UnSubscribeOrderLog method instead.")]
		public void UnRegisterOrderLog(Security security) => this.UnSubscribeOrderLog(security);

		/// <inheritdoc />
		[Obsolete("Use SubscribeNews method instead.")]
		public void RegisterNews(Security security = null, IMessageAdapter adapter = null) => this.SubscribeNews(security, adapter: adapter);

		/// <inheritdoc />
		[Obsolete("Use UnSubscribeNews method instead.")]
		public void UnRegisterNews(Security security = null) => this.UnSubscribeNews(security);

		/// <inheritdoc />
		public void RegisterPortfolio(Portfolio portfolio) => _subscriptionManager.RegisterPortfolio(portfolio);

		/// <inheritdoc />
		public void UnRegisterPortfolio(Portfolio portfolio) => _subscriptionManager.UnRegisterPortfolio(portfolio);

		/// <inheritdoc />
		public void RequestNewsStory(News news, IMessageAdapter adapter = null)
		{
			if (news is null)
				throw new ArgumentNullException(nameof(news));

			this.SubscribeMarketData(new MarketDataMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType2 = DataType.News,
				IsSubscribe = true,
				NewsId = news.Id.To<string>(),
				Adapter = adapter,
			});
		}

		/// <inheritdoc />
		public IEnumerable<Subscription> Subscriptions => _subscriptionManager.Subscriptions;

		/// <inheritdoc />
		public void Subscribe(Subscription subscription) => _subscriptionManager.Subscribe(subscription);

		/// <inheritdoc />
		public void UnSubscribe(Subscription subscription) => _subscriptionManager.UnSubscribe(subscription);

		/// <summary>
		/// Try get subscription by id.
		/// </summary>
		/// <param name="subscriptionId">Subscription id.</param>
		/// <returns>Subscription.</returns>
		public Subscription TryGetSubscriptionById(long subscriptionId)
			=> _subscriptionManager.TryGetSubscription(subscriptionId, true, false, null)?.Subscription;
	}
}