namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	partial class Strategy
	{
		private ISubscriptionProvider SubscriptionProvider => (ISubscriptionProvider)SafeGetConnector();

		IEnumerable<Subscription> ISubscriptionProvider.Subscriptions => _subscriptions.CachedKeys;

		/// <inheritdoc />
		public event Action<Subscription, Level1ChangeMessage> Level1Received;

		/// <inheritdoc />
		public event Action<Subscription, QuoteChangeMessage> OrderBookReceived;

		/// <inheritdoc />
		public event Action<Subscription, Trade> TickTradeReceived;

		/// <inheritdoc />
		public event Action<Subscription, Security> SecurityReceived;

		/// <inheritdoc />
		public event Action<Subscription, ExchangeBoard> BoardReceived;

		/// <inheritdoc />
		public event Action<Subscription, MarketDepth> MarketDepthReceived;

		/// <inheritdoc />
		public event Action<Subscription, OrderLogItem> OrderLogItemReceived;

		/// <inheritdoc />
		public event Action<Subscription, News> NewsReceived;

		/// <inheritdoc />
		public event Action<Subscription, Candle> CandleReceived;

		/// <inheritdoc />
		public event Action<Subscription, MyTrade> OwnTradeReceived;

		/// <inheritdoc />
		public event Action<Subscription, Order> OrderReceived;

		/// <inheritdoc />
		public event Action<Subscription, OrderFail> OrderRegisterFailReceived;

		/// <inheritdoc />
		public event Action<Subscription, OrderFail> OrderCancelFailReceived;

		/// <inheritdoc />
		public event Action<Subscription, OrderFail> OrderEditFailReceived;

		/// <inheritdoc />
		public event Action<Subscription, Portfolio> PortfolioReceived;

		/// <inheritdoc />
		public event Action<Subscription, Position> PositionReceived;

		/// <inheritdoc />
		public event Action<Subscription, Message> SubscriptionReceived;

		/// <inheritdoc />
		public event Action<Subscription> SubscriptionOnline;

		/// <inheritdoc />
		public event Action<Subscription> SubscriptionStarted;

		/// <inheritdoc />
		public event Action<Subscription, Exception> SubscriptionStopped;

		/// <inheritdoc />
		public event Action<Subscription, Exception, bool> SubscriptionFailed;

		/// <inheritdoc />
		public virtual void Subscribe(Subscription subscription)
		{
			Subscribe(subscription, false);
		}

		private void Subscribe(Subscription subscription, bool isGlobal)
		{
			_subscriptions.Add(subscription, isGlobal);

			if (subscription.TransactionId == default)
				subscription.TransactionId = Connector.TransactionIdGenerator.GetNextId();

			_subscriptionsById.Add(subscription.TransactionId, subscription);

			SubscriptionProvider.Subscribe(subscription);
		}

		/// <inheritdoc />
		public virtual void UnSubscribe(Subscription subscription)
		{
			SubscriptionProvider.UnSubscribe(subscription);
		}

		private void OnConnectorSubscriptionFailed(Subscription subscription, Exception error, bool isSubscribe)
		{
			if (_subscriptions.ContainsKey(subscription))
				SubscriptionFailed?.Invoke(subscription, error, isSubscribe);
		}

		private void OnConnectorSubscriptionStopped(Subscription subscription, Exception error)
		{
			if (_subscriptions.ContainsKey(subscription))
				SubscriptionStopped?.Invoke(subscription, error);
		}

		private void OnConnectorSubscriptionStarted(Subscription subscription)
		{
			if (_subscriptions.ContainsKey(subscription))
				SubscriptionStarted?.Invoke(subscription);
		}

		private void OnConnectorSubscriptionOnline(Subscription subscription)
		{
			if (_subscriptions.ContainsKey(subscription))
				SubscriptionOnline?.Invoke(subscription);
		}

		private void OnConnectorSubscriptionReceived(Subscription subscription, Message message)
		{
			if (_subscriptions.ContainsKey(subscription))
				SubscriptionReceived?.Invoke(subscription, message);
		}

		private void OnConnectorCandleReceived(Subscription subscription, Candle candle)
		{
			if (_subscriptions.ContainsKey(subscription))
				CandleReceived?.Invoke(subscription, candle);
		}

		private void OnConnectorNewsReceived(Subscription subscription, News news)
		{
			if (_subscriptions.ContainsKey(subscription))
				NewsReceived?.Invoke(subscription, news);
		}

		private void OnConnectorOrderLogItemReceived(Subscription subscription, OrderLogItem ol)
		{
			if (_subscriptions.ContainsKey(subscription))
				OrderLogItemReceived?.Invoke(subscription, ol);
		}

		private void OnConnectorMarketDepthReceived(Subscription subscription, MarketDepth depth)
		{
			if (_subscriptions.ContainsKey(subscription))
				MarketDepthReceived?.Invoke(subscription, depth);
		}

		private void OnConnectorBoardReceived(Subscription subscription, ExchangeBoard board)
		{
			if (_subscriptions.ContainsKey(subscription))
				BoardReceived?.Invoke(subscription, board);
		}

		private void OnConnectorSecurityReceived(Subscription subscription, Security security)
		{
			if (_subscriptions.ContainsKey(subscription))
				SecurityReceived?.Invoke(subscription, security);
		}

		private void OnConnectorTickTradeReceived(Subscription subscription, Trade trade)
		{
			if (_subscriptions.ContainsKey(subscription))
				TickTradeReceived?.Invoke(subscription, trade);
		}

		private void OnConnectorOrderBookReceived(Subscription subscription, QuoteChangeMessage message)
		{
			if (_subscriptions.ContainsKey(subscription))
				OrderBookReceived?.Invoke(subscription, message);
		}

		private void OnConnectorLevel1Received(Subscription subscription, Level1ChangeMessage message)
		{
			if (_subscriptions.ContainsKey(subscription))
				Level1Received?.Invoke(subscription, message);
		}
	}
}