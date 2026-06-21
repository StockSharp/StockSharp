namespace StockSharp.Algo.Strategies;

using StockSharp.Alerts;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Testing;

// HighLevelSubscriptions subsystem ported from the monolith StrategyOld onto the decomposed engine.
//
// The public ISubscriptionHandler / ISubscriptionHandler<T> interfaces are NOT redeclared here:
// they are namespace-level types already declared by the monolith (StrategyOld_HighLevel.cs) and
// shared by both strategy implementations. This file only re-implements the high-level data
// subscription helpers, the alert helpers, the indicator tracking list and the timer helpers on
// top of the decomposed infrastructure.
//
// The chart drawing surface (CreateChartArea / DrawCandles / DrawIndicator / DrawOwnTrades /
// DrawOrders and the private DrawFlush used below) belongs to the HighLevelCharting subsystem and
// is implemented in Strategy_HighLevelCharting.cs - it is intentionally not duplicated here.
public partial class Strategy
{
	// The monolith exposed an IndicatorList that also drove IsFormed. Here we keep a minimal tracking
	// collection used by the high-level Bind* helpers. IsFormed remains controlled by the engine and is
	// intentionally not re-derived from this list.
	private readonly INotifyList<IIndicator> _indicators = new SynchronizedSet<IIndicator>();

	/// <summary>
	/// All indicators registered via the high-level <see cref="ISubscriptionHandler{T}"/> binders.
	/// </summary>
	public INotifyList<IIndicator> Indicators => _indicators;

	// The decomposed Strategy does not own a backtesting flag in this subsystem, so it is derived from
	// the connector type, exactly as the monolith StrategyOld.IsBacktesting did.
	private bool IsBacktestingMode
		=> _connector is HistoryEmulationConnector;

	private DateTime HighLevelCurrentTime
		=> ((IStrategyHost)this).CurrentTime;

	/// <summary>
	/// Send alert notification.
	/// </summary>
	/// <param name="type">Alert type.</param>
	/// <param name="caption">Signal header.</param>
	/// <param name="message">Alert text.</param>
	protected void Alert(AlertNotifications type, string caption, string message)
	{
		// Skip non-log alerts during backtesting
		if (IsBacktestingMode && type != AlertNotifications.Log)
			return;

		var svc = GetAlertService();
		if (svc is null)
			return;

		svc.NotifyAsync(type, null, LogLevels.Info, caption, message, HighLevelCurrentTime, default)
			.AsTask()
			.ContinueWith(t =>
			{
				if (t.IsFaulted && t.Exception is not null)
					this.AddErrorLog(t.Exception);
			});
	}

	/// <summary>
	/// Send alert notification with strategy name as caption.
	/// </summary>
	/// <param name="type">Alert type.</param>
	/// <param name="message">Alert text.</param>
	protected void Alert(AlertNotifications type, string message)
		=> Alert(type, Name, message);

	/// <summary>
	/// Send popup alert notification.
	/// </summary>
	/// <param name="message">Alert text.</param>
	protected void AlertPopup(string message)
		=> Alert(AlertNotifications.Popup, message);

	/// <summary>
	/// Send sound alert notification.
	/// </summary>
	/// <param name="message">Alert text.</param>
	protected void AlertSound(string message)
		=> Alert(AlertNotifications.Sound, message);

	/// <summary>
	/// Send log alert notification.
	/// </summary>
	/// <param name="message">Alert text.</param>
	protected void AlertLog(string message)
		=> Alert(AlertNotifications.Log, message);

	/// <summary>
	/// Subscription handler.
	/// </summary>
	/// <typeparam name="T">Market-data type.</typeparam>
	private class SubscriptionHandler<T> : Disposable, ISubscriptionHandler<T>
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

			internal virtual IEnumerable<IIndicatorValue> Invoke(T typed, DateTime time)
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

			internal override IEnumerable<IIndicatorValue> Invoke(T typed, DateTime time)
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

		protected override void DisposeManaged()
		{
			base.DisposeManaged();

			Stop();
		}

		/// <summary>
		/// <see cref="Subscription"/>.
		/// </summary>
		public Subscription Subscription { get; }

		private void StartSubscription()
		{
			var type = typeof(T);

			void tryActivateProtection(decimal price, DateTime time)
			{
				var info = _strategy.PosControllerTryActivate(price, time);

				if (info is not null)
					_strategy.ActiveProtection(info.Value);
			}

			void handle(object v, DateTime time, Func<ICandleMessage> getCandle)
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

						tryActivateProtection(v.ClosePrice, _strategy.HighLevelCurrentTime);

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

						tryActivateProtection(v.Price, _strategy.HighLevelCurrentTime);

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

						tryActivateProtection(price, _strategy.HighLevelCurrentTime);

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

						tryActivateProtection(price, _strategy.HighLevelCurrentTime);

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

		public ISubscriptionHandler<T> Stop()
		{
			_strategy.UnSubscribe(Subscription);
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

			ValidateIndicators(indicator);

			return BindEx(indicator, (v, iv) => callback(v, iv.ToNullableDecimal(indicator.Source)), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator, Action<T, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator);

			return BindEx(indicator, (v, iv) => callback(v, iv.ToDecimal(indicator.Source)), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, Action<T, decimal?, decimal?> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2);

			return BindEx(indicator1, indicator2, (v, iv1, iv2) => callback(v, iv1.ToNullableDecimal(indicator1.Source), iv2.ToNullableDecimal(indicator2.Source)), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, Action<T, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2);

			return BindEx(indicator1, indicator2, (v, iv1, iv2) => callback(v, iv1.ToDecimal(indicator1.Source), iv2.ToDecimal(indicator2.Source)), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, decimal?, decimal?, decimal?> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3);

			return BindEx(indicator1, indicator2, indicator3, (v, iv1, iv2, iv3) => callback(v, iv1.ToNullableDecimal(indicator1.Source), iv2.ToNullableDecimal(indicator2.Source), iv3.ToNullableDecimal(indicator3.Source)), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, decimal, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3);

			return BindEx(indicator1, indicator2, indicator3, (v, iv1, iv2, iv3) => callback(v, iv1.ToDecimal(indicator1.Source), iv2.ToDecimal(indicator2.Source), iv3.ToDecimal(indicator3.Source)), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, decimal?, decimal?, decimal?, decimal?> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3, indicator4);

			return BindEx(indicator1, indicator2, indicator3, indicator4, (v, iv1, iv2, iv3, iv4) => callback(v, iv1.ToNullableDecimal(indicator1.Source), iv2.ToNullableDecimal(indicator2.Source), iv3.ToNullableDecimal(indicator3.Source), iv4.ToNullableDecimal(indicator4.Source)), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, decimal, decimal, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3, indicator4);

			return BindEx(indicator1, indicator2, indicator3, indicator4, (v, iv1, iv2, iv3, iv4) => callback(v, iv1.ToDecimal(indicator1.Source), iv2.ToDecimal(indicator2.Source), iv3.ToDecimal(indicator3.Source), iv4.ToDecimal(indicator4.Source)), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, Action<T, decimal?, decimal?, decimal?, decimal?, decimal?> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3, indicator4, indicator5);

			return BindEx(indicator1, indicator2, indicator3, indicator4, indicator5, (v, iv1, iv2, iv3, iv4, iv5) => callback(v,
				iv1.ToNullableDecimal(indicator1.Source),
				iv2.ToNullableDecimal(indicator2.Source),
				iv3.ToNullableDecimal(indicator3.Source),
				iv4.ToNullableDecimal(indicator4.Source),
				iv5.ToNullableDecimal(indicator5.Source)), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, Action<T, decimal, decimal, decimal, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3, indicator4, indicator5);

			return BindEx(indicator1, indicator2, indicator3, indicator4, indicator5, (v, iv1, iv2, iv3, iv4, iv5) => callback(v,
				iv1.ToDecimal(indicator1.Source),
				iv2.ToDecimal(indicator2.Source),
				iv3.ToDecimal(indicator3.Source),
				iv4.ToDecimal(indicator4.Source),
				iv5.ToDecimal(indicator5.Source)), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, Action<T, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6);

			return BindEx(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, (v, iv1, iv2, iv3, iv4, iv5, iv6) => callback(v,
				iv1.ToNullableDecimal(indicator1.Source),
				iv2.ToNullableDecimal(indicator2.Source),
				iv3.ToNullableDecimal(indicator3.Source),
				iv4.ToNullableDecimal(indicator4.Source),
				iv5.ToNullableDecimal(indicator5.Source),
				iv6.ToNullableDecimal(indicator6.Source)), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, Action<T, decimal, decimal, decimal, decimal, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6);

			return BindEx(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, (v, iv1, iv2, iv3, iv4, iv5, iv6) => callback(v,
				iv1.ToDecimal(indicator1.Source),
				iv2.ToDecimal(indicator2.Source),
				iv3.ToDecimal(indicator3.Source),
				iv4.ToDecimal(indicator4.Source),
				iv5.ToDecimal(indicator5.Source),
				iv6.ToDecimal(indicator6.Source)), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, Action<T, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, indicator7);

			return BindEx(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, indicator7, (v, iv1, iv2, iv3, iv4, iv5, iv6, iv7) => callback(v,
				iv1.ToNullableDecimal(indicator1.Source),
				iv2.ToNullableDecimal(indicator2.Source),
				iv3.ToNullableDecimal(indicator3.Source),
				iv4.ToNullableDecimal(indicator4.Source),
				iv5.ToNullableDecimal(indicator5.Source),
				iv6.ToNullableDecimal(indicator6.Source),
				iv7.ToNullableDecimal(indicator7.Source)), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, Action<T, decimal, decimal, decimal, decimal, decimal, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, indicator7);

			return BindEx(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, indicator7, (v, iv1, iv2, iv3, iv4, iv5, iv6, iv7) => callback(v,
				iv1.ToDecimal(indicator1.Source),
				iv2.ToDecimal(indicator2.Source),
				iv3.ToDecimal(indicator3.Source),
				iv4.ToDecimal(indicator4.Source),
				iv5.ToDecimal(indicator5.Source),
				iv6.ToDecimal(indicator6.Source),
				iv7.ToDecimal(indicator7.Source)), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, IIndicator indicator8, Action<T, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, indicator7, indicator8);

			return BindEx(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, indicator7, indicator8, (v, iv1, iv2, iv3, iv4, iv5, iv6, iv7, iv8) => callback(v,
				iv1.ToNullableDecimal(indicator1.Source),
				iv2.ToNullableDecimal(indicator2.Source),
				iv3.ToNullableDecimal(indicator3.Source),
				iv4.ToNullableDecimal(indicator4.Source),
				iv5.ToNullableDecimal(indicator5.Source),
				iv6.ToNullableDecimal(indicator6.Source),
				iv7.ToNullableDecimal(indicator7.Source),
				iv8.ToNullableDecimal(indicator8.Source)), true);
		}

		public ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, IIndicator indicator8, Action<T, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			ValidateIndicators(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, indicator7, indicator8);

			return BindEx(indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, indicator7, indicator8, (v, iv1, iv2, iv3, iv4, iv5, iv6, iv7, iv8) => callback(v,
				iv1.ToDecimal(indicator1.Source),
				iv2.ToDecimal(indicator2.Source),
				iv3.ToDecimal(indicator3.Source),
				iv4.ToDecimal(indicator4.Source),
				iv5.ToDecimal(indicator5.Source),
				iv6.ToDecimal(indicator6.Source),
				iv7.ToDecimal(indicator7.Source),
				iv8.ToDecimal(indicator8.Source)), false);
		}

		private static void ValidateIndicators(params IIndicator[] indicators)
		{
			foreach (var ind in indicators)
			{
				if (ind is null)
					throw new ArgumentNullException(nameof(indicators));

				if (ind is IComplexIndicator)
					throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicators));
			}
		}

		public ISubscriptionHandler<T> Bind(IIndicator[] indicators, Action<T, decimal[]> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicators is null)
				throw new ArgumentNullException(nameof(indicators));

			if (indicators.Any(i => i is IComplexIndicator))
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicators));

			return BindEx(indicators, (v, ivs) => callback(v, [.. ivs.Select((val, idx) => val.ToDecimal(indicators[idx].Source))]), false);
		}

		public ISubscriptionHandler<T> BindWithEmpty(IIndicator[] indicators, Action<T, decimal?[]> callback)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));

			if (indicators is null)
				throw new ArgumentNullException(nameof(indicators));

			if (indicators.Any(i => i is IComplexIndicator))
				throw new ArgumentException(LocalizedStrings.IndicatorNotComposite, nameof(indicators));

			return BindEx(indicators, (v, ivs) => callback(v, [.. ivs.Select((val, idx) => val.ToNullableDecimal(indicators[idx].Source))]), true);
		}

		public ISubscriptionHandler<T> BindEx(IIndicator indicator, Action<T, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator], (c, v) => callback(c, v[0]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, Action<T, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator1, indicator2], (c, v) => callback(c, v[0], v[1]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator1, indicator2, indicator3], (c, v) => callback(c, v[0], v[1], v[2]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator1, indicator2, indicator3, indicator4], (c, v) => callback(c, v[0], v[1], v[2], v[3]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator1, indicator2, indicator3, indicator4, indicator5], (c, v) => callback(c, v[0], v[1], v[2], v[3], v[4]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator1, indicator2, indicator3, indicator4, indicator5, indicator6], (c, v) => callback(c, v[0], v[1], v[2], v[3], v[4], v[5]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, indicator7], (c, v) => callback(c, v[0], v[1], v[2], v[3], v[4], v[5], v[6]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, IIndicator indicator8, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty)
			=> BindEx([indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, indicator7, indicator8], (c, v) => callback(c, v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7]), allowEmpty);

		public ISubscriptionHandler<T> BindEx(IIndicator[] indicators, Action<T, IIndicatorValue[]> callback, bool allowEmpty)
		{
			_binders.Add(new SubscriptionHandlerBinderArray(this, indicators, allowEmpty).SetCallback(callback));
			return this;
		}
	}

	// Single-security protection activation used by the high-level handlers. Mirrors the monolith's
	// _posController?.TryActivate(price, time) call against the protective controller already owned by
	// the decomposed Strategy (declared in Strategy.cs).
	private (bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition)? PosControllerTryActivate(decimal price, DateTime time)
		=> _posController?.TryActivate(price, time);

	/// <summary>
	/// Subscribe to candles.
	/// </summary>
	/// <param name="tf">Time-frame.</param>
	/// <param name="isFinishedOnly"><see cref="MarketDataMessage.IsFinishedOnly"/></param>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<ICandleMessage> SubscribeCandles(TimeSpan tf, bool isFinishedOnly = true, Security security = default)
		=> SubscribeCandles(tf.TimeFrame(), isFinishedOnly, security);

	/// <summary>
	/// Subscribe to candles.
	/// </summary>
	/// <param name="dt"><see cref="DataType"/></param>
	/// <param name="isFinishedOnly"><see cref="MarketDataMessage.IsFinishedOnly"/></param>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
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
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<ICandleMessage> SubscribeCandles(Subscription subscription)
		=> new SubscriptionHandler<ICandleMessage>(this, subscription);

	/// <summary>
	/// Subscribe to <see cref="DataType.Ticks"/>.
	/// </summary>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<ITickTradeMessage> SubscribeTicks(Security security = null)
		=> SubscribeTicks(new Subscription(DataType.Ticks, security ?? Security));

	/// <summary>
	/// Subscribe to <see cref="DataType.Ticks"/>.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<ITickTradeMessage> SubscribeTicks(Subscription subscription)
		=> new SubscriptionHandler<ITickTradeMessage>(this, subscription);

	/// <summary>
	/// Subscribe to <see cref="DataType.Level1"/>.
	/// </summary>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<Level1ChangeMessage> SubscribeLevel1(Security security = null)
		=> SubscribeLevel1(new Subscription(DataType.Level1, security ?? Security));

	/// <summary>
	/// Subscribe to <see cref="DataType.Level1"/>.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<Level1ChangeMessage> SubscribeLevel1(Subscription subscription)
		=> new SubscriptionHandler<Level1ChangeMessage>(this, subscription);

	/// <summary>
	/// Subscribe to <see cref="DataType.MarketDepth"/>.
	/// </summary>
	/// <param name="security"><see cref="BusinessEntities.Security"/>. If security is not passed, then <see cref="Security"/> value is used.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<IOrderBookMessage> SubscribeOrderBook(Security security = null)
		=> SubscribeOrderBook(new Subscription(DataType.MarketDepth, security ?? Security));

	/// <summary>
	/// Subscribe to <see cref="DataType.MarketDepth"/>.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	protected ISubscriptionHandler<IOrderBookMessage> SubscribeOrderBook(Subscription subscription)
		=> new SubscriptionHandler<IOrderBookMessage>(this, subscription);

	/// <summary>
	/// Timer handler.
	/// </summary>
	public interface ITimerHandler : IDisposable
	{
		/// <summary>
		/// Start the timer.
		/// </summary>
		/// <returns><see cref="ITimerHandler"/></returns>
		ITimerHandler Start();

		/// <summary>
		/// Stop the timer.
		/// </summary>
		/// <returns><see cref="ITimerHandler"/></returns>
		ITimerHandler Stop();

		/// <summary>
		/// Timer interval.
		/// </summary>
		TimeSpan Interval { get; set; }

		/// <summary>
		/// Whether the timer is running.
		/// </summary>
		bool IsStarted { get; }
	}

	/// <summary>
	/// Timer handler implementation.
	/// </summary>
	private class TimerHandler : Disposable, ITimerHandler
	{
		private readonly Strategy _strategy;
		private readonly Action _callback;
		private IMarketRule _rule;

		public TimerHandler(Strategy strategy, TimeSpan interval, Action callback)
		{
			if (interval <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(interval), interval, LocalizedStrings.InvalidValue);

			_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
			_callback = callback ?? throw new ArgumentNullException(nameof(callback));
			Interval = interval;
		}

		/// <inheritdoc />
		public TimeSpan Interval { get; set; }

		/// <inheritdoc />
		public bool IsStarted => _rule != null;

		/// <inheritdoc />
		public ITimerHandler Start()
		{
			if (IsStarted)
				return this;

			// The decomposed Strategy is both an ITimeProvider and an IMarketRuleContainer, so the timer is
			// expressed with the same WhenIntervalElapsed market rule the monolith used.
			_rule = ((ITimeProvider)_strategy)
				.WhenIntervalElapsed(Interval)
				.Do(_callback)
				.Apply(_strategy);

			return this;
		}

		/// <inheritdoc />
		public ITimerHandler Stop()
		{
			_rule?.Dispose();
			_rule = null;
			return this;
		}

		/// <inheritdoc />
		protected override void DisposeManaged()
		{
			Stop();
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// Create a timer that executes callback at specified intervals.
	/// </summary>
	/// <param name="interval">Timer interval.</param>
	/// <param name="callback">Callback to execute.</param>
	/// <returns><see cref="ITimerHandler"/></returns>
	protected ITimerHandler CreateTimer(TimeSpan interval, Action callback)
		=> new TimerHandler(this, interval, callback);

	/// <summary>
	/// Create a timer that executes callback at specified intervals and starts it immediately.
	/// </summary>
	/// <param name="interval">Timer interval.</param>
	/// <param name="callback">Callback to execute.</param>
	/// <returns><see cref="ITimerHandler"/></returns>
	protected ITimerHandler StartTimer(TimeSpan interval, Action callback)
		=> CreateTimer(interval, callback).Start();
}