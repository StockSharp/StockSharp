namespace StockSharp.Algo.Strategies;

using System.Drawing;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies.Protective;
using StockSharp.Charting;

/// <summary>
/// Subscription handler.
/// </summary>
/// <typeparam name="T">Market-data type.</typeparam>
public interface ISubscriptionHandler<T>
{
	/// <summary>
	/// <see cref="Subscription"/>.
	/// </summary>
	Subscription Subscription { get; }

	/// <summary>
	/// Start subscription.
	/// </summary>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Start();

	/// <summary>
	/// Bind the subscription.
	/// </summary>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(Action<T> callback);

	/// <summary>
	/// Bind indicator to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator, Action<T, decimal?> callback);

	/// <summary>
	/// Bind indicator to the subscription.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator, Action<T, decimal> callback);

	/// <summary>
	/// Bind indicator to the subscription.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if the indicator returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator, Action<T, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, Action<T, decimal?, decimal?> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, Action<T, decimal, decimal> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, Action<T, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, decimal?, decimal?, decimal?> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, decimal, decimal, decimal> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, decimal?, decimal?, decimal?, decimal?> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, decimal, decimal, decimal, decimal> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicator to the subscription.
	/// </summary>
	/// <param name="indicators">Indicators.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator[] indicators, Action<T, decimal[]> callback);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicators">Indicators.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator[] indicators, Action<T, decimal?[]> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicators">Indicators.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator[] indicators, Action<T, IIndicatorValue[]> callback, bool allowEmpty = false);
}

public partial class Strategy
{
	/// <summary>
	/// To create initialized object of buy order at market price.
	/// </summary>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Volume"/> value is used.</param>
	/// <param name="security">The security. If <see langword="null" /> value is passed, then <see cref="Security"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order BuyMarket(decimal? volume = null, Security security = null)
	{
		var order = CreateOrder(Sides.Buy, default, volume);

		if (security != null)
			order.Security = security;

		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// To create the initialized order object of sell order at market price.
	/// </summary>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Volume"/> value is used.</param>
	/// <param name="security">The security. If <see langword="null" /> value is passed, then <see cref="Security"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order SellMarket(decimal? volume = null, Security security = null)
	{
		var order = CreateOrder(Sides.Sell, default, volume);

		if (security != null)
			order.Security = security;

		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// To create the initialized order object for buy.
	/// </summary>
	/// <param name="price">Price.</param>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
	/// <param name="security">The security. If <see langword="null" /> value is passed, then <see cref="Security"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order BuyLimit(decimal price, decimal? volume = null, Security security = null)
	{
		var order = CreateOrder(Sides.Buy, price, volume);

		if (security != null)
			order.Security = security;

		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// To create the initialized order object for sell.
	/// </summary>
	/// <param name="price">Price.</param>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
	/// <param name="security">The security. If <see langword="null" /> value is passed, then <see cref="Security"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order SellLimit(decimal price, decimal? volume = null, Security security = null)
	{
		var order = CreateOrder(Sides.Sell, price, volume);

		if (security != null)
			order.Security = security;

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
	private class SubscriptionHandler<T> : ISubscriptionHandler<T>
	{
		/// <summary>
		/// Subscription binder with zero indicators.
		/// </summary>
		private class SubscriptionHandlerBinder0
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

		private class SubscriptionHandlerBinderArray : SubscriptionHandlerBinder0
		{
			private Action<T, IIndicatorValue[]> _callback;
			private readonly IIndicator[] _indicators;
			private readonly bool _allowEmpty;

			internal SubscriptionHandlerBinderArray(SubscriptionHandler<T> parent, IIndicator[] indicators, bool allowEmpty)
				: base(parent)
			{
				if (indicators == null)
					throw new ArgumentNullException(nameof(indicators));

				if (indicators.Length == 0)
					throw new ArgumentException("Indicators array cannot be empty.", nameof(indicators));

				_indicators = indicators;
				_allowEmpty = allowEmpty;

				foreach (var indicator in _indicators)
				{
					if (indicator == null)
						throw new ArgumentNullException(nameof(indicators), "Indicator cannot be null.");

					parent._strategy.Indicators.TryAdd(indicator);
				}
			}

			internal SubscriptionHandlerBinderArray SetCallback(Action<T, IIndicatorValue[]> callback)
			{
				_callback = callback ?? throw new ArgumentNullException(nameof(callback));
				StartSubscription();
				return this;
			}

			internal override IEnumerable<IIndicatorValue> Invoke(T typed, DateTimeOffset time)
			{
				var hasEmpty = false;

				var values = new IIndicatorValue[_indicators.Length];

				for (var i = 0; i < _indicators.Length; i++)
				{
					values[i] = _indicators[i].Process(typed, time, true);

					if (values[i].IsEmpty)
						hasEmpty = true;
				}

				if (!hasEmpty || _allowEmpty)
					_callback(typed, values);

				return values;
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
				var dt = 1.Tick();

				Subscription
					.WhenTickTradeReceived(_strategy)
					.Do(v =>
					{
						if (_strategy.ProcessState != ProcessStates.Started)
							return;

						tryActivateProtection(v.Price, _strategy.CurrentTime);

						handle(v, v.ServerTime, () => new TickCandleMessage
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
				var dt = 1.Tick();
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
				var dt = 1.Tick();
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

		public ISubscriptionHandler<T> Start()
		{
			_strategy.Subscribe(Subscription);
			return this;
		}

		public ISubscriptionHandler<T> Bind(Action<T> callback)
		{
			_binders.Add(new SubscriptionHandlerBinder0(this).SetCallback(callback));
			return this;
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator, Action<T, decimal?> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicator is null)
				throw new ArgumentNullException(nameof(indicator));

			if (indicator is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator));

			return BindEx(indicator, (v, iv) => callback(v, iv.ToNullableDecimal()), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator, Action<T, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicator is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator));

			return BindEx(indicator, (v, iv) => callback(v, iv.ToDecimal()), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, Action<T, decimal?, decimal?> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicator1 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator1));

			if (indicator2 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator2));

			return BindEx(indicator1, indicator2, (v, iv1, iv2) => callback(v, iv1.ToNullableDecimal(), iv2.ToNullableDecimal()), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, Action<T, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicator1 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator1));

			if (indicator2 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator2));

			return BindEx(indicator1, indicator2, (v, iv1, iv2) => callback(v, iv1.ToDecimal(), iv2.ToDecimal()), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, decimal?, decimal?, decimal?> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicator1 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator1));

			if (indicator2 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator2));

			if (indicator3 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator3));

			return BindEx(indicator1, indicator2, indicator3, (v, iv1, iv2, iv3) => callback(v, iv1.ToNullableDecimal(), iv2.ToNullableDecimal(), iv3.ToNullableDecimal()), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, decimal, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicator1 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator1));

			if (indicator2 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator2));

			if (indicator3 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator3));

			return BindEx(indicator1, indicator2, indicator3, (v, iv1, iv2, iv3) => callback(v, iv1.ToDecimal(), iv2.ToDecimal(), iv3.ToDecimal()), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, decimal?, decimal?, decimal?, decimal?> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicator1 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator1));

			if (indicator2 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator2));

			if (indicator3 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator3));

			if (indicator4 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator4));

			return BindEx(indicator1, indicator2, indicator3, indicator4, (v, iv1, iv2, iv3, iv4) => callback(v, iv1.ToNullableDecimal(), iv2.ToNullableDecimal(), iv3.ToNullableDecimal(), iv4.ToNullableDecimal()), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, decimal, decimal, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicator1 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator1));

			if (indicator2 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator2));

			if (indicator3 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator3));

			if (indicator4 is IComplexIndicator)
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicator4));

			return BindEx(indicator1, indicator2, indicator3, indicator4, (v, iv1, iv2, iv3, iv4) => callback(v, iv1.ToDecimal(), iv2.ToDecimal(), iv3.ToDecimal(), iv4.ToDecimal()), false);
		}

		public ISubscriptionHandler<T> Bind(IIndicator[] indicators, Action<T, decimal[]> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicators is null)
				throw new ArgumentNullException(nameof(indicators));

			if (indicators.Any(i => i is IComplexIndicator))
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicators));

			return BindEx(indicators, (v, ivs) => callback(v, [.. ivs.Select(i => i.ToDecimal())]), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator[] indicators, Action<T, decimal?[]> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicators is null)
				throw new ArgumentNullException(nameof(indicators));

			if (indicators.Any(i => i is IComplexIndicator))
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicators));

			return BindEx(indicators, (v, ivs) => callback(v, [.. ivs.Select(i => i.ToNullableDecimal())]), true);
		}

		public ISubscriptionHandler<T> BindEx(IIndicator indicator, Action<T, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator], (c, v) => callback(c, v[0]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, Action<T, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator1, indicator2], (c, v) => callback(c, v[0], v[1]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator1, indicator2, indicator3], (c, v) => callback(c, v[0], v[1], v[2]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator1, indicator2, indicator3, indicator4], (c, v) => callback(c, v[0], v[1], v[2], v[3]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator[] indicators, Action<T, IIndicatorValue[]> callback, bool allowEmpty)
		{
			_binders.Add(new SubscriptionHandlerBinderArray(this, indicators, allowEmpty).SetCallback(callback));
			return this;
		}
	}

	private void ActiveProtection((bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition) info)
	{
		// sending protection (=closing position) order as regular order
		RegisterOrder(CreateOrder(info.side, info.price, info.volume));
	}

	/// <summary>
	/// Subscribe to candles.
	/// </summary>
	/// <param name="tf">Time-frame.</param>
	/// <param name="isFinishedOnly"><see cref="MarketDataMessage.IsFinishedOnly"/></param>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<ICandleMessage> SubscribeCandles(TimeSpan tf, bool isFinishedOnly = true, Security security = default)
		=> SubscribeCandles(tf.TimeFrame(), isFinishedOnly, security);

	/// <summary>
	/// Subscribe to candles.
	/// </summary>
	/// <param name="dt"><see cref="DataType"/></param>
	/// <param name="isFinishedOnly"><see cref="MarketDataMessage.IsFinishedOnly"/></param>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<ICandleMessage> SubscribeCandles(DataType dt, bool isFinishedOnly = true, Security security = default)
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
	protected ISubscriptionHandler<ICandleMessage> SubscribeCandles(Subscription subscription)
		=> new SubscriptionHandler<ICandleMessage>(this, subscription);

	/// <summary>
	/// Subscribe to <see cref="DataType.Ticks"/>.
	/// </summary>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<ITickTradeMessage> SubscribeTicks(Security security = null)
		=> SubscribeTicks(new Subscription(DataType.Ticks, security ?? Security));

	/// <summary>
	/// Subscribe to <see cref="DataType.Ticks"/>.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<ITickTradeMessage> SubscribeTicks(Subscription subscription)
		=> new SubscriptionHandler<ITickTradeMessage>(this, subscription);

	/// <summary>
	/// Subscribe to <see cref="DataType.Level1"/>.
	/// </summary>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<Level1ChangeMessage> SubscribeLevel1(Security security = null)
		=> SubscribeLevel1(new Subscription(DataType.Level1, security ?? Security));

	/// <summary>
	/// Subscribe to <see cref="DataType.Level1"/>.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<Level1ChangeMessage> SubscribeLevel1(Subscription subscription)
		=> new SubscriptionHandler<Level1ChangeMessage>(this, subscription);

	/// <summary>
	/// Subscribe to <see cref="DataType.MarketDepth"/>.
	/// </summary>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<IOrderBookMessage> SubscribeOrderBook(Security security = null)
		=> SubscribeOrderBook(new Subscription(DataType.MarketDepth, security ?? Security));

	/// <summary>
	/// Subscribe to <see cref="DataType.MarketDepth"/>.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="SubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<IOrderBookMessage> SubscribeOrderBook(Subscription subscription)
		=> new SubscriptionHandler<IOrderBookMessage>(this, subscription);

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
		_chart ??= GetChart();
		return _chart?.AddArea();
	}

	/// <summary>
	/// Draw candles on chart.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <param name="subscription"><see cref="SubscriptionHandler{T}"/></param>
	/// <returns><see cref="IChartCandleElement"/></returns>
	protected IChartCandleElement DrawCandles<T>(IChartArea area, ISubscriptionHandler<T> subscription)
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
		TimeSpan? takeTimeout = default,
		TimeSpan? stopTimeout = default,
		bool useMarketOrders = default)
	{
		if (!takeProfit.IsSet() && !stopLoss.IsSet())
			return;

		if (takeTimeout < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(takeTimeout), takeTimeout, LocalizedStrings.InvalidValue);

		if (stopTimeout < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(stopTimeout), stopTimeout, LocalizedStrings.InvalidValue);

		_protectiveController = new();
		_takeProfit = takeProfit;
		_stopLoss = stopLoss;
		_isStopTrailing = isStopTrailing;
		_takeTimeout = takeTimeout ?? default;
		_stopTimeout = stopTimeout ?? default;
		_protectiveUseMarketOrders = useMarketOrders;
	}
}