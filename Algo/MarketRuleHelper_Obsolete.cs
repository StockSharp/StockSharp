namespace StockSharp.Algo;

using StockSharp.Algo.Candles;

partial class MarketRuleHelper
{
	private abstract class SecurityRule<TArg> : MarketRule<Security, TArg>
	{
		protected SecurityRule(Security security, IMarketDataProvider provider)
			: base(security)
		{
			Security = security ?? throw new ArgumentNullException(nameof(security));
			Provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		protected Security Security { get; }
		protected IMarketDataProvider Provider { get; }
	}

	[Obsolete]
	private class SecurityChangedRule : SecurityRule<Security>
	{
		private readonly Func<Security, bool> _condition;

		public SecurityChangedRule(Security security, IMarketDataProvider provider)
			: this(security, provider, s => true)
		{
		}

		public SecurityChangedRule(Security security, IMarketDataProvider provider, Func<Security, bool> condition)
			: base(security, provider)
		{
			_condition = condition ?? throw new ArgumentNullException(nameof(condition));

			Name = LocalizedStrings.Level1 + " " + security;

			Provider.SecurityChanged += OnSecurityChanged;
		}

		private void OnSecurityChanged(Security security)
		{
			if (Security is BasketSecurity basket)
			{
				if (basket.Contains(ServicesRegistry.TrySecurityProvider, security) && _condition(security))
					Activate(security);
			}
			else
			{
				if (security == Security && _condition(Security))
					Activate(Security);
			}
		}

		protected override void DisposeManaged()
		{
			Provider.SecurityChanged -= OnSecurityChanged;
			base.DisposeManaged();
		}
	}

	[Obsolete]
	private class SecurityNewTradeRule : SecurityRule<Trade>
	{
		public SecurityNewTradeRule(Security security, IMarketDataProvider provider)
			: base(security, provider)
		{
			Name = LocalizedStrings.TradesElement + " " + security;
			Provider.NewTrade += OnNewTrade;
		}

		private void OnNewTrade(Trade trade)
		{
			var sec = Security;

			var basket = sec as BasketSecurity;

			var has = basket?.Contains(ServicesRegistry.TrySecurityProvider, trade.Security) ?? trade.Security == sec;

			if (has)
				Activate(trade);
		}

		protected override void DisposeManaged()
		{
			Provider.NewTrade -= OnNewTrade;
			base.DisposeManaged();
		}
	}

	[Obsolete]
	private class SecurityNewOrderLogItemRule : SecurityRule<OrderLogItem>
	{
		public SecurityNewOrderLogItemRule(Security security, IMarketDataProvider provider)
			: base(security, provider)
		{
			Name = LocalizedStrings.OrderLog + " " + security;
			Provider.NewOrderLogItem += OnNewOrderLogItem;
		}

		private void OnNewOrderLogItem(OrderLogItem item)
		{
			var sec = Security;

			var basket = sec as BasketSecurity;

			var has = basket?.Contains(ServicesRegistry.TrySecurityProvider, item.Order.Security) ?? item.Order.Security == sec;

			if (has)
				Activate(item);
		}

		protected override void DisposeManaged()
		{
			Provider.NewOrderLogItem -= OnNewOrderLogItem;
			base.DisposeManaged();
		}
	}

	[Obsolete]
	private class SecurityLastTradeRule : SecurityRule<Security>
	{
		private readonly Func<Security, bool> _condition;

		public SecurityLastTradeRule(Security security, IMarketDataProvider provider, Func<Security, bool> condition)
			: base(security, provider)
		{
			_condition = condition ?? throw new ArgumentNullException(nameof(condition));

			Name = LocalizedStrings.LastTrade + " " + security;

			Provider.SecurityChanged += OnSecurityChanged;
			Provider.NewTrade += OnNewTrade;
		}

		private void OnSecurityChanged(Security security)
		{
			if (CheckLastTrade(security))
				Activate(security);
		}

		private bool CheckLastTrade(Security security)
		{
			if (Security is BasketSecurity basket)
			{
				return basket.Contains(ServicesRegistry.TrySecurityProvider, security) && _condition(security);
			}
			else
			{
				return security == Security && _condition(Security);
			}
		}

		private void OnNewTrade(Trade trade)
		{
			if (CheckTrades(Security, trade))
				Activate(trade.Security);
		}

		private bool CheckTrades(Security security, Trade trade)
		{
			return security is BasketSecurity basket
				? basket.Contains(ServicesRegistry.TrySecurityProvider, trade.Security) && _condition(trade.Security)
				: trade.Security == security && _condition(trade.Security);
		}

		protected override void DisposeManaged()
		{
			Provider.NewTrade -= OnNewTrade;
			Provider.SecurityChanged -= OnSecurityChanged;

			base.DisposeManaged();
		}
	}

	[Obsolete]
	private class SecurityMarketDepthChangedRule : SecurityRule<MarketDepth>
	{
		public SecurityMarketDepthChangedRule(Security security, IMarketDataProvider provider)
			: base(security, provider)
		{
			Name = LocalizedStrings.MarketDepth + " " + security;
			Provider.MarketDepthChanged += OnMarketDepthChanged;
		}

		private void OnMarketDepthChanged(MarketDepth depth)
		{
			if (depth.Security != Security)
				return;

			Activate(depth);
		}

		protected override void DisposeManaged()
		{
			Provider.MarketDepthChanged -= OnMarketDepthChanged;
			base.DisposeManaged();
		}
	}

	[Obsolete]
	private class BasketSecurityMarketDepthChangedRule : SecurityRule<MarketDepth>
	{
		public BasketSecurityMarketDepthChangedRule(BasketSecurity security, IMarketDataProvider provider)
			: base(security, provider)
		{
			Name = LocalizedStrings.MarketDepth + " " + security;
			Provider.MarketDepthChanged += OnMarketDepthChanged;
		}

		private void OnMarketDepthChanged(MarketDepth depth)
		{
			if (CheckDepth(Security, depth))
				Activate(depth);
		}

		private static bool CheckDepth(Security security, MarketDepth depth)
		{
			var basket = security as BasketSecurity;

			return basket?.Contains(ServicesRegistry.TrySecurityProvider, depth.Security) ?? depth.Security == security;
		}

		protected override void DisposeManaged()
		{
			Provider.MarketDepthChanged -= OnMarketDepthChanged;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// To create a rule for the instrument change event.
	/// </summary>
	/// <param name="security">The instrument to be traced for changes.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenLevel1Received method.")]
	public static MarketRule<Security, Security> WhenChanged(this Security security, IMarketDataProvider provider)
	{
		return new SecurityChangedRule(security, provider);
	}

	/// <summary>
	/// To create a rule for the event of new trade occurrence for the instrument.
	/// </summary>
	/// <param name="security">The instrument to be traced for new trade occurrence event.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenTickTradeReceived method.")]
	public static MarketRule<Security, Trade> WhenNewTrade(this Security security, IMarketDataProvider provider)
	{
		return new SecurityNewTradeRule(security, provider);
	}

	/// <summary>
	/// To create a rule for the event of new notes occurrence in the orders log for instrument.
	/// </summary>
	/// <param name="security">The instrument to be traced for the event of new notes occurrence in the orders log.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderLogReceived method.")]
	public static MarketRule<Security, OrderLogItem> WhenNewOrderLogItem(this Security security, IMarketDataProvider provider)
	{
		return new SecurityNewOrderLogItemRule(security, provider);
	}

	/// <summary>
	/// To create a rule for the event of order book change by instrument.
	/// </summary>
	/// <param name="security">The instrument to be traced for the event of order book change by instrument.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderBookReceived method.")]
	public static MarketRule<Security, MarketDepth> WhenMarketDepthChanged(this Security security, IMarketDataProvider provider)
	{
		return new SecurityMarketDepthChangedRule(security, provider);
	}

	/// <summary>
	/// To create a rule for the event of order book change by instruments basket.
	/// </summary>
	/// <param name="security">Instruments basket to be traced for the event of order books change by internal instruments.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderBookReceived method.")]
	public static MarketRule<Security, MarketDepth> WhenMarketDepthChanged(this BasketSecurity security, IMarketDataProvider provider)
	{
		return new BasketSecurityMarketDepthChangedRule(security, provider);
	}

	/// <summary>
	/// To create a rule for the event of excess of the best bid of specific level.
	/// </summary>
	/// <param name="security">The instrument to be traced for the event of excess of the best bid of specific level.</param>
	/// <param name="provider">The market data provider.</param>
	/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenTickTradeReceived method.")]
	public static MarketRule<Security, Security> WhenBestBidPriceMore(this Security security, IMarketDataProvider provider, Unit price)
	{
		return CreateSecurityCondition(security, provider, Level1Fields.BestBidPrice, price, false);
	}

	/// <summary>
	/// To create a rule for the event of dropping the best bid below the specific level.
	/// </summary>
	/// <param name="security">The instrument to be traced for the event of dropping the best bid below the specific level.</param>
	/// <param name="provider">The market data provider.</param>
	/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenTickTradeReceived method.")]
	public static MarketRule<Security, Security> WhenBestBidPriceLess(this Security security, IMarketDataProvider provider, Unit price)
	{
		return CreateSecurityCondition(security, provider, Level1Fields.BestBidPrice, price, true);
	}

	/// <summary>
	/// To create a rule for the event of excess of the best offer of the specific level.
	/// </summary>
	/// <param name="security">The instrument to be traced for the event of excess of the best offer of the specific level.</param>
	/// <param name="provider">The market data provider.</param>
	/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenTickTradeReceived method.")]
	public static MarketRule<Security, Security> WhenBestAskPriceMore(this Security security, IMarketDataProvider provider, Unit price)
	{
		return CreateSecurityCondition(security, provider, Level1Fields.BestAskPrice, price, false);
	}

	/// <summary>
	/// To create a rule for the event of dropping the best offer below the specific level.
	/// </summary>
	/// <param name="security">The instrument to be traced for the event of dropping the best offer below the specific level.</param>
	/// <param name="provider">The market data provider.</param>
	/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenTickTradeReceived method.")]
	public static MarketRule<Security, Security> WhenBestAskPriceLess(this Security security, IMarketDataProvider provider, Unit price)
	{
		return CreateSecurityCondition(security, provider, Level1Fields.BestAskPrice, price, true);
	}

	/// <summary>
	/// To create a rule for the event of increase of the last trade price above the specific level.
	/// </summary>
	/// <param name="security">The instrument to be traced for the event of increase of the last trade price above the specific level.</param>
	/// <param name="provider">The market data provider.</param>
	/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenTickTradeReceived method.")]
	public static MarketRule<Security, Security> WhenLastTradePriceMore(this Security security, IMarketDataProvider provider, Unit price)
	{
		return CreateLastTradeCondition(security, provider, price, false);
	}

	/// <summary>
	/// To create a rule for the event of reduction of the last trade price below the specific level.
	/// </summary>
	/// <param name="security">The instrument to be traced for the event of reduction of the last trade price below the specific level.</param>
	/// <param name="provider">The market data provider.</param>
	/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenTickTradeReceived method.")]
	public static MarketRule<Security, Security> WhenLastTradePriceLess(this Security security, IMarketDataProvider provider, Unit price)
	{
		return CreateLastTradeCondition(security, provider, price, true);
	}

	[Obsolete]
	private static SecurityChangedRule CreateSecurityCondition(Security security, IMarketDataProvider provider, Level1Fields field, Unit offset, bool isLess)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (offset == null)
			throw new ArgumentNullException(nameof(offset));

		if (offset <= 0)
			throw new ArgumentOutOfRangeException(nameof(offset), offset, LocalizedStrings.InvalidValue);

		var price = (decimal?)provider.GetSecurityValue(security, field);

		if (price == null && offset.Type != UnitTypes.Limit)
			throw new InvalidOperationException(LocalizedStrings.QuoteMissed);

		if (isLess)
		{
			var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price - offset);
			return new SecurityChangedRule(security, provider, s =>
			{
				var quote = (decimal?)provider.GetSecurityValue(s, field);
				return quote != null && quote < finishPrice;
			});
		}
		else
		{
			var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price + offset);
			return new SecurityChangedRule(security, provider, s =>
			{
				var quote = (decimal?)provider.GetSecurityValue(s, field);
				return quote != null && quote > finishPrice;
			});
		}
	}

	[Obsolete]
	private static SecurityLastTradeRule CreateLastTradeCondition(Security security, IMarketDataProvider provider, Unit offset, bool isLess)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (provider == null)
			throw new ArgumentNullException(nameof(provider));

		if (offset == null)
			throw new ArgumentNullException(nameof(offset));

		if (offset <= 0)
			throw new ArgumentOutOfRangeException(nameof(offset), offset, LocalizedStrings.InvalidValue);

		var price = (decimal?)provider.GetSecurityValue(security, Level1Fields.LastTradePrice);

		if (price == null && offset.Type != UnitTypes.Limit)
			throw new ArgumentException(LocalizedStrings.NoInfoAboutLastTrade, nameof(security));

		if (isLess)
		{
			var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price - offset);
			return new SecurityLastTradeRule(security, provider, s => (decimal?)provider.GetSecurityValue(s, Level1Fields.LastTradePrice) < finishPrice);
		}
		else
		{
			var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price + offset);
			return new SecurityLastTradeRule(security, provider, s => (decimal?)provider.GetSecurityValue(s, Level1Fields.LastTradePrice) > finishPrice);
		}
	}

	[Obsolete]
	private abstract class MarketDepthRule : MarketRule<MarketDepth, MarketDepth>
	{
		protected MarketDepthRule(MarketDepth depth)
			: base(depth)
		{
			Depth = depth ?? throw new ArgumentNullException(nameof(depth));
		}

		protected MarketDepth Depth { get; }
	}

	[Obsolete]
	private class MarketDepthChangedRule : MarketDepthRule
	{
		private readonly Func<MarketDepth, bool> _condition;
		private readonly IMarketDataProvider _provider;

		public MarketDepthChangedRule(MarketDepth depth, IMarketDataProvider provider)
			: this(depth, provider, d => true)
		{
		}

		public MarketDepthChangedRule(MarketDepth depth, IMarketDataProvider provider, Func<MarketDepth, bool> condition)
			: base(depth)
		{
			_condition = condition ?? throw new ArgumentNullException(nameof(condition));

			Name = LocalizedStrings.MarketDepth + " " + depth.Security;

			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
			_provider.MarketDepthChanged += ProviderOnMarketDepthChanged;
		}

		private void ProviderOnMarketDepthChanged(MarketDepth depth)
		{
			if (Depth == depth)
				OnQuotesChanged();
		}

		private void OnQuotesChanged()
		{
			if (_condition(Depth))
				Activate(Depth);
		}

		protected override void DisposeManaged()
		{
			_provider.MarketDepthChanged -= ProviderOnMarketDepthChanged;

			base.DisposeManaged();
		}
	}

	/// <summary>
	/// To create a rule for the order book change event.
	/// </summary>
	/// <param name="depth">The order book to be traced for change event.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderBookReceived method.")]
	public static MarketRule<MarketDepth, MarketDepth> WhenChanged(this MarketDepth depth, IMarketDataProvider provider)
	{
		return new MarketDepthChangedRule(depth, provider);
	}

	/// <summary>
	/// To create a rule for the event of order book spread size increase on a specific value.
	/// </summary>
	/// <param name="depth">The order book to be traced for the spread change event.</param>
	/// <param name="price">The shift value.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderBookReceived method.")]
	public static MarketRule<MarketDepth, MarketDepth> WhenSpreadMore(this MarketDepth depth, Unit price, IMarketDataProvider provider)
	{
		var pair = depth.BestPair;
		var firstPrice = pair?.SpreadPrice ?? 0;
		return new MarketDepthChangedRule(depth, provider, d => d.BestPair != null && d.BestPair.SpreadPrice > (firstPrice + price))
		{
			Name = $"{depth.Security} spread > {price}"
		};
	}

	/// <summary>
	/// To create a rule for the event of order book spread size decrease on a specific value.
	/// </summary>
	/// <param name="depth">The order book to be traced for the spread change event.</param>
	/// <param name="price">The shift value.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderBookReceived method.")]
	public static MarketRule<MarketDepth, MarketDepth> WhenSpreadLess(this MarketDepth depth, Unit price, IMarketDataProvider provider)
	{
		var pair = depth.BestPair;
		var firstPrice = pair?.SpreadPrice ?? 0;
		return new MarketDepthChangedRule(depth, provider, d => d.BestPair != null && d.BestPair.SpreadPrice < (firstPrice - price))
		{
			Name = $"{depth.Security} spread < {price}"
		};
	}

	/// <summary>
	/// To create a rule for the event of the best bid increase on a specific value.
	/// </summary>
	/// <param name="depth">The order book to be traced for the event of the best bid increase on a specific value.</param>
	/// <param name="price">The shift value.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderBookReceived method.")]
	public static MarketRule<MarketDepth, MarketDepth> WhenBestBidPriceMore(this MarketDepth depth, Unit price, IMarketDataProvider provider)
	{
		return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestBid, false))
		{
			Name = $"{depth.Security} {LocalizedStrings.BestBid} > {price}"
		};
	}

	/// <summary>
	/// To create a rule for the event of the best bid decrease on a specific value.
	/// </summary>
	/// <param name="depth">The order book to be traced for the event of the best bid decrease on a specific value.</param>
	/// <param name="price">The shift value.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderBookReceived method.")]
	public static MarketRule<MarketDepth, MarketDepth> WhenBestBidPriceLess(this MarketDepth depth, Unit price, IMarketDataProvider provider)
	{
		return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestBid, true))
		{
			Name = $"{depth.Security} {LocalizedStrings.BestBid} < {price}"
		};
	}

	/// <summary>
	/// To create a rule for the event of the best offer increase on a specific value.
	/// </summary>
	/// <param name="depth">The order book to be traced for the event of the best offer increase on a specific value.</param>
	/// <param name="price">The shift value.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderBookReceived method.")]
	public static MarketRule<MarketDepth, MarketDepth> WhenBestAskPriceMore(this MarketDepth depth, Unit price, IMarketDataProvider provider)
	{
		return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestAsk, false))
		{
			Name = $"{depth.Security} {LocalizedStrings.BestAsk} > {price}"
		};
	}

	/// <summary>
	/// To create a rule for the event of the best offer decrease on a specific value.
	/// </summary>
	/// <param name="depth">The order book to be traced for the event of the best offer decrease on a specific value.</param>
	/// <param name="price">The shift value.</param>
	/// <param name="provider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderBookReceived method.")]
	public static MarketRule<MarketDepth, MarketDepth> WhenBestAskPriceLess(this MarketDepth depth, Unit price, IMarketDataProvider provider)
	{
		return new MarketDepthChangedRule(depth, provider, CreateDepthCondition(price, () => depth.BestAsk, true))
		{
			Name = $"{depth.Security} {LocalizedStrings.BestAsk} < {price}"
		};
	}

	[Obsolete]
	private static Func<MarketDepth, bool> CreateDepthCondition(Unit price, Func<QuoteChange?> currentQuote, bool isLess)
	{
		if (price == null)
			throw new ArgumentNullException(nameof(price));

		if (currentQuote == null)
			throw new ArgumentNullException(nameof(currentQuote));

		if (price <= 0)
			throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.InvalidValue);

		var q = currentQuote();
		if (q == null)
			throw new ArgumentException(LocalizedStrings.QuoteMissed, nameof(currentQuote));

		var curQuote = q.Value;

		if (isLess)
		{
			var finishPrice = (decimal)(curQuote.Price - price);
			return depth =>
			{
				var quote = currentQuote();
				return quote != null && quote.Value.Price < finishPrice;
			};
		}
		else
		{
			var finishPrice = (decimal)(curQuote.Price + price);
			return depth =>
			{
				var quote = currentQuote();
				return quote != null && quote.Value.Price > finishPrice;
			};
		}
	}

	[Obsolete]
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
	[Obsolete("Use WhenOrderBookReceived method.")]
	public static MarketRule<Subscription, MarketDepth> WhenMarketDepthReceived(this Subscription subscription, ISubscriptionProvider provider)
	{
		return new MarketDepthReceivedRule(subscription, provider);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use overrloding with Subscription arg type.")]
	public static MarketRule<Subscription, Candle> WhenCandlesStarted(this ISubscriptionProvider subscriptionProvider, CandleSeries candleSeries)
		=> WhenCandlesStarted<Candle>(subscriptionProvider, GetSubscription(subscriptionProvider, candleSeries));

	/// <summary>
	/// </summary>
	[Obsolete("Use overrloding with Subscription arg type.")]
	public static MarketRule<Subscription, Candle> WhenCandlesChanged(this ISubscriptionProvider subscriptionProvider, CandleSeries candleSeries)
		=> WhenCandlesChanged<Candle>(subscriptionProvider, GetSubscription(subscriptionProvider, candleSeries));

	/// <summary>
	/// </summary>
	[Obsolete("Use overrloding with Subscription arg type.")]
	public static MarketRule<Subscription, Candle> WhenCandlesFinished(this ISubscriptionProvider subscriptionProvider, CandleSeries candleSeries)
		=> WhenCandlesFinished<Candle>(subscriptionProvider, GetSubscription(subscriptionProvider, candleSeries));

	/// <summary>
	/// Backward compatibility.
	/// </summary>
	[Obsolete("Use ICandleMessage.")]
	public static MarketRule<TToken, ICandleMessage> Do<TToken>(this MarketRule<TToken, ICandleMessage> rule, Action<Candle> action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		return rule.Do((ICandleMessage msg) => action((Candle)msg));
	}

	/// <summary>
	/// Backward compatibility.
	/// </summary>
	[Obsolete("Use ICandleMessage.")]
	public static MarketRule<TToken, ICandleMessage> Do<TToken, TResult>(this MarketRule<TToken, ICandleMessage> rule, Func<Candle, TResult> action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		return rule.Do((ICandleMessage msg) => action((Candle)msg));
	}

	[Obsolete]
	private class OrderTakeProfitStopLossRule : OrderRule<Order>
	{
		private readonly Unit _offset;
		private readonly bool _isTake;
		private readonly IMarketDataProvider _marketDataProvider;
		private decimal? _bestBidPrice;
		private decimal? _bestAskPrice;
		private decimal _averagePrice;

		private readonly List<Tuple<decimal, decimal>> _trades = [];

		public OrderTakeProfitStopLossRule(Order order, Unit offset, bool isTake, ITransactionProvider transactionProvider, IMarketDataProvider marketDataProvider)
			: base(order, transactionProvider)
		{
			if (offset == null)
				throw new ArgumentNullException(nameof(offset));

			if (offset.Value <= 0)
				throw new ArgumentOutOfRangeException(nameof(offset));

			_offset = offset;
			_isTake = isTake;
			_marketDataProvider = marketDataProvider ?? throw new ArgumentNullException(nameof(marketDataProvider));

			Name = _isTake ? LocalizedStrings.TakeProfit : LocalizedStrings.StopLoss;

			TrySubscribe();
		}

		protected override void Subscribe()
		{
			_marketDataProvider.MarketDepthChanged += OnMarketDepthChanged;
			Provider.NewMyTrade += OnNewMyTrade;
		}

		protected override void UnSubscribe()
		{
			_marketDataProvider.MarketDepthChanged -= OnMarketDepthChanged;
			Provider.NewMyTrade -= OnNewMyTrade;
		}

		private void OnMarketDepthChanged(MarketDepth depth)
		{
			if (depth.Security != Order.Security)
				return;

			_bestBidPrice = depth.BestBid?.Price;
			_bestAskPrice = depth.BestAsk?.Price;

			TryActivate();
		}

		private void OnNewMyTrade(MyTrade trade)
		{
			if (trade.Order != Order)
				return;

			_trades.Add(Tuple.Create(trade.Trade.Price, trade.Trade.Volume));

			var numerator = 0m;
			var denominator = 0m;

			foreach (var t in _trades)
			{
				if (t.Item2 == 0)
					continue;

				numerator += t.Item1 * t.Item2;
				denominator += t.Item2;
			}

			if (denominator == 0)
				return;

			_averagePrice = numerator / denominator;

			TryActivate();
		}

		private void TryActivate()
		{
			if (_trades.Count == 0)
				return;

			bool isActivate;

			if (_isTake)
			{
				if (Order.Side == Sides.Buy)
					isActivate = _bestAskPrice != null && _bestAskPrice.Value >= (_averagePrice + _offset);
				else
					isActivate = _bestBidPrice != null && _bestBidPrice.Value <= (_averagePrice - _offset);
			}
			else
			{
				if (Order.Side == Sides.Buy)
					isActivate = _bestAskPrice != null && _bestAskPrice.Value <= (_averagePrice - _offset);
				else
					isActivate = _bestBidPrice != null && _bestBidPrice.Value >= (_averagePrice + _offset);
			}

			if (isActivate)
				Activate(Order);
		}
	}

	/// <summary>
	/// To create a rule for the order's profit more on offset.
	/// </summary>
	/// <param name="order">The order to be traced for profit.</param>
	/// <param name="profitOffset">Profit offset.</param>
	/// <param name="connector">Connection to the trading system.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderChanged method.")]
	public static MarketRule<Order, Order> WhenProfitMore(this Order order, Unit profitOffset, IConnector connector)
	{
		return WhenProfitMore(order, profitOffset, connector, connector);
	}

	/// <summary>
	/// To create a rule for the order's profit more on offset.
	/// </summary>
	/// <param name="order">The order to be traced for profit.</param>
	/// <param name="profitOffset">Profit offset.</param>
	/// <param name="transactionProvider">The transactional provider.</param>
	/// <param name="marketDataProvider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderChanged method.")]
	public static MarketRule<Order, Order> WhenProfitMore(this Order order, Unit profitOffset, ITransactionProvider transactionProvider, IMarketDataProvider marketDataProvider)
	{
		return new OrderTakeProfitStopLossRule(order, profitOffset, true, transactionProvider, marketDataProvider);
	}

	/// <summary>
	/// To create a rule for the order's loss more on offset.
	/// </summary>
	/// <param name="order">The order to be traced for loss.</param>
	/// <param name="profitOffset">Loss offset.</param>
	/// <param name="connector">Connection to the trading system.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderChanged method.")]
	public static MarketRule<Order, Order> WhenLossMore(this Order order, Unit profitOffset, IConnector connector)
	{
		return WhenLossMore(order, profitOffset, connector, connector);
	}

	/// <summary>
	/// To create a rule for the order's loss more on offset.
	/// </summary>
	/// <param name="order">The order to be traced for loss.</param>
	/// <param name="profitOffset">Loss offset.</param>
	/// <param name="transactionProvider">The transactional provider.</param>
	/// <param name="marketDataProvider">The market data provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderChanged method.")]
	public static MarketRule<Order, Order> WhenLossMore(this Order order, Unit profitOffset, ITransactionProvider transactionProvider, IMarketDataProvider marketDataProvider)
	{
		return new OrderTakeProfitStopLossRule(order, profitOffset, false, transactionProvider, marketDataProvider);
	}
}
