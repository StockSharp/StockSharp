namespace StockSharp.Algo
{
	using System;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Algo.Candles;

	partial class MarketRuleHelper
	{
		private abstract class SubscriptionRule<TArg> : MarketRule<Subscription, TArg>
		{
			protected SubscriptionRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription)
			{
				Provider = provider;
				Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
			}

			protected ISubscriptionProvider Provider { get; }
			protected Subscription Subscription { get; }
		}

		private class SubscriptionStartedRule : SubscriptionRule<Subscription>
		{
			public SubscriptionStartedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.SubscriptionStarted)}";
				Provider.SubscriptionStarted += ProviderOnSubscriptionStarted;
			}

			private void ProviderOnSubscriptionStarted(Subscription subscription)
			{
				if (Subscription == subscription)
					Activate(subscription);
			}

			protected override void DisposeManaged()
			{
				Provider.SubscriptionStarted -= ProviderOnSubscriptionStarted;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.SubscriptionStarted"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, Subscription> WhenSubscriptionStarted(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new SubscriptionStartedRule(subscription, provider);
		}

		private class SubscriptionOnlineRule : SubscriptionRule<Subscription>
		{
			public SubscriptionOnlineRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.SubscriptionOnline)}";
				Provider.SubscriptionOnline += ProviderOnSubscriptionOnline;
			}

			private void ProviderOnSubscriptionOnline(Subscription subscription)
			{
				if (Subscription == subscription)
					Activate(subscription);
			}

			protected override void DisposeManaged()
			{
				Provider.SubscriptionOnline -= ProviderOnSubscriptionOnline;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.SubscriptionOnline"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, Subscription> WhenSubscriptionOnline(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new SubscriptionOnlineRule(subscription, provider);
		}

		private class SubscriptionStoppedRule : MarketRule<Subscription, Tuple<Subscription, Exception>>
		{
			private readonly ISubscriptionProvider _provider;
			private readonly Subscription _subscription;

			public SubscriptionStoppedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription)
			{
				_provider = provider ?? throw new ArgumentNullException(nameof(provider));
				_subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));

				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.SubscriptionStopped)}";
				
				_provider.SubscriptionStopped += ProviderOnSubscriptionStopped;
			}

			private void ProviderOnSubscriptionStopped(Subscription subscription, Exception error)
			{
				if (_subscription == subscription)
					Activate(Tuple.Create(subscription, error));
			}

			protected override void DisposeManaged()
			{
				_provider.SubscriptionStopped -= ProviderOnSubscriptionStopped;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.SubscriptionStopped"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, Tuple<Subscription, Exception>> WhenSubscriptionStopped(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new SubscriptionStoppedRule(subscription, provider);
		}

		private class SubscriptionFailedRule : MarketRule<Subscription, Tuple<Subscription, Exception, bool>>
		{
			private readonly ISubscriptionProvider _provider;
			private readonly Subscription _subscription;

			public SubscriptionFailedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription)
			{
				_provider = provider ?? throw new ArgumentNullException(nameof(provider));
				_subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
				
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.SubscriptionFailed)}";
				
				_provider.SubscriptionFailed += ProviderOnSubscriptionFailed;
			}

			private void ProviderOnSubscriptionFailed(Subscription subscription, Exception error, bool isSubscribe)
			{
				if (_subscription == subscription)
					Activate(Tuple.Create(subscription, error, isSubscribe));
			}

			protected override void DisposeManaged()
			{
				_provider.SubscriptionFailed -= ProviderOnSubscriptionFailed;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.SubscriptionFailed"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, Tuple<Subscription, Exception, bool>> WhenSubscriptionFailed(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new SubscriptionFailedRule(subscription, provider);
		}

		private class OrderBookReceivedRule : SubscriptionRule<QuoteChangeMessage>
		{
			public OrderBookReceivedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.OrderBookReceived)}";
				Provider.OrderBookReceived += ProviderOnOrderBookReceived;
			}

			private void ProviderOnOrderBookReceived(Subscription subscription, QuoteChangeMessage message)
			{
				if (Subscription == subscription)
					Activate(message);
			}

			protected override void DisposeManaged()
			{
				Provider.OrderBookReceived -= ProviderOnOrderBookReceived;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.OrderBookReceived"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, QuoteChangeMessage> WhenOrderBookReceived(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new OrderBookReceivedRule(subscription, provider);
		}

		private class MarketDepthReceivedRule : SubscriptionRule<MarketDepth>
		{
			public MarketDepthReceivedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.MarketDepthReceived)}";
				Provider.MarketDepthReceived += ProviderOnMarketDepthReceived;
			}

			private void ProviderOnMarketDepthReceived(Subscription subscription, MarketDepth depth)
			{
				if (Subscription == subscription)
					Activate(depth);
			}

			protected override void DisposeManaged()
			{
				Provider.MarketDepthReceived -= ProviderOnMarketDepthReceived;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.MarketDepthReceived"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, MarketDepth> WhenMarketDepthReceived(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new MarketDepthReceivedRule(subscription, provider);
		}

		private class Level1ReceivedRule : SubscriptionRule<Level1ChangeMessage>
		{
			public Level1ReceivedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.Level1Received)}";
				Provider.Level1Received += ProviderOnLevel1Received;
			}

			private void ProviderOnLevel1Received(Subscription subscription, Level1ChangeMessage message)
			{
				if (Subscription == subscription)
					Activate(message);
			}

			protected override void DisposeManaged()
			{
				Provider.Level1Received -= ProviderOnLevel1Received;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.Level1Received"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, Level1ChangeMessage> WhenLevel1Received(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new Level1ReceivedRule(subscription, provider);
		}

		private class OrderLogReceivedRule : SubscriptionRule<OrderLogItem>
		{
			public OrderLogReceivedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.OrderLogItemReceived)}";
				Provider.OrderLogItemReceived += ProviderOnOrderLogItemReceived;
			}

			private void ProviderOnOrderLogItemReceived(Subscription subscription, OrderLogItem item)
			{
				if (Subscription == subscription)
					Activate(item);
			}

			protected override void DisposeManaged()
			{
				Provider.OrderLogItemReceived -= ProviderOnOrderLogItemReceived;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.OrderLogItemReceived"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, OrderLogItem> WhenOrderLogReceived(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new OrderLogReceivedRule(subscription, provider);
		}

		private class TickTradeReceivedRule : SubscriptionRule<Trade>
		{
			public TickTradeReceivedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.TickTradeReceived)}";
				Provider.TickTradeReceived += ProviderOnTickTradeReceived;
			}

			private void ProviderOnTickTradeReceived(Subscription subscription, Trade trade)
			{
				if (Subscription == subscription)
					Activate(trade);
			}

			protected override void DisposeManaged()
			{
				Provider.TickTradeReceived -= ProviderOnTickTradeReceived;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.TickTradeReceived"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, Trade> WhenTickTradeReceived(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new TickTradeReceivedRule(subscription, provider);
		}

		private class CandleReceivedRule : SubscriptionRule<Candle>
		{
			public CandleReceivedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.CandleReceived)}";
				Provider.CandleReceived += ProviderOnCandleReceived;
			}

			private void ProviderOnCandleReceived(Subscription subscription, Candle candle)
			{
				if (Subscription == subscription)
					Activate(candle);
			}

			protected override void DisposeManaged()
			{
				Provider.CandleReceived -= ProviderOnCandleReceived;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.CandleReceived"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, Candle> WhenCandleReceived(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new CandleReceivedRule(subscription, provider);
		}

		private class NewsReceivedRule : SubscriptionRule<News>
		{
			public NewsReceivedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.NewsReceived)}";
				Provider.NewsReceived += ProviderOnNewsReceived;
			}

			private void ProviderOnNewsReceived(Subscription subscription, News news)
			{
				if (Subscription == subscription)
					Activate(news);
			}

			protected override void DisposeManaged()
			{
				Provider.NewsReceived -= ProviderOnNewsReceived;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.NewsReceived"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, News> WhenNewsReceived(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new NewsReceivedRule(subscription, provider);
		}

		private class OwnTradeReceivedRule : SubscriptionRule<MyTrade>
		{
			public OwnTradeReceivedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.OwnTradeReceived)}";
				Provider.OwnTradeReceived += ProviderOnOwnTradeReceived;
			}

			private void ProviderOnOwnTradeReceived(Subscription subscription, MyTrade trade)
			{
				if (Subscription == subscription)
					Activate(trade);
			}

			protected override void DisposeManaged()
			{
				Provider.OwnTradeReceived -= ProviderOnOwnTradeReceived;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.OwnTradeReceived"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, MyTrade> WhenOwnTradeReceived(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new OwnTradeReceivedRule(subscription, provider);
		}

		private class OrderReceivedRule : SubscriptionRule<Order>
		{
			public OrderReceivedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.OrderReceived)}";
				Provider.OrderReceived += ProviderOnOrderReceived;
			}

			private void ProviderOnOrderReceived(Subscription subscription, Order order)
			{
				if (Subscription == subscription)
					Activate(order);
			}

			protected override void DisposeManaged()
			{
				Provider.OrderReceived -= ProviderOnOrderReceived;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.OrderReceived"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, Order> WhenOrderReceived(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new OrderReceivedRule(subscription, provider);
		}

		private class OrderFailReceivedRule : SubscriptionRule<OrderFail>
		{
			private bool _isRegister;

			public OrderFailReceivedRule(Subscription subscription, ISubscriptionProvider provider, bool isRegister)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(OrderFail)}Received";

				_isRegister = isRegister;

				if (_isRegister)
					Provider.OrderRegisterFailReceived += ProviderOnOrderFailReceived;
				else
					Provider.OrderCancelFailReceived += ProviderOnOrderFailReceived;
			}

			private void ProviderOnOrderFailReceived(Subscription subscription, OrderFail fail)
			{
				if (Subscription == subscription)
					Activate(fail);
			}

			protected override void DisposeManaged()
			{
				if (_isRegister)
					Provider.OrderRegisterFailReceived -= ProviderOnOrderFailReceived;
				else
					Provider.OrderCancelFailReceived -= ProviderOnOrderFailReceived;

				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.OrderRegisterFailReceived"/> or <see cref="ISubscriptionProvider.OrderCancelFailReceived"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="isRegister"><see cref="ISubscriptionProvider.OrderRegisterFailReceived"/> or <see cref="ISubscriptionProvider.OrderCancelFailReceived"/>.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, OrderFail> WhenOrderFailReceived(this Subscription subscription, ISubscriptionProvider provider, bool isRegister)
		{
			return new OrderFailReceivedRule(subscription, provider, isRegister);
		}

		private class PositionReceivedRule : SubscriptionRule<Position>
		{
			public PositionReceivedRule(Subscription subscription, ISubscriptionProvider provider)
				: base(subscription, provider)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.PositionReceived)}";
				Provider.PositionReceived += ProviderOnPositionReceived;
			}

			private void ProviderOnPositionReceived(Subscription subscription, Position position)
			{
				if (Subscription == subscription)
					Activate(position);
			}

			protected override void DisposeManaged()
			{
				Provider.PositionReceived -= ProviderOnPositionReceived;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of <see cref="ISubscriptionProvider.PositionReceived"/>.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <param name="provider">Subscription provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, Position> WhenPositionReceived(this Subscription subscription, ISubscriptionProvider provider)
		{
			return new PositionReceivedRule(subscription, provider);
		}
	}
}
