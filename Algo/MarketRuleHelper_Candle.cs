namespace StockSharp.Algo
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class MarketRuleHelper
	{
		private abstract class BaseCandleSeriesRule<TArg> : MarketRule<CandleSeries, TArg>
		{
			protected BaseCandleSeriesRule(CandleSeries series)
				: base(series)
			{
				Series = series ?? throw new ArgumentNullException(nameof(series));
			}

			protected CandleSeries Series { get; }
		}

		private abstract class CandleSeriesRule<TCandle> : BaseCandleSeriesRule<TCandle>
			where TCandle : ICandleMessage
		{
			private readonly ICandleManager<TCandle> _candleManager;

			protected CandleSeriesRule(ICandleManager<TCandle> candleManager, CandleSeries series)
				: base(series)
			{
				_candleManager = candleManager ?? throw new ArgumentNullException(nameof(candleManager));
				_candleManager.Processing += OnProcessing;
			}

			private void OnProcessing(CandleSeries series, TCandle candle)
			{
				if (Series != series)
					return;

				OnProcessCandle(candle);
			}

			protected abstract void OnProcessCandle(TCandle candle);

			protected override void DisposeManaged()
			{
				_candleManager.Processing -= OnProcessing;
				base.DisposeManaged();
			}
		}

		private sealed class CandleStateSeriesRule<TCandle> : CandleSeriesRule<TCandle>
			where TCandle : ICandleMessage
		{
			private readonly CandleStates _state;
			private readonly CandleStates[] _states;

			public CandleStateSeriesRule(ICandleManager<TCandle> candleManager, CandleSeries series, params CandleStates[] states)
				: base(candleManager, series)
			{
				if (states == null)
					throw new ArgumentNullException(nameof(states));

				if (states.IsEmpty())
					throw new ArgumentOutOfRangeException(nameof(states));

				_state = states[0];

				if (states.Length > 1)
					_states = states;
			}

			protected override void OnProcessCandle(TCandle candle)
			{
				if ((_states == null && candle.State == _state) || (_states != null && _states.Contains(candle.State)))
					Activate(candle);
			}
		}

		private sealed class CandleStartedRule<TCandle> : CandleSeriesRule<TCandle>
			where TCandle : ICandleMessage
		{
			private TCandle _currCandle;

			public CandleStartedRule(ICandleManager<TCandle> candleManager, CandleSeries series)
				: base(candleManager, series)
			{
			}

			protected override void OnProcessCandle(TCandle candle)
			{
				if (_currCandle?.IsSame(candle) == true)
					return;

				_currCandle = candle;
				Activate(candle);
			}
		}

		private sealed class CandleChangedSeriesRule<TCandle> : CandleSeriesRule<TCandle>
			where TCandle : ICandleMessage
		{
			private readonly Func<TCandle, bool> _condition;

			public CandleChangedSeriesRule(ICandleManager<TCandle> candleManager, CandleSeries series)
				: this(candleManager, series, c => true)
			{
			}

			public CandleChangedSeriesRule(ICandleManager<TCandle> candleManager, CandleSeries series, Func<TCandle, bool> condition)
				: base(candleManager, series)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
				Name = LocalizedStrings.Str1064 + " " + series;
			}

			protected override void OnProcessCandle(TCandle candle)
			{
				if (candle.State == CandleStates.Active && _condition(candle))
					Activate(candle);
			}
		}

		private sealed class CurrentCandleSeriesRule<TCandle> : CandleSeriesRule<TCandle>
			where TCandle : ICandleMessage
		{
			private readonly Func<TCandle, bool> _condition;

			public CurrentCandleSeriesRule(ICandleManager<TCandle> candleManager, CandleSeries series, Func<TCandle, bool> condition)
				: base(candleManager, series)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
			}

			protected override void OnProcessCandle(TCandle candle)
			{
				if (candle.State == CandleStates.Active && _condition(candle))
					Activate(candle);
			}
		}

		private abstract class CandleRule<TCandle> : MarketRule<TCandle, TCandle>
			where TCandle : ICandleMessage
		{
			private readonly ICandleManager<TCandle> _candleManager;

			protected CandleRule(ICandleManager<TCandle> candleManager, TCandle candle)
				: base(candle)
			{
				_candleManager = candleManager ?? throw new ArgumentNullException(nameof(candleManager));
				_candleManager.Processing += OnProcessing;

				Candle = candle;
			}

			private void OnProcessing(CandleSeries series, TCandle candle)
			{
				if (!Candle.IsSame(candle))
					return;

				OnProcessCandle(candle);
			}

			protected abstract void OnProcessCandle(TCandle candle);

			protected override void DisposeManaged()
			{
				_candleManager.Processing -= OnProcessing;
				base.DisposeManaged();
			}

			protected TCandle Candle { get; }
		}

		private sealed class ChangedCandleRule<TCandle> : CandleRule<TCandle>
			where TCandle : ICandleMessage
		{
			private readonly Func<TCandle, bool> _condition;

			public ChangedCandleRule(ICandleManager<TCandle> candleManager, TCandle candle)
				: this(candleManager, candle, c => true)
			{
			}

			public ChangedCandleRule(ICandleManager<TCandle> candleManager, TCandle candle, Func<TCandle, bool> condition)
				: base(candleManager, candle)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
				Name = LocalizedStrings.Str1065 + " " + candle;
			}

			protected override void OnProcessCandle(TCandle candle)
			{
				if (candle.State == CandleStates.Active && Candle.IsSame(candle) && _condition(Candle))
					Activate(Candle);
			}
		}

		private sealed class FinishedCandleRule<TCandle> : CandleRule<TCandle>
			where TCandle : ICandleMessage
		{
			public FinishedCandleRule(ICandleManager<TCandle> candleManager, TCandle candle)
				: base(candleManager, candle)
			{
				Name = LocalizedStrings.Str1066 + " " + candle;
			}

			protected override void OnProcessCandle(TCandle candle)
			{
				if (candle.State == CandleStates.Finished && candle.IsSame(Candle))
					Activate(Candle);
			}
		}

		/// <summary>
		/// To create a rule for the event of candle closing price excess above a specific level.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle closing price excess above a specific level.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenClosePriceMore<TCandle>(this ICandleManager<TCandle> candleManager, TCandle candle, Unit price)
			where TCandle : ICandleMessage
		{
			return new ChangedCandleRule<TCandle>(candleManager, candle, candle.CreateCandleCondition(price, c => c.ClosePrice, false))
			{
				Name = LocalizedStrings.Str1067Params.Put(candle, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of candle closing price reduction below a specific level.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle closing price reduction below a specific level.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenClosePriceLess<TCandle>(this ICandleManager<TCandle> candleManager, TCandle candle, Unit price)
			where TCandle : ICandleMessage
		{
			return new ChangedCandleRule<TCandle>(candleManager, candle, candle.CreateCandleCondition(price, c => c.ClosePrice, true))
			{
				Name = LocalizedStrings.Str1068Params.Put(candle, price)
			};
		}

		private static Func<TCandle, bool> CreateCandleCondition<TCandle>(this TCandle candle, Unit price, Func<TCandle, decimal> currentPrice, bool isLess)
			where TCandle : ICandleMessage
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			if (price == null)
				throw new ArgumentNullException(nameof(price));

			if (currentPrice == null)
				throw new ArgumentNullException(nameof(currentPrice));

			if (price.Value == 0)
				throw new ArgumentException(LocalizedStrings.Str1051, nameof(price));

			if (price.Value < 0)
				throw new ArgumentException(LocalizedStrings.Str1052, nameof(price));

			if (isLess)
			{
				var finishPrice = (decimal)(price.Type == UnitTypes.Limit ? price : currentPrice(candle) - price);
				return c => currentPrice(c) < finishPrice;
			}
			else
			{
				var finishPrice = (decimal)(price.Type == UnitTypes.Limit ? price : currentPrice(candle) + price);
				return c => currentPrice(c) > finishPrice;
			}
		}

		/// <summary>
		/// To create a rule for the event of candle total volume excess above a specific level.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle total volume excess above a specific level.</param>
		/// <param name="volume">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenTotalVolumeMore<TCandle>(this ICandleManager<TCandle> candleManager, TCandle candle, Unit volume)
			where TCandle : ICandleMessage
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			var finishVolume = volume.Type == UnitTypes.Limit ? volume : candle.TotalVolume + volume;

			return new ChangedCandleRule<TCandle>(candleManager, candle, c => c.TotalVolume > finishVolume)
			{
				Name = candle + LocalizedStrings.Str1069Params.Put(volume)
			};
		}

		/// <summary>
		/// To create a rule for the event of candle total volume excess above a specific level.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series, from which a candle will be taken.</param>
		/// <param name="volume">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, TCandle> WhenCurrentCandleTotalVolumeMore<TCandle>(this ICandleManager<TCandle> candleManager, CandleSeries series, Unit volume)
			where TCandle : ICandleMessage
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var finishVolume = volume;

			if (volume.Type != UnitTypes.Limit)
			{
				var curCandle = candleManager.GetCurrentCandle<TCandle>(series);

				if (curCandle == null)
					throw new ArgumentException(LocalizedStrings.Str1070, nameof(series));

				finishVolume = curCandle.TotalVolume + volume;	
			}

			return new CurrentCandleSeriesRule<TCandle>(candleManager, series, candle => candle.TotalVolume > finishVolume)
			{
				Name = series + LocalizedStrings.Str1071Params.Put(volume)
			};
		}

		/// <summary>
		/// To create a rule for the event of new candles occurrence.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for new candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, TCandle> WhenCandlesStarted<TCandle>(this ICandleManager<TCandle> candleManager, CandleSeries series)
			where TCandle : ICandleMessage
		{
			return new CandleStartedRule<TCandle>(candleManager, series) { Name = LocalizedStrings.Str1072 + " " + series };
		}

		/// <summary>
		/// To create a rule for candle change event.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for changed candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, TCandle> WhenCandlesChanged<TCandle>(this ICandleManager<TCandle> candleManager, CandleSeries series)
			where TCandle : ICandleMessage
		{
			return new CandleChangedSeriesRule<TCandle>(candleManager, series);
		}

		/// <summary>
		/// To create a rule for candles end event.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for end of candle.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, TCandle> WhenCandlesFinished<TCandle>(this ICandleManager<TCandle> candleManager, CandleSeries series)
			where TCandle : ICandleMessage
		{
			return new CandleStateSeriesRule<TCandle>(candleManager, series, CandleStates.Finished) { Name = LocalizedStrings.Str1073 + " " + series };
		}

		/// <summary>
		/// To create a rule for the event of candles occurrence, change and end.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, TCandle> WhenCandles<TCandle>(this ICandleManager<TCandle> candleManager, CandleSeries series)
			where TCandle : ICandleMessage
		{
			return new CandleStateSeriesRule<TCandle>(candleManager, series, CandleStates.Active, CandleStates.Finished)
			{
				Name = LocalizedStrings.Candles + " " + series
			};
		}

		/// <summary>
		/// To create a rule for candle change event.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for change.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenChanged<TCandle>(this ICandleManager<TCandle> candleManager, TCandle candle)
			where TCandle : ICandleMessage
		{
			return new ChangedCandleRule<TCandle>(candleManager, candle);
		}

		/// <summary>
		/// To create a rule for candle end event.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for end.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenFinished<TCandle>(this ICandleManager<TCandle> candleManager, TCandle candle)
			where TCandle : ICandleMessage
		{
			return new FinishedCandleRule<TCandle>(candleManager, candle).Once();
		}

		/// <summary>
		/// To create a rule for the event of candle partial end.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for partial end.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="percent">The percentage of candle completion.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenPartiallyFinished<TCandle>(this ICandleManager<TCandle> candleManager, TCandle candle, IConnector connector, decimal percent)
			where TCandle : ICandleMessage
		{
			var rule = candle is ITimeFrameCandleMessage
				? (MarketRule<TCandle, TCandle>)new TimeFrameCandleChangedRule<TCandle>(candle, connector, percent)
			    : new ChangedCandleRule<TCandle>(candleManager, candle, c => c.IsCandlePartiallyFinished(percent));

			rule.Name = LocalizedStrings.Str1075Params.Put(percent);
			return rule;
		}

		/// <summary>
		/// To create a rule for the event of candle partial end.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">The candle series to be traced for candle partial end.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="percent">The percentage of candle completion.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, TCandle> WhenPartiallyFinishedCandles<TCandle>(this ICandleManager<TCandle> candleManager, CandleSeries series, IConnector connector, decimal percent)
			where TCandle : ICandleMessage
		{
			var rule = series.CandleType.Is<ITimeFrameCandleMessage>()
				? (MarketRule<CandleSeries, TCandle>)new TimeFrameCandlesChangedSeriesRule<TCandle>(candleManager, series, connector, percent)
				: new CandleChangedSeriesRule<TCandle>(candleManager, series, candle => candle.IsCandlePartiallyFinished(percent));

			rule.Name = LocalizedStrings.Str1076Params.Put(percent);
			return rule;
		}

		private sealed class TimeFrameCandleChangedRule<TCandle> : MarketRule<TCandle, TCandle>
			where TCandle : ICandleMessage
		{
			private readonly MarketTimer _timer;

			public TimeFrameCandleChangedRule(TCandle candle, IConnector connector, decimal percent)
				: base(candle)
			{
				_timer = CreateAndActivateTimeFrameTimer(candle.SecurityId, (TimeSpan)candle.Arg, connector, () => Activate(candle), percent, false);
			}

			protected override void DisposeManaged()
			{
				_timer.Dispose();
				base.DisposeManaged();
			}
		}

		private sealed class TimeFrameCandlesChangedSeriesRule<TCandle> : BaseCandleSeriesRule<TCandle>
			where TCandle : ICandleMessage
		{
			private readonly MarketTimer _timer;

			public TimeFrameCandlesChangedSeriesRule(ICandleManager<TCandle> candleManager, CandleSeries series, IConnector connector, decimal percent)
				: base(series)
			{
				if (candleManager == null)
					throw new ArgumentNullException(nameof(candleManager));

				_timer = CreateAndActivateTimeFrameTimer(series.Security.ToSecurityId(), (TimeSpan)series.Arg, connector, () => Activate(candleManager.GetCurrentCandle<TCandle>(Series)), percent, true);
			}

			protected override void DisposeManaged()
			{
				_timer.Dispose();
				base.DisposeManaged();
			}
		}

		private static MarketTimer CreateAndActivateTimeFrameTimer(SecurityId securityId, TimeSpan timeFrame, IConnector connector, Action callback, decimal percent, bool periodical)
		{
			if (securityId == default)
				throw new ArgumentNullException(nameof(securityId));

			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (callback == null)
				throw new ArgumentNullException(nameof(callback));

			if (percent <= 0)
				throw new ArgumentOutOfRangeException(nameof(percent), LocalizedStrings.Str1077);

			MarketTimer timer = null;

			timer = new MarketTimer(connector, () =>
			{
				if (periodical)
					timer.Interval(timeFrame);
				else
					timer.Stop();

				callback();
			});

			var time = connector.CurrentTime;

			// TODO
			var candleBounds = timeFrame.GetCandleBounds(time, TimeZoneInfo.Utc, new());

			percent /= 100;

			var startTime = candleBounds.Min + TimeSpan.FromMilliseconds(timeFrame.TotalMilliseconds * (double)percent);

			var diff = startTime - time;

			if (diff == TimeSpan.Zero)
				timer.Interval(timeFrame);
			else if (diff > TimeSpan.Zero)
				timer.Interval(diff);
			else
				timer.Interval(timeFrame + diff);

			return timer.Start();
		}

		private static bool IsCandlePartiallyFinished<TCandle>(this TCandle candle, decimal percent)
			where TCandle : ICandleMessage
		{
			if (candle is null)
				throw new ArgumentNullException(nameof(candle));

			if (percent <= 0)
				throw new ArgumentOutOfRangeException(nameof(percent), LocalizedStrings.Str1077);

			var realPercent = percent / 100;

			if (candle is ITickCandleMessage tickCandle)
			{
				var count = realPercent * (int)candle.Arg;
				return tickCandle.TotalTicks != null && tickCandle.TotalTicks.Value >= count;
			}
			else if (candle is IRangeCandleMessage rangeCandle)
			{
				return (decimal)(candle.LowPrice + rangeCandle.TypedArg) >= realPercent * candle.HighPrice;
			}
			else if (candle is IVolumeCandleMessage volCandle)
			{
				var volume = realPercent * volCandle.TypedArg;
				return candle.TotalVolume >= volume;
			}
			else
				throw new ArgumentOutOfRangeException(nameof(candle), candle.GetType(), LocalizedStrings.WrongCandleType);
		}

		/// <summary>
		/// Backward compatibility.
		/// </summary>
		public static MarketRule<TToken, ICandleMessage> Do<TToken>(this MarketRule<TToken, ICandleMessage> rule, Action<Candle> action)
		{
			if (action is null)
				throw new ArgumentNullException(nameof(action));

			return rule.Do((ICandleMessage msg) => action((Candle)msg));
		}

		/// <summary>
		/// Backward compatibility.
		/// </summary>
		public static MarketRule<TToken, ICandleMessage> Do<TToken, TResult>(this MarketRule<TToken, ICandleMessage> rule, Func<Candle, TResult> action)
		{
			if (action is null)
				throw new ArgumentNullException(nameof(action));

			return rule.Do((ICandleMessage msg) => action((Candle)msg));
		}
	}
}