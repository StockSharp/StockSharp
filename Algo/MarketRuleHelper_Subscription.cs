namespace StockSharp.Algo;

partial class MarketRuleHelper
{
	private abstract class SubscriptionRule<TArg>(Subscription subscription, ISubscriptionProvider provider) : MarketRule<Subscription, TArg>(subscription)
	{
		protected ISubscriptionProvider Provider { get; } = provider;
		protected Subscription Subscription { get; } = subscription ?? throw new ArgumentNullException(nameof(subscription));
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

	private class OrderBookReceivedRule : SubscriptionRule<IOrderBookMessage>
	{
		public OrderBookReceivedRule(Subscription subscription, ISubscriptionProvider provider)
			: base(subscription, provider)
		{
			Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.OrderBookReceived)}";
			Provider.OrderBookReceived += ProviderOnOrderBookReceived;
		}

		private void ProviderOnOrderBookReceived(Subscription subscription, IOrderBookMessage message)
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
	public static MarketRule<Subscription, IOrderBookMessage> WhenOrderBookReceived(this Subscription subscription, ISubscriptionProvider provider)
	{
		return new OrderBookReceivedRule(subscription, provider);
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

	private class OrderLogReceivedRule : SubscriptionRule<IOrderLogMessage>
	{
		public OrderLogReceivedRule(Subscription subscription, ISubscriptionProvider provider)
			: base(subscription, provider)
		{
			Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.OrderLogReceived)}";
			Provider.OrderLogReceived += ProviderOnOrderLogReceived;
		}

		private void ProviderOnOrderLogReceived(Subscription subscription, IOrderLogMessage item)
		{
			if (Subscription == subscription)
				Activate(item);
		}

		protected override void DisposeManaged()
		{
			Provider.OrderLogReceived -= ProviderOnOrderLogReceived;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// To create a rule for the event of <see cref="ISubscriptionProvider.OrderLogReceived"/>.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="provider">Subscription provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, IOrderLogMessage> WhenOrderLogReceived(this Subscription subscription, ISubscriptionProvider provider)
	{
		return new OrderLogReceivedRule(subscription, provider);
	}

	private class TickTradeReceivedRule : SubscriptionRule<ITickTradeMessage>
	{
		public TickTradeReceivedRule(Subscription subscription, ISubscriptionProvider provider)
			: base(subscription, provider)
		{
			Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.TickTradeReceived)}";
			Provider.TickTradeReceived += ProviderOnTickTradeReceived;
		}

		private void ProviderOnTickTradeReceived(Subscription subscription, ITickTradeMessage trade)
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
	public static MarketRule<Subscription, ITickTradeMessage> WhenTickTradeReceived(this Subscription subscription, ISubscriptionProvider provider)
	{
		return new TickTradeReceivedRule(subscription, provider);
	}

	private class CandleReceivedRule<TCandle> : SubscriptionRule<TCandle>
		where TCandle : ICandleMessage
	{
		public CandleReceivedRule(Subscription subscription, ISubscriptionProvider provider)
			: base(subscription, provider)
		{
			Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.CandleReceived)}";
			Provider.CandleReceived += ProviderOnCandleReceived;
		}

		private void ProviderOnCandleReceived(Subscription subscription, ICandleMessage candle)
		{
			if (Subscription == subscription)
				Activate((TCandle)candle);
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
	public static MarketRule<Subscription, ICandleMessage> WhenCandleReceived(this Subscription subscription, ISubscriptionProvider provider)
		=> WhenCandleReceived<ICandleMessage>(subscription, provider);

	/// <summary>
	/// To create a rule for the event of <see cref="ISubscriptionProvider.CandleReceived"/>.
	/// </summary>
	/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
	/// <param name="subscription">Subscription.</param>
	/// <param name="provider">Subscription provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, TCandle> WhenCandleReceived<TCandle>(this Subscription subscription, ISubscriptionProvider provider)
		where TCandle : ICandleMessage
	{
		return new CandleReceivedRule<TCandle>(subscription, provider);
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

	/// <summary>
	/// To create a rule for the event of <see cref="ISubscriptionProvider.OwnTradeReceived"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, MyTrade> WhenOwnTradeReceived(this ISubscriptionProvider provider)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		return WhenOwnTradeReceived(provider.OrderLookup, provider);
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

	/// <summary>
	/// To create a rule for the event of <see cref="ISubscriptionProvider.OrderReceived"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, Order> WhenOrderReceived(this ISubscriptionProvider provider)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		return WhenOrderReceived(provider.OrderLookup, provider);
	}

	private class OrderRegisteredRule : SubscriptionRule<Order>
	{
		private readonly HashSet<Order> _orders = [];

		public OrderRegisteredRule(Subscription subscription, ISubscriptionProvider provider)
			: base(subscription, provider)
		{
			Name = $"{subscription.TransactionId}/{subscription.DataType} OrderRegistered";
			Provider.OrderReceived += ProviderOnOrderReceived;
		}

		private void ProviderOnOrderReceived(Subscription subscription, Order order)
		{
			if (Subscription == subscription && _orders.Add(order))
				Activate(order);
		}

		protected override void DisposeManaged()
		{
			Provider.OrderReceived -= ProviderOnOrderReceived;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// To create a rule for event of occurrence of new order.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="provider">Subscription provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, Order> WhenOrderRegistered(this Subscription subscription, ISubscriptionProvider provider)
	{
		return new OrderRegisteredRule(subscription, provider);
	}

	/// <summary>
	/// To create a rule for event of occurrence of new order.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, Order> WhenOrderRegistered(this ISubscriptionProvider provider)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		return WhenOrderRegistered(provider.OrderLookup, provider);
	}

	private class OrderFailReceivedRule : SubscriptionRule<OrderFail>
	{
		private readonly bool _isRegister;

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

	private class OrderEditFailReceivedRule : SubscriptionRule<OrderFail>
	{
		public OrderEditFailReceivedRule(Subscription subscription, ISubscriptionProvider provider)
			: base(subscription, provider)
		{
			Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(OrderFail)}Received";
			Provider.OrderEditFailReceived += ProviderOnOrderFailReceived;
		}

		private void ProviderOnOrderFailReceived(Subscription subscription, OrderFail fail)
		{
			if (Subscription == subscription)
				Activate(fail);
		}

		protected override void DisposeManaged()
		{
			Provider.OrderEditFailReceived -= ProviderOnOrderFailReceived;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// To create a rule for the event of <see cref="ISubscriptionProvider.OrderEditFailReceived"/> or <see cref="ISubscriptionProvider.OrderCancelFailReceived"/>.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="provider">Subscription provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, OrderFail> WhenOrderEditFailReceived(this Subscription subscription, ISubscriptionProvider provider)
		=> new OrderEditFailReceivedRule(subscription, provider);

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

	/// <summary>
	/// To create a rule for the event of <see cref="ISubscriptionProvider.PositionReceived"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, Position> WhenPositionReceived(this ISubscriptionProvider provider)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		return WhenPositionReceived(provider.PortfolioLookup, provider);
	}

	private class PortfolioReceivedRule : SubscriptionRule<Portfolio>
	{
		public PortfolioReceivedRule(Subscription subscription, ISubscriptionProvider provider)
			: base(subscription, provider)
		{
			Name = $"{subscription.TransactionId}/{subscription.DataType} {nameof(ISubscriptionProvider.PortfolioReceived)}";
			Provider.PortfolioReceived += ProviderOnPortfolioReceived;
		}

		private void ProviderOnPortfolioReceived(Subscription subscription, Portfolio portfolio)
		{
			if (Subscription == subscription)
				Activate(portfolio);
		}

		protected override void DisposeManaged()
		{
			Provider.PortfolioReceived -= ProviderOnPortfolioReceived;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// To create a rule for the event of <see cref="ISubscriptionProvider.PortfolioReceived"/>.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="provider">Subscription provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, Portfolio> WhenPortfolioReceived(this Subscription subscription, ISubscriptionProvider provider)
	{
		return new PortfolioReceivedRule(subscription, provider);
	}

	/// <summary>
	/// To create a rule for the event of <see cref="ISubscriptionProvider.PortfolioReceived"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, Portfolio> WhenPortfolioReceived(this ISubscriptionProvider provider)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		return WhenPortfolioReceived(provider.PortfolioLookup, provider);
	}

	private class ConditionOrderBookReceivedRule(Subscription subscription, ISubscriptionProvider provider, Func<IOrderBookMessage, bool> condition) : OrderBookReceivedRule(subscription, provider)
	{
		private readonly Func<IOrderBookMessage, bool> _condition = condition ?? throw new ArgumentNullException(nameof(condition));

		protected override void Activate(IOrderBookMessage book)
		{
			if (_condition(book))
				base.Activate(book);
		}
	}

	/// <summary>
	/// To create a rule for the event of excess of the best bid of specific level.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="provider"><see cref="ISubscriptionProvider"/></param>
	/// <param name="level">The level.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, IOrderBookMessage> WhenBestBidPriceMore(this Subscription subscription, ISubscriptionProvider provider, decimal level)
		=> new ConditionOrderBookReceivedRule(subscription, provider, b => b.GetBestBid()?.Price > level);

	/// <summary>
	/// To create a rule for the event of dropping the best bid below the specific level.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="provider"><see cref="ISubscriptionProvider"/></param>
	/// <param name="level">The level.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, IOrderBookMessage> WhenBestBidPriceLess(this Subscription subscription, ISubscriptionProvider provider, decimal level)
		=> new ConditionOrderBookReceivedRule(subscription, provider, b => b.GetBestBid()?.Price < level);

	/// <summary>
	/// To create a rule for the event of excess of the best offer of the specific level.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="provider"><see cref="ISubscriptionProvider"/></param>
	/// <param name="level">The level.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, IOrderBookMessage> WhenBestAskPriceMore(this Subscription subscription, ISubscriptionProvider provider, decimal level)
		=> new ConditionOrderBookReceivedRule(subscription, provider, b => b.GetBestAsk()?.Price > level);

	/// <summary>
	/// To create a rule for the event of dropping the best offer below the specific level.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="provider"><see cref="ISubscriptionProvider"/></param>
	/// <param name="level">The level.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, IOrderBookMessage> WhenBestAskPriceLess(this Subscription subscription, ISubscriptionProvider provider, decimal level)
		=> new ConditionOrderBookReceivedRule(subscription, provider, b => b.GetBestAsk()?.Price < level);

	private class ConditionTickTradeReceivedRule(Subscription subscription, ISubscriptionProvider provider, Func<ITickTradeMessage, bool> condition) : TickTradeReceivedRule(subscription, provider)
	{
		private readonly Func<ITickTradeMessage, bool> _condition = condition ?? throw new ArgumentNullException(nameof(condition));

		protected override void Activate(ITickTradeMessage tick)
		{
			if (_condition(tick))
				base.Activate(tick);
		}
	}

	/// <summary>
	/// To create a rule for the event of increase of the last trade price above the specific level.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="provider"><see cref="ISubscriptionProvider"/></param>
	/// <param name="level">The level.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, ITickTradeMessage> WhenLastTradePriceMore(this Subscription subscription, ISubscriptionProvider provider, decimal level)
		=> new ConditionTickTradeReceivedRule(subscription, provider, t => t.Price > level);

	/// <summary>
	/// To create a rule for the event of reduction of the last trade price below the specific level.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="provider"><see cref="ISubscriptionProvider"/></param>
	/// <param name="level">The level.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Subscription, ITickTradeMessage> WhenLastTradePriceLess(this Subscription subscription, ISubscriptionProvider provider, decimal level)
		=> new ConditionTickTradeReceivedRule(subscription, provider, t => t.Price < level);
}
