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

		private abstract class CandleSeriesRule<TArg> : BaseCandleSeriesRule<TArg>
		{
			private readonly ICandleManager _candleManager;

			protected CandleSeriesRule(ICandleManager candleManager, CandleSeries series)
				: base(series)
			{
				_candleManager = candleManager ?? throw new ArgumentNullException(nameof(candleManager));
				_candleManager.Processing += OnProcessing;
			}

			private void OnProcessing(CandleSeries series, Candle candle)
			{
				if (Series != series)
					return;

				OnProcessCandle(candle);
			}

			protected abstract void OnProcessCandle(Candle candle);

			protected override void DisposeManaged()
			{
				_candleManager.Processing -= OnProcessing;
				base.DisposeManaged();
			}
		}

		private sealed class CandleStateSeriesRule : CandleSeriesRule<Candle>
		{
			private readonly CandleStates _state;
			private readonly CandleStates[] _states;

			public CandleStateSeriesRule(ICandleManager candleManager, CandleSeries series, params CandleStates[] states)
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

			protected override void OnProcessCandle(Candle candle)
			{
				if ((_states == null && candle.State == _state) || (_states != null && _states.Contains(candle.State)))
					Activate(candle);
			}
		}

		private sealed class CandleStartedRule : CandleSeriesRule<Candle>
		{
			private Candle _currCandle;

			public CandleStartedRule(ICandleManager candleManager, CandleSeries series)
				: base(candleManager, series)
			{
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (_currCandle != null && _currCandle == candle)
					return;

				_currCandle = candle;
				Activate(candle);
			}
		}

		private sealed class CandleChangedSeriesRule : CandleSeriesRule<Candle>
		{
			private readonly Func<Candle, bool> _condition;

			public CandleChangedSeriesRule(ICandleManager candleManager, CandleSeries series)
				: this(candleManager, series, c => true)
			{
			}

			public CandleChangedSeriesRule(ICandleManager candleManager, CandleSeries series, Func<Candle, bool> condition)
				: base(candleManager, series)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
				Name = LocalizedStrings.Str1064 + " " + series;
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (candle.State == CandleStates.Active && _condition(candle))
					Activate(candle);
			}
		}

		private sealed class CurrentCandleSeriesRule : CandleSeriesRule<Candle>
		{
			private readonly Func<Candle, bool> _condition;

			public CurrentCandleSeriesRule(ICandleManager candleManager, CandleSeries series, Func<Candle, bool> condition)
				: base(candleManager, series)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (candle.State == CandleStates.Active && _condition(candle))
					Activate(candle);
			}
		}

		private abstract class CandleRule : MarketRule<Candle, Candle>
		{
			private readonly ICandleManager _candleManager;

			protected CandleRule(ICandleManager candleManager, Candle candle)
				: base(candle)
			{
				_candleManager = candleManager ?? throw new ArgumentNullException(nameof(candleManager));
				_candleManager.Processing += OnProcessing;

				Candle = candle;
			}

			private void OnProcessing(CandleSeries series, Candle candle)
			{
				if (Candle != candle)
					return;

				OnProcessCandle(candle);
			}

			protected abstract void OnProcessCandle(Candle candle);

			protected override void DisposeManaged()
			{
				_candleManager.Processing -= OnProcessing;
				base.DisposeManaged();
			}

			protected Candle Candle { get; }
		}

		private sealed class ChangedCandleRule : CandleRule
		{
			private readonly Func<Candle, bool> _condition;

			public ChangedCandleRule(ICandleManager candleManager, Candle candle)
				: this(candleManager, candle, c => true)
			{
			}

			public ChangedCandleRule(ICandleManager candleManager, Candle candle, Func<Candle, bool> condition)
				: base(candleManager, candle)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
				Name = LocalizedStrings.Str1065 + " " + candle;
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (candle.State == CandleStates.Active && Candle == candle && _condition(Candle))
					Activate(Candle);
			}
		}

		private sealed class FinishedCandleRule : CandleRule
		{
			public FinishedCandleRule(ICandleManager candleManager, Candle candle)
				: base(candleManager, candle)
			{
				Name = LocalizedStrings.Str1066 + " " + candle;
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (candle.State == CandleStates.Finished && candle == Candle)
					Activate(Candle);
			}
		}

		/// <summary>
		/// To create a rule for the event of candle closing price excess above a specific level.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle closing price excess above a specific level.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenClosePriceMore(this ICandleManager candleManager, Candle candle, Unit price)
		{
			return new ChangedCandleRule(candleManager, candle, candle.CreateCandleCondition(price, c => c.ClosePrice, false))
			{
				Name = LocalizedStrings.Str1067Params.Put(candle, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of candle closing price reduction below a specific level.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle closing price reduction below a specific level.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenClosePriceLess(this ICandleManager candleManager, Candle candle, Unit price)
		{
			return new ChangedCandleRule(candleManager, candle, candle.CreateCandleCondition(price, c => c.ClosePrice, true))
			{
				Name = LocalizedStrings.Str1068Params.Put(candle, price)
			};
		}

		private static Func<Candle, bool> CreateCandleCondition(this Candle candle, Unit price, Func<Candle, decimal> currentPrice, bool isLess)
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
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle total volume excess above a specific level.</param>
		/// <param name="volume">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenTotalVolumeMore(this ICandleManager candleManager, Candle candle, Unit volume)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			var finishVolume = volume.Type == UnitTypes.Limit ? volume : candle.TotalVolume + volume;

			return new ChangedCandleRule(candleManager, candle, c => c.TotalVolume > finishVolume)
			{
				Name = candle + LocalizedStrings.Str1069Params.Put(volume)
			};
		}

		/// <summary>
		/// To create a rule for the event of candle total volume excess above a specific level.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series, from which a candle will be taken.</param>
		/// <param name="volume">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCurrentCandleTotalVolumeMore(this ICandleManager candleManager, CandleSeries series, Unit volume)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var finishVolume = volume;

			if (volume.Type != UnitTypes.Limit)
			{
				var curCandle = candleManager.GetCurrentCandle<Candle>(series);

				if (curCandle == null)
					throw new ArgumentException(LocalizedStrings.Str1070, nameof(series));

				finishVolume = curCandle.TotalVolume + volume;	
			}

			return new CurrentCandleSeriesRule(candleManager, series, candle => candle.TotalVolume > finishVolume)
			{
				Name = series + LocalizedStrings.Str1071Params.Put(volume)
			};
		}

		/// <summary>
		/// To create a rule for the event of new candles occurrence.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for new candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandlesStarted(this ICandleManager candleManager, CandleSeries series)
		{
			return new CandleStartedRule(candleManager, series) { Name = LocalizedStrings.Str1072 + " " + series };
		}

		/// <summary>
		/// To create a rule for candle change event.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for changed candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandlesChanged(this ICandleManager candleManager, CandleSeries series)
		{
			return new CandleChangedSeriesRule(candleManager, series);
		}

		/// <summary>
		/// To create a rule for candles end event.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for end of candle.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandlesFinished(this ICandleManager candleManager, CandleSeries series)
		{
			return new CandleStateSeriesRule(candleManager, series, CandleStates.Finished) { Name = LocalizedStrings.Str1073 + " " + series };
		}

		/// <summary>
		/// To create a rule for the event of candles occurrence, change and end.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandles(this ICandleManager candleManager, CandleSeries series)
		{
			return new CandleStateSeriesRule(candleManager, series, CandleStates.Active, CandleStates.Finished)
			{
				Name = LocalizedStrings.Candles + " " + series
			};
		}

		/// <summary>
		/// To create a rule for candle change event.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for change.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenChanged(this ICandleManager candleManager, Candle candle)
		{
			return new ChangedCandleRule(candleManager, candle);
		}

		/// <summary>
		/// To create a rule for candle end event.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for end.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenFinished(this ICandleManager candleManager, Candle candle)
		{
			return new FinishedCandleRule(candleManager, candle).Once();
		}

		/// <summary>
		/// To create a rule for the event of candle partial end.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for partial end.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="percent">The percentage of candle completion.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenPartiallyFinished(this ICandleManager candleManager, Candle candle, IConnector connector, decimal percent)
		{
			var rule = (candle is TimeFrameCandle)
						? (MarketRule<Candle, Candle>)new TimeFrameCandleChangedRule(candle, connector, percent)
			           	: new ChangedCandleRule(candleManager, candle, c => c.IsCandlePartiallyFinished(percent));

			rule.Name = LocalizedStrings.Str1075Params.Put(percent);
			return rule;
		}

		/// <summary>
		/// To create a rule for the event of candle partial end.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">The candle series to be traced for candle partial end.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="percent">The percentage of candle completion.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenPartiallyFinishedCandles(this ICandleManager candleManager, CandleSeries series, IConnector connector, decimal percent)
		{
			var rule = (series.CandleType == typeof(TimeFrameCandle))
				? (MarketRule<CandleSeries, Candle>)new TimeFrameCandlesChangedSeriesRule(candleManager, series, connector, percent)
				: new CandleChangedSeriesRule(candleManager, series, candle => candle.IsCandlePartiallyFinished(percent));

			rule.Name = LocalizedStrings.Str1076Params.Put(percent);
			return rule;
		}

		private sealed class TimeFrameCandleChangedRule : MarketRule<Candle, Candle>
		{
			private readonly MarketTimer _timer;

			public TimeFrameCandleChangedRule(Candle candle, IConnector connector, decimal percent)
				: base(candle)
			{
				_timer = CreateAndActivateTimeFrameTimer(candle.Security, (TimeSpan)candle.Arg, connector, () => Activate(candle), percent, false);
			}

			protected override void DisposeManaged()
			{
				_timer.Dispose();
				base.DisposeManaged();
			}
		}

		private sealed class TimeFrameCandlesChangedSeriesRule : BaseCandleSeriesRule<Candle>
		{
			private readonly MarketTimer _timer;

			public TimeFrameCandlesChangedSeriesRule(ICandleManager candleManager, CandleSeries series, IConnector connector, decimal percent)
				: base(series)
			{
				if (candleManager == null)
					throw new ArgumentNullException(nameof(candleManager));

				_timer = CreateAndActivateTimeFrameTimer(series.Security, (TimeSpan)series.Arg, connector, () => Activate(candleManager.GetCurrentCandle<Candle>(Series)), percent, true);
			}

			protected override void DisposeManaged()
			{
				_timer.Dispose();
				base.DisposeManaged();
			}
		}

		private static MarketTimer CreateAndActivateTimeFrameTimer(Security security, TimeSpan timeFrame, IConnector connector, Action callback, decimal percent, bool periodical)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

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
			var candleBounds = timeFrame.GetCandleBounds(time, security.Board);

			percent = percent / 100;

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

		private static bool IsCandlePartiallyFinished(this Candle candle, decimal percent)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			if (percent <= 0)
				throw new ArgumentOutOfRangeException(nameof(percent), LocalizedStrings.Str1077);

			var realPercent = percent / 100;

			var type = candle.GetType();

			if (type == typeof(TickCandle))
			{
				var tickCandle = (TickCandle)candle;
				var count = realPercent * (int)candle.Arg;
				return tickCandle.TotalTicks != null && tickCandle.TotalTicks.Value >= count;
			}
			else if (type == typeof(RangeCandle))
			{
				return (decimal)(candle.LowPrice + (Unit)candle.Arg) >= realPercent * candle.HighPrice;
			}
			else if (type == typeof(VolumeCandle))
			{
				var volume = realPercent * (decimal)candle.Arg;
				return candle.TotalVolume >= volume;
			}
			else
				throw new ArgumentOutOfRangeException(nameof(candle), type, LocalizedStrings.WrongCandleType);
		}
	}
}
