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
		public event Action<Subscription, Level1ChangeMessage> Level1Received
		{
			add => SubscriptionProvider.Level1Received += value;
			remove => SubscriptionProvider.Level1Received -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, QuoteChangeMessage> OrderBookReceived
		{
			add => SubscriptionProvider.OrderBookReceived += value;
			remove => SubscriptionProvider.OrderBookReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, Trade> TickTradeReceived
		{
			add => SubscriptionProvider.TickTradeReceived += value;
			remove => SubscriptionProvider.TickTradeReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, Security> SecurityReceived
		{
			add => SubscriptionProvider.SecurityReceived += value;
			remove => SubscriptionProvider.SecurityReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, ExchangeBoard> BoardReceived
		{
			add => SubscriptionProvider.BoardReceived += value;
			remove => SubscriptionProvider.BoardReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, MarketDepth> MarketDepthReceived
		{
			add => SubscriptionProvider.MarketDepthReceived += value;
			remove => SubscriptionProvider.MarketDepthReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, OrderLogItem> OrderLogItemReceived
		{
			add => SubscriptionProvider.OrderLogItemReceived += value;
			remove => SubscriptionProvider.OrderLogItemReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, News> NewsReceived
		{
			add => SubscriptionProvider.NewsReceived += value;
			remove => SubscriptionProvider.NewsReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, Candle> CandleReceived
		{
			add => SubscriptionProvider.CandleReceived += value;
			remove => SubscriptionProvider.CandleReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, MyTrade> OwnTradeReceived
		{
			add => SubscriptionProvider.OwnTradeReceived += value;
			remove => SubscriptionProvider.OwnTradeReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, Order> OrderReceived
		{
			add => SubscriptionProvider.OrderReceived += value;
			remove => SubscriptionProvider.OrderReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, OrderFail> OrderRegisterFailReceived
		{
			add => SubscriptionProvider.OrderRegisterFailReceived += value;
			remove => SubscriptionProvider.OrderRegisterFailReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, OrderFail> OrderCancelFailReceived
		{
			add => SubscriptionProvider.OrderCancelFailReceived += value;
			remove => SubscriptionProvider.OrderCancelFailReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, OrderFail> OrderEditFailReceived
		{
			add => SubscriptionProvider.OrderEditFailReceived += value;
			remove => SubscriptionProvider.OrderEditFailReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, Portfolio> PortfolioReceived
		{
			add => SubscriptionProvider.PortfolioReceived += value;
			remove => SubscriptionProvider.PortfolioReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, Position> PositionReceived
		{
			add => SubscriptionProvider.PositionReceived += value;
			remove => SubscriptionProvider.PositionReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, Message> SubscriptionReceived
		{
			add => SubscriptionProvider.SubscriptionReceived += value;
			remove => SubscriptionProvider.SubscriptionReceived -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription> SubscriptionOnline
		{
			add => SubscriptionProvider.SubscriptionOnline += value;
			remove => SubscriptionProvider.SubscriptionOnline -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription> SubscriptionStarted
		{
			add => SubscriptionProvider.SubscriptionStarted += value;
			remove => SubscriptionProvider.SubscriptionStarted -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, Exception> SubscriptionStopped
		{
			add => SubscriptionProvider.SubscriptionStopped += value;
			remove => SubscriptionProvider.SubscriptionStopped -= value;
		}

		/// <inheritdoc />
		public event Action<Subscription, Exception, bool> SubscriptionFailed
		{
			add => SubscriptionProvider.SubscriptionFailed += value;
			remove => SubscriptionProvider.SubscriptionFailed -= value;
		}

		/// <inheritdoc />
		public void Subscribe(Subscription subscription)
		{
			Subscribe(subscription, false);
		}

		private void Subscribe(Subscription subscription, bool isGlobal)
		{
			_subscriptions.Add(subscription, isGlobal);
			SubscriptionProvider.Subscribe(subscription);
		}

		/// <inheritdoc />
		public void UnSubscribe(Subscription subscription)
		{
			SubscriptionProvider.UnSubscribe(subscription);
		}
	}
}