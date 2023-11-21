namespace StockSharp.Algo
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using Ecng.ComponentModel;
	using System.Xml.Linq;

	partial class MarketRuleHelper
	{
		private abstract class BaseCandleSeriesRule<TCandle> : MarketRule<Subscription, TCandle>
			where TCandle : ICandleMessage
		{
			protected BaseCandleSeriesRule(Subscription subscription)
				: base(subscription)
			{
				Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
			}

			protected Subscription Subscription { get; }

			protected void Activate(ICandleMessage candle)
			{
				if (candle is TCandle typedCandle)
					base.Activate(typedCandle);
				else
				{
#pragma warning disable CS0618 // Type or member is obsolete
					base.Activate(((CandleMessage)candle).ToCandle(Subscription.CandleSeries.Security).To<TCandle>());
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}
		}

		private abstract class CandleSeriesRule<TCandle> : BaseCandleSeriesRule<TCandle>
			where TCandle : ICandleMessage
		{
			private readonly ISubscriptionProvider _subscriptionProvider;

			protected CandleSeriesRule(ISubscriptionProvider subscriptionProvider, Subscription subscription)
				: base(subscription)
			{
				_subscriptionProvider = subscriptionProvider ?? throw new ArgumentNullException(nameof(subscriptionProvider));
				_subscriptionProvider.CandleReceived += OnProcessing;
			}

			private void OnProcessing(Subscription subscription, ICandleMessage candle)
			{
				if (Subscription != subscription

					// for backward compatibility (old code used CandleSeries rules)
					&& Subscription.CandleSeries != subscription.CandleSeries
				)
					return;

				OnProcessCandle(candle);
			}

			protected abstract void OnProcessCandle(ICandleMessage candle);

			protected override void DisposeManaged()
			{
				_subscriptionProvider.CandleReceived -= OnProcessing;
				base.DisposeManaged();
			}
		}

		private class CandleStateSeriesRule<TCandle> : CandleSeriesRule<TCandle>
			where TCandle : ICandleMessage
		{
			private readonly CandleStates _state;
			private readonly CandleStates[] _states;

			public CandleStateSeriesRule(ISubscriptionProvider subscriptionProvider, Subscription subscription, params CandleStates[] states)
				: base(subscriptionProvider, subscription)
			{
				if (states == null)
					throw new ArgumentNullException(nameof(states));

				if (states.IsEmpty())
					throw new ArgumentOutOfRangeException(nameof(states));

				_state = states[0];

				if (states.Length > 1)
					_states = states;
			}

			protected override void OnProcessCandle(ICandleMessage candle)
			{
				if ((_states == null && candle.State == _state) || (_states != null && _states.Contains(candle.State)))
					Activate(candle);
			}
		}

		private class CandleStartedRule<TCandle> : CandleSeriesRule<TCandle>
			where TCandle : ICandleMessage
		{
			private ICandleMessage _currCandle;

			public CandleStartedRule(ISubscriptionProvider subscriptionProvider, Subscription subscription)
				: base(subscriptionProvider, subscription)
			{
			}

			protected override void OnProcessCandle(ICandleMessage candle)
			{
				if (_currCandle?.IsSame(candle) == true)
					return;

				_currCandle = candle;
				Activate(candle);
			}
		}

		private class CandleChangedSeriesRule<TCandle> : CandleSeriesRule<TCandle>
			where TCandle : ICandleMessage
		{
			private readonly Func<ICandleMessage, bool> _condition;

			public CandleChangedSeriesRule(ISubscriptionProvider subscriptionProvider, Subscription subscription)
				: this(subscriptionProvider, subscription, c => true)
			{
			}

			public CandleChangedSeriesRule(ISubscriptionProvider subscriptionProvider, Subscription subscription, Func<ICandleMessage, bool> condition)
				: base(subscriptionProvider, subscription)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
				Name = LocalizedStrings.Candles + " " + subscription;
			}

			protected override void OnProcessCandle(ICandleMessage candle)
			{
				if (candle.State == CandleStates.Active && _condition(candle))
					Activate(candle);
			}
		}

		private class CurrentCandleSeriesRule<TCandle> : CandleSeriesRule<TCandle>
			where TCandle : ICandleMessage
		{
			private readonly Func<ICandleMessage, bool> _condition;

			public CurrentCandleSeriesRule(ISubscriptionProvider subscriptionProvider, Subscription subscription, Func<ICandleMessage, bool> condition)
				: base(subscriptionProvider, subscription)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
			}

			protected override void OnProcessCandle(ICandleMessage candle)
			{
				if (candle.State == CandleStates.Active && _condition(candle))
					Activate(candle);
			}
		}

		private abstract class CandleRule<TCandle> : MarketRule<TCandle, TCandle>
			where TCandle : ICandleMessage
		{
			private readonly ISubscriptionProvider _subscriptionProvider;

			protected CandleRule(ISubscriptionProvider subscriptionProvider, TCandle candle)
				: base(candle)
			{
				_subscriptionProvider = subscriptionProvider ?? throw new ArgumentNullException(nameof(subscriptionProvider));
				_subscriptionProvider.CandleReceived += OnProcessing;

				Candle = candle;
			}

			private void OnProcessing(Subscription subscription, ICandleMessage candle)
			{
				if (!Candle.IsSame(candle))
					return;

				OnProcessCandle(candle);
			}

			protected abstract void OnProcessCandle(ICandleMessage candle);

			protected override void DisposeManaged()
			{
				_subscriptionProvider.CandleReceived -= OnProcessing;
				base.DisposeManaged();
			}

			protected TCandle Candle { get; }
		}

		private sealed class ChangedCandleRule<TCandle> : CandleRule<TCandle>
			where TCandle : ICandleMessage
		{
			private readonly Func<TCandle, bool> _condition;

			public ChangedCandleRule(ISubscriptionProvider subscriptionProvider, TCandle candle)
				: this(subscriptionProvider, candle, c => true)
			{
			}

			public ChangedCandleRule(ISubscriptionProvider subscriptionProvider, TCandle candle, Func<TCandle, bool> condition)
				: base(subscriptionProvider, candle)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
				Name = LocalizedStrings.Candles + " " + candle;
			}

			protected override void OnProcessCandle(ICandleMessage candle)
			{
				if (candle.State == CandleStates.Active && Candle.IsSame(candle) && _condition(Candle))
					Activate(Candle);
			}
		}

		private sealed class FinishedCandleRule<TCandle> : CandleRule<TCandle>
			where TCandle : ICandleMessage
		{
			public FinishedCandleRule(ISubscriptionProvider subscriptionProvider, TCandle candle)
				: base(subscriptionProvider, candle)
			{
				Name = LocalizedStrings.Candles + " " + candle;
			}

			protected override void OnProcessCandle(ICandleMessage candle)
			{
				if (candle.State == CandleStates.Finished && candle.IsSame(Candle))
					Activate(Candle);
			}
		}

		/// <summary>
		/// To create a rule for the event of candle closing price excess above a specific level.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle closing price excess above a specific level.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenClosePriceMore<TCandle>(this ISubscriptionProvider subscriptionProvider, TCandle candle, Unit price)
			where TCandle : ICandleMessage
		{
			return new ChangedCandleRule<TCandle>(subscriptionProvider, candle, candle.CreateCandleCondition(price, c => c.ClosePrice, false))
			{
				Name = $"({candle.SecurityId}) C > {price}"
			};
		}

		/// <summary>
		/// To create a rule for the event of candle closing price reduction below a specific level.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle closing price reduction below a specific level.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenClosePriceLess<TCandle>(this ISubscriptionProvider subscriptionProvider, TCandle candle, Unit price)
			where TCandle : ICandleMessage
		{
			return new ChangedCandleRule<TCandle>(subscriptionProvider, candle, candle.CreateCandleCondition(price, c => c.ClosePrice, true))
			{
				Name = $"({candle.SecurityId}) C < {price}"
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

			if (price <= 0)
				throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.InvalidValue);

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
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle total volume excess above a specific level.</param>
		/// <param name="volume">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenTotalVolumeMore<TCandle>(this ISubscriptionProvider subscriptionProvider, TCandle candle, Unit volume)
			where TCandle : ICandleMessage
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			var finishVolume = volume.Type == UnitTypes.Limit ? volume : candle.TotalVolume + volume;

			return new ChangedCandleRule<TCandle>(subscriptionProvider, candle, c => c.TotalVolume > finishVolume)
			{
				Name = $"({candle.SecurityId}) V > {volume}"
			};
		}

		private static Subscription GetSubscription(ISubscriptionProvider subscriptionProvider, CandleSeries candleSeries)
		{
			if (subscriptionProvider is null)
				throw new ArgumentNullException(nameof(subscriptionProvider));

			if (candleSeries is null)
				throw new ArgumentNullException(nameof(candleSeries));

			return subscriptionProvider.Subscriptions.FirstOrDefault(s => s.CandleSeries == candleSeries) ?? new(candleSeries);
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
		/// To create a rule for the event of new candles occurrence.
		/// </summary>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="subscription">Candles series to be traced for new candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, ICandleMessage> WhenCandlesStarted(this ISubscriptionProvider subscriptionProvider, Subscription subscription)
			=> WhenCandlesStarted<ICandleMessage>(subscriptionProvider, subscription);

		/// <summary>
		/// To create a rule for the event of new candles occurrence.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="subscription">Candles series to be traced for new candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, TCandle> WhenCandlesStarted<TCandle>(this ISubscriptionProvider subscriptionProvider, Subscription subscription)
			where TCandle : ICandleMessage
			=> new CandleStartedRule<TCandle>(subscriptionProvider, subscription) { Name = LocalizedStrings.Candles + " " + subscription };

		/// <summary>
		/// To create a rule for candle change event.
		/// </summary>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="subscription">Candles series to be traced for changed candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, ICandleMessage> WhenCandlesChanged(this ISubscriptionProvider subscriptionProvider, Subscription subscription)
			=> WhenCandlesChanged<ICandleMessage>(subscriptionProvider, subscription);

		/// <summary>
		/// To create a rule for candle change event.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="subscription">Candles series to be traced for changed candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, TCandle> WhenCandlesChanged<TCandle>(this ISubscriptionProvider subscriptionProvider, Subscription subscription)
			where TCandle : ICandleMessage
			=> new CandleChangedSeriesRule<TCandle>(subscriptionProvider, subscription);

		/// <summary>
		/// To create a rule for candles end event.
		/// </summary>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="subscription">Candles series to be traced for end of candle.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, ICandleMessage> WhenCandlesFinished(this ISubscriptionProvider subscriptionProvider, Subscription subscription)
			=> WhenCandlesFinished<ICandleMessage>(subscriptionProvider, subscription);

		/// <summary>
		/// To create a rule for candles end event.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="subscription">Candles series to be traced for end of candle.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, TCandle> WhenCandlesFinished<TCandle>(this ISubscriptionProvider subscriptionProvider, Subscription subscription)
			where TCandle : ICandleMessage
			=> new CandleStateSeriesRule<TCandle>(subscriptionProvider, subscription, CandleStates.Finished) { Name = LocalizedStrings.Candles + " " + subscription };

		/// <summary>
		/// To create a rule for the event of candles occurrence, change and end.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="subscription">Candles series to be traced for candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, TCandle> WhenCandles<TCandle>(this ISubscriptionProvider subscriptionProvider, Subscription subscription)
			where TCandle : ICandleMessage
			=> new CandleStateSeriesRule<TCandle>(subscriptionProvider, subscription, CandleStates.Active, CandleStates.Finished)
			{
				Name = LocalizedStrings.Candles + " " + subscription
			};

		/// <summary>
		/// To create a rule for candle change event.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="candle">The candle to be traced for change.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenChanged<TCandle>(this ISubscriptionProvider subscriptionProvider, TCandle candle)
			where TCandle : ICandleMessage
			=> new ChangedCandleRule<TCandle>(subscriptionProvider, candle);

		/// <summary>
		/// To create a rule for candle end event.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="candle">The candle to be traced for end.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenFinished<TCandle>(this ISubscriptionProvider subscriptionProvider, TCandle candle)
			where TCandle : ICandleMessage
			=> new FinishedCandleRule<TCandle>(subscriptionProvider, candle).Once();

		/// <summary>
		/// To create a rule for the event of candle partial end.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="candle">The candle to be traced for partial end.</param>
		/// <param name="percent">The percentage of candle completion.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TCandle, TCandle> WhenPartiallyFinished<TCandle>(this ISubscriptionProvider subscriptionProvider, TCandle candle, decimal percent)
			where TCandle : ICandleMessage
			=> new ChangedCandleRule<TCandle>(subscriptionProvider, candle, c => c.IsCandlePartiallyFinished(percent))
			{
				Name = $"({candle.SecurityId}) {percent}%"
			};

		/// <summary>
		/// To create a rule for the event of candle partial end.
		/// </summary>
		/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
		/// <param name="subscriptionProvider">The subscription manager.</param>
		/// <param name="subscription">The candle series to be traced for candle partial end.</param>
		/// <param name="percent">The percentage of candle completion.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Subscription, TCandle> WhenPartiallyFinishedCandles<TCandle>(this ISubscriptionProvider subscriptionProvider, Subscription subscription, decimal percent)
			where TCandle : ICandleMessage
			=> new CandleChangedSeriesRule<TCandle>(subscriptionProvider, subscription, candle => candle.IsCandlePartiallyFinished(percent))
			{
				Name = $"({subscription.SecurityId}) {percent}%"
			};

		private static bool IsCandlePartiallyFinished<TCandle>(this TCandle candle, decimal percent)
			where TCandle : ICandleMessage
		{
			if (candle is null)
				throw new ArgumentNullException(nameof(candle));

			if (percent <= 0)
				throw new ArgumentOutOfRangeException(nameof(percent), percent, LocalizedStrings.InvalidValue);

			var realPercent = percent / 100;

			if (candle is ITickCandleMessage tickCandle)
			{
				var count = realPercent * (int)candle.DataType.Arg;
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
			{
				return candle.State == CandleStates.Finished;
				//throw new ArgumentOutOfRangeException(nameof(candle), candle.GetType(), LocalizedStrings.WrongCandleType);
			}
		}

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
	}
}