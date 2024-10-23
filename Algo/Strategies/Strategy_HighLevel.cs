﻿namespace StockSharp.Algo.Strategies;

using System.Drawing;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies.Protective;
using StockSharp.Charting;

public partial class Strategy
{
	/// <summary>
	/// To create initialized object of buy order at market price.
	/// </summary>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order BuyMarket(decimal? volume = null)
	{
		var order = this.CreateOrder(Sides.Buy, default, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// To create the initialized order object of sell order at market price.
	/// </summary>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order SellMarket(decimal? volume = null)
	{
		var order = this.CreateOrder(Sides.Sell, default, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// To create the initialized order object for buy.
	/// </summary>
	/// <param name="price">Price.</param>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order BuyLimit(decimal price, decimal? volume = null)
	{
		var order = this.CreateOrder(Sides.Buy, price, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// To create the initialized order object for sell.
	/// </summary>
	/// <param name="price">Price.</param>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order SellLimit(decimal price, decimal? volume = null)
	{
		var order = this.CreateOrder(Sides.Sell, price, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// To close open position by market (to register the order of the type <see cref="OrderTypes.Market"/>).
	/// </summary>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If not passed the value from <see cref="Strategy.Security"/> will be obtain.</param>
	/// <param name="portfolio"><see cref="BusinessEntities.Portfolio"/>. If not passed the value from <see cref="Strategy.Portfolio"/> will be obtain.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The market order is not operable on all exchanges.
	/// </remarks>
	public Order ClosePosition(Security security = default, Portfolio portfolio = default)
	{
		var position = security is null ? Position : GetPositionValue(security, portfolio) ?? default;
		
		if (position == 0)
			return null;

		var volume = position.Abs();

		return position > 0 ? SellMarket(volume) : BuyMarket(volume);
	}

	/// <summary>
	/// Subscription handler.
	/// </summary>
	/// <typeparam name="T">Market-data type.</typeparam>
	protected class SubscriptionHandler<T>
	{
		/// <summary>
		/// Subscription binder with zero indicators.
		/// </summary>
		public class SubscriptionHandlerBinder0
		{
			private readonly SubscriptionHandler<T> _parent;
			private Action<T> _callback;

			internal SubscriptionHandlerBinder0(SubscriptionHandler<T> parent)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			internal void StartSubscription()
				=> _parent.StartSubscription();

			internal SubscriptionHandlerBinder0 SetCallback(Action<T> callback)
			{
				_callback = callback ?? throw new ArgumentNullException(nameof(callback));
				StartSubscription();
				return this;
			}

			internal virtual IEnumerable<IIndicatorValue> Invoke(T typed, DateTimeOffset time)
			{
				_callback(typed);
				return [];
			}
		}

		/// <summary>
		/// Subscription binder with single indicator.
		/// </summary>
		public class SubscriptionHandlerBinder1 : SubscriptionHandlerBinder0
		{
			private Action<T, IIndicatorValue> _callback;

			internal SubscriptionHandlerBinder1(SubscriptionHandler<T> parent, IIndicator indicator1)
				: base(parent)
			{
				Indicator1 = indicator1 ?? throw new ArgumentNullException(nameof(indicator1));

				parent._strategy.Indicators.TryAdd(indicator1);
			}

			/// <summary>
			/// <see cref="IIndicator"/>
			/// </summary>
			protected IIndicator Indicator1 { get; }

			internal SubscriptionHandlerBinder1 SetCallback(Action<T, IIndicatorValue> callback)
			{
				_callback = callback ?? throw new ArgumentNullException(nameof(callback));
				StartSubscription();
				return this;
			}

			internal override IEnumerable<IIndicatorValue> Invoke(T typed, DateTimeOffset time)
			{
				var v1 = Indicator1.Process(typed, time, true);

				_callback(typed, v1);

				return [v1];
			}
		}

		/// <summary>
		/// Subscription binder with two indicators.
		/// </summary>
		public class SubscriptionHandlerBinder2 : SubscriptionHandlerBinder1
		{
			private Action<T, IIndicatorValue, IIndicatorValue> _callback;

			internal SubscriptionHandlerBinder2(SubscriptionHandler<T> parent, IIndicator indicator1, IIndicator indicator2)
				: base(parent, indicator1)
			{
				Indicator2 = indicator2 ?? throw new ArgumentNullException(nameof(indicator2));

				parent._strategy.Indicators.TryAdd(indicator2);
			}

			/// <summary>
			/// <see cref="IIndicator"/>
			/// </summary>
			protected IIndicator Indicator2 { get; }

			internal SubscriptionHandlerBinder2 SetCallback(Action<T, IIndicatorValue, IIndicatorValue> callback)
			{
				_callback = callback ?? throw new ArgumentNullException(nameof(callback));
				StartSubscription();
				return this;
			}

			internal override IEnumerable<IIndicatorValue> Invoke(T typed, DateTimeOffset time)
			{
				var v1 = Indicator1.Process(typed, time, true);
				var v2 = Indicator2.Process(typed, time, true);

				_callback(typed, v1, v2);

				return [v1, v2];
			}
		}

		/// <summary>
		/// Subscription binder with three indicators.
		/// </summary>
		public class SubscriptionHandlerBinder3 : SubscriptionHandlerBinder2
		{
			private Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue> _callback;

			internal SubscriptionHandlerBinder3(SubscriptionHandler<T> parent, IIndicator indicator1, IIndicator indicator2, IIndicator indicator3)
				: base(parent, indicator1, indicator2)
			{
				Indicator3 = indicator3 ?? throw new ArgumentNullException(nameof(indicator3));

				parent._strategy.Indicators.TryAdd(indicator3);
			}

			/// <summary>
			/// <see cref="IIndicator"/>
			/// </summary>
			protected IIndicator Indicator3 { get; }

			internal SubscriptionHandlerBinder3 SetCallback(Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback)
			{
				_callback = callback ?? throw new ArgumentNullException(nameof(callback));
				StartSubscription();
				return this;
			}

			internal override IEnumerable<IIndicatorValue> Invoke(T typed, DateTimeOffset time)
			{
				var v1 = Indicator1.Process(typed, time, true);
				var v2 = Indicator2.Process(typed, time, true);
				var v3 = Indicator3.Process(typed, time, true);

				_callback(typed, v1, v2, v3);

				return [v1, v2, v3];
			}
		}

		private readonly Strategy _strategy;
		private readonly CachedSynchronizedList<SubscriptionHandlerBinder0> _binders = [];

		internal SubscriptionHandler(Strategy strategy, Subscription subscription)
        {
			_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
			Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
		}

		/// <summary>
		/// <see cref="Subscription"/>.
		/// </summary>
		public Subscription Subscription { get; }

		private void StartSubscription()
		{
			var type = typeof(T);

			void tryActivateProtection(decimal price, DateTimeOffset time)
			{
				var info = _strategy._posController?.TryActivate(price, time);

				if (info is not null)
					_strategy.ActiveProtection(info.Value);
			}

			void handle(object v, DateTimeOffset time, Func<ICandleMessage> getCandle)
			{
				var typed = v.To<T>();

				var indValues = new List<IIndicatorValue>();

				foreach (var binder in _binders.Cache)
					indValues.AddRange(binder.Invoke(typed, time));

				_strategy.DrawFlush(Subscription, getCandle, indValues);
			}

			if (type.Is<ICandleMessage>())
			{
				Subscription
					.WhenCandleReceived(_strategy)
					.Do(v =>
					{
						if (_strategy.ProcessState != ProcessStates.Started)
							return;

						tryActivateProtection(v.ClosePrice, _strategy.CurrentTime);

						handle(v, v.ServerTime, () => v);
					})
					.Apply(_strategy);
			}
			else if (type.Is<ITickTradeMessage>())
			{
				var dt = DataType.Create(typeof(TickCandleMessage), 1);

				Subscription
					.WhenTickTradeReceived(_strategy)
					.Do(v =>
					{
						if (_strategy.ProcessState != ProcessStates.Started)
							return;

						tryActivateProtection(v.Price, _strategy.CurrentTime);

						handle(v, v.ServerTime, () =>	new TickCandleMessage
						{
							DataType = dt,
							OpenTime = v.ServerTime,
							SecurityId = v.SecurityId,
							OpenPrice = v.Price,
							HighPrice = v.Price,
							LowPrice = v.Price,
							ClosePrice = v.Price,
							TotalVolume = v.Volume,
							State = CandleStates.Finished,
						});
					})
					.Apply(_strategy);
			}
			else if (type.Is<IOrderBookMessage>())
			{
				var dt = DataType.Create(typeof(TickCandleMessage), 1);
				var field = Subscription.MarketData?.BuildField ?? Level1Fields.SpreadMiddle;

				Subscription
					.WhenOrderBookReceived(_strategy)
					.Do(v =>
					{
						if (_strategy.ProcessState != ProcessStates.Started)
							return;

						var value = field switch
						{
							Level1Fields.BestBidPrice => v.GetBestBid()?.Price,
							Level1Fields.BestAskPrice => v.GetBestAsk()?.Price,
							Level1Fields.SpreadMiddle => v.GetSpreadMiddle(null),
							_ => throw new ArgumentOutOfRangeException(field.To<string>()),
						};

						if (value is not decimal price)
							return;

						tryActivateProtection(price, _strategy.CurrentTime);

						handle(v, v.ServerTime, () => new TickCandleMessage
						{
							DataType = dt,
							OpenTime = v.ServerTime,
							SecurityId = v.SecurityId,
							OpenPrice = price,
							HighPrice = price,
							LowPrice = price,
							ClosePrice = price,
							State = CandleStates.Finished,
						});
					})
					.Apply(_strategy);
			}
			else if (type.Is<Level1ChangeMessage>())
			{
				var dt = DataType.Create(typeof(TickCandleMessage), 1);
				var field = Subscription.MarketData?.BuildField ?? Level1Fields.LastTradePrice;

				Subscription
					.WhenLevel1Received(_strategy)
					.Do(v =>
					{
						if (_strategy.ProcessState != ProcessStates.Started)
							return;

						if (v.TryGet(field) is not decimal price)
							return;

						tryActivateProtection(price, _strategy.CurrentTime);

						handle(v, v.ServerTime, () => new TickCandleMessage
						{
							DataType = dt,
							OpenTime = v.ServerTime,
							SecurityId = v.SecurityId,
							OpenPrice = price,
							HighPrice = price,
							LowPrice = price,
							ClosePrice = price,
							State = CandleStates.Finished,
						});
					})
					.Apply(_strategy);
			}
			else
			{
				throw new NotSupportedException(LocalizedStrings.UnsupportedType.Put(type));
			}
		}

		/// <summary>
		/// Start subscription.
		/// </summary>
		/// <returns><see cref="SubscriptionHandler{T}"/></returns>
		public SubscriptionHandler<T> Start()
		{
			_strategy.Subscribe(Subscription);
			return this;
		}

		/// <summary>
		/// Bind the subscription.
		/// </summary>
		/// <param name="callback">Callback.</param>
		/// <returns><see cref="SubscriptionHandler{T}"/></returns>
		public SubscriptionHandler<T> Bind(Action<T> callback)
		{
			_binders.Add(new SubscriptionHandlerBinder0(this).SetCallback(callback));
			return this;
		}

		/// <summary>
		/// Bind indicator to the subscription.
		/// </summary>
		/// <param name="indicator">Indicator.</param>
		/// <param name="callback">Callback.</param>
		/// <returns><see cref="SubscriptionHandler{T}"/></returns>
		public SubscriptionHandler<T> Bind(IIndicator indicator, Action<T, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			return Bind(indicator, (v, iv) => callback(v, iv.ToDecimal()));
		}

		/// <summary>
		/// Bind indicator to the subscription.
		/// </summary>
		/// <param name="indicator">Indicator.</param>
		/// <param name="callback">Callback.</param>
		/// <returns><see cref="SubscriptionHandler{T}"/></returns>
		public SubscriptionHandler<T> Bind(IIndicator indicator, Action<T, IIndicatorValue> callback)
		{
			_binders.Add(new SubscriptionHandlerBinder1(this, indicator).SetCallback(callback));
			return this;
		}

		/// <summary>
		/// Bind indicator to the subscription.
		/// </summary>
		/// <param name="indicator1">Indicator.</param>
		/// <param name="indicator2">Indicator.</param>
		/// <param name="callback">Callback.</param>
		/// <returns><see cref="SubscriptionHandler{T}"/></returns>
		public SubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, Action<T, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			return Bind(indicator1, indicator2, (v, iv1, iv2) => callback(v, iv1.ToDecimal(), iv2.ToDecimal()));
		}

		/// <summary>
		/// Bind indicators to the subscription.
		/// </summary>
		/// <param name="indicator1">Indicator.</param>
		/// <param name="indicator2">Indicator.</param>
		/// <param name="callback">Callback.</param>
		/// <returns><see cref="SubscriptionHandler{T}"/></returns>
		public SubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, Action<T, IIndicatorValue, IIndicatorValue> callback)
		{
			_binders.Add(new SubscriptionHandlerBinder2(this, indicator1, indicator2).SetCallback(callback));
			return this;
		}

		/// <summary>
		/// Bind indicator to the subscription.
		/// </summary>
		/// <param name="indicator1">Indicator.</param>
		/// <param name="indicator2">Indicator.</param>
		/// <param name="indicator3">Indicator.</param>
		/// <param name="callback">Callback.</param>
		/// <returns><see cref="SubscriptionHandler{T}"/></returns>
		public SubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, decimal, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			return Bind(indicator1, indicator2, indicator3, (v, iv1, iv2, iv3) => callback(v, iv1.ToDecimal(), iv2.ToDecimal(), iv3.ToDecimal()));
		}

		/// <summary>
		/// Bind indicators to the subscription.
		/// </summary>
		/// <param name="indicator1">Indicator.</param>
		/// <param name="indicator2">Indicator.</param>
		/// <param name="indicator3">Indicator.</param>
		/// <param name="callback">Callback.</param>
		/// <returns><see cref="SubscriptionHandler{T}"/></returns>
		public SubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback)
		{
			_binders.Add(new SubscriptionHandlerBinder3(this, indicator1, indicator2, indicator3).SetCallback(callback));
			return this;
		}
	}

	private void ActiveProtection((bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition) info)
	{
		// sending protection (=closing position) order as regular order
		RegisterOrder(this.CreateOrder(info.side, info.price, info.volume));
	}

	/// <summary>
	/// Subscribe to candles.
	/// </summary>
	/// <param name="tf">Time-frame.</param>
	/// <param name="isFinishedOnly"><see cref="MarketDataMessage.IsFinishedOnly"/></param>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected SubscriptionHandler<ICandleMessage> SubscribeCandles(TimeSpan tf, bool isFinishedOnly = true, Security security = default)
		=> SubscribeCandles(DataType.TimeFrame(tf), isFinishedOnly, security);

	/// <summary>
	/// Subscribe to candles.
	/// </summary>
	/// <param name="dt"><see cref="DataType"/></param>
	/// <param name="isFinishedOnly"><see cref="MarketDataMessage.IsFinishedOnly"/></param>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected SubscriptionHandler<ICandleMessage> SubscribeCandles(DataType dt, bool isFinishedOnly = true, Security security = default)
		=> SubscribeCandles(new(dt, security ?? Security)
		{
			MarketData =
			{
				IsFinishedOnly = isFinishedOnly,
			}
		});

	/// <summary>
	/// Subscribe to candles.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected SubscriptionHandler<ICandleMessage> SubscribeCandles(Subscription subscription)
		=> new(this, subscription);

	/// <summary>
	/// Subscribe to <see cref="DataType.Ticks"/>.
	/// </summary>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected SubscriptionHandler<ITickTradeMessage> SubscribeTicks(Security security = null)
		=> SubscribeTicks(new Subscription(DataType.Ticks, security ?? Security));

	/// <summary>
	/// Subscribe to <see cref="DataType.Ticks"/>.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected SubscriptionHandler<ITickTradeMessage> SubscribeTicks(Subscription subscription)
		=> new(this, subscription);

	/// <summary>
	/// Subscribe to <see cref="DataType.Level1"/>.
	/// </summary>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected SubscriptionHandler<Level1ChangeMessage> SubscribeLevel1(Security security = null)
		=> SubscribeLevel1(new Subscription(DataType.Level1, security ?? Security));

	/// <summary>
	/// Subscribe to <see cref="DataType.Level1"/>.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected SubscriptionHandler<Level1ChangeMessage> SubscribeLevel1(Subscription subscription)
		=> new(this, subscription);

	/// <summary>
	/// Subscribe to <see cref="DataType.MarketDepth"/>.
	/// </summary>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected SubscriptionHandler<IOrderBookMessage> SubscribeOrderBook(Security security = null)
		=> SubscribeOrderBook(new Subscription(DataType.MarketDepth, security ?? Security));

	/// <summary>
	/// Subscribe to <see cref="DataType.MarketDepth"/>.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected SubscriptionHandler<IOrderBookMessage> SubscribeOrderBook(Subscription subscription)
		=> new(this, subscription);

	private IChart _chart;
	private SynchronizedList<Order> _drawingOrders;
	private SynchronizedList<MyTrade> _drawingTrades;
	private readonly CachedSynchronizedList<IChartOrderElement> _ordersElems = [];
	private readonly CachedSynchronizedList<IChartTradeElement> _tradesElems = [];
	private readonly SynchronizedDictionary<Subscription, IChartElement> _subscriptionElems = [];
	private readonly SynchronizedDictionary<IIndicator, IChartIndicatorElement> _indElems = [];

	/// <summary>
	/// Create chart area.
	/// </summary>
	/// <returns><see cref="IChartArea"/></returns>
	protected IChartArea CreateChartArea()
	{
		_chart ??= this.GetChart();
		return _chart?.AddArea();
	}

	/// <summary>
	/// Draw candles on chart.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <param name="subscription"><see cref="SubscriptionHandler{T}"/></param>
	/// <returns><see cref="IChartCandleElement"/></returns>
	protected IChartCandleElement DrawCandles<T>(IChartArea area, SubscriptionHandler<T> subscription)
		=> DrawCandles(area, subscription.Subscription);

	/// <summary>
	/// Draw candles on chart.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="IChartCandleElement"/></returns>
	protected IChartCandleElement DrawCandles(IChartArea area, Subscription subscription)
	{
		var elem = area.AddCandles();
		_subscriptionElems.Add(subscription, elem);
		return elem;
	}

	/// <summary>
	/// Draw indicator on chart.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <param name="color"><see cref="IChartIndicatorElement.Color"/></param>
	/// <param name="additionalColor"><see cref="IChartIndicatorElement.AdditionalColor"/></param>
	/// <returns><see cref="IChartIndicatorElement"/></returns>
	protected IChartIndicatorElement DrawIndicator(IChartArea area, IIndicator indicator, Color? color = default, Color? additionalColor = default)
	{
		var elem = area.AddIndicator(indicator);

		if (color is not null)
			elem.Color = color.Value;

		if (additionalColor is not null)
			elem.AdditionalColor = additionalColor.Value;

		_indElems.Add(indicator, elem);

		return elem;
	}

	/// <summary>
	/// Draw trades on chart.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <returns><see cref="IChartTradeElement"/></returns>
	protected IChartTradeElement DrawOwnTrades(IChartArea area)
	{
		var elem = area.AddTrades();
		_drawingTrades = [];
		_tradesElems.Add(elem);
		return elem;
	}

	/// <summary>
	/// Draw orders on chart.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <returns><see cref="IChartOrderElement"/></returns>
	protected IChartOrderElement DrawOrders(IChartArea area)
	{
		var elem = area.AddOrders();
		_drawingOrders = [];
		_ordersElems.Add(elem);
		return elem;
	}

	private void DrawFlush(Subscription subscription, Func<ICandleMessage> getCandle, List<IIndicatorValue> indValues)
	{
		if (subscription is null)	throw new ArgumentNullException(nameof(subscription));
		if (getCandle is null)		throw new ArgumentNullException(nameof(getCandle));
		if (indValues is null)		throw new ArgumentNullException(nameof(indValues));

		var trade = _drawingTrades?.CopyAndClear().FirstOrDefault();
		var order = _drawingOrders?.CopyAndClear().FirstOrDefault();

		if (_chart == null)
			return;

		var data = _chart.CreateData();
		var candle = getCandle();

		var item = data.Group(candle.OpenTime);

		if (_subscriptionElems.TryGetValue(subscription, out var candleElem))
			item.Add(candleElem, candle);

		foreach (var indValue in indValues)
		{
			if (_indElems.TryGetValue(indValue.Indicator, out var indElem))
				item.Add(indElem, indValue);
		}

		if (order is not null)
		{
			foreach (var ordersElem in _ordersElems.Cache)
				item.Add(ordersElem, order);
		}

		if (trade is not null)
		{
			foreach (var tradesElem in _tradesElems.Cache)
				item.Add(tradesElem, trade);
		}

		_chart.Draw(data);
	}

	private Unit _takeProfit, _stopLoss;
	private bool _isStopTrailing;
	private TimeSpan _takeTimeout, _stopTimeout;
	private bool _protectiveUseMarketOrders;
	private ProtectiveController _protectiveController;
	private IProtectivePositionController _posController;

	/// <summary>
	/// Start position protection.
	/// </summary>
	/// <param name="takeProfit">Take offset.</param>
	/// <param name="stopLoss">Stop offset.</param>
	/// <param name="isStopTrailing">Whether to use a trailing technique.</param>
	/// <param name="takeTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="stopTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="useMarketOrders">Whether to use market orders.</param>
	protected void StartProtection(
		Unit takeProfit, Unit stopLoss,
		bool isStopTrailing = default,
		TimeSpan takeTimeout = default,
		TimeSpan stopTimeout = default,
		bool useMarketOrders = default)
	{
		if (!takeProfit.IsSet() && !stopLoss.IsSet())
			return;

		_protectiveController = new();
		_takeProfit = takeProfit;
		_stopLoss = stopLoss;
		_isStopTrailing = isStopTrailing;
		_takeTimeout = takeTimeout;
		_stopTimeout = stopTimeout;
		_protectiveUseMarketOrders = useMarketOrders;
	}
}