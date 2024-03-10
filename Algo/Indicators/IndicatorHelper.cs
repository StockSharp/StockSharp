#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: IndicatorHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Extension class for indicators.
	/// </summary>
	public static class IndicatorHelper
	{
		/// <summary>
		/// To get the current value of the indicator.
		/// </summary>
		/// <param name="indicator">Indicator.</param>
		/// <returns>The current value.</returns>
		public static decimal GetCurrentValue(this IIndicator indicator)
		{
			return indicator.GetNullableCurrentValue() ?? 0;
		}

		/// <summary>
		/// To get the current value of the indicator.
		/// </summary>
		/// <param name="indicator">Indicator.</param>
		/// <returns>The current value.</returns>
		public static decimal? GetNullableCurrentValue(this IIndicator indicator)
		{
			if (indicator == null)
				throw new ArgumentNullException(nameof(indicator));

			return indicator.GetCurrentValue<decimal?>();
		}

		/// <summary>
		/// To get the current value of the indicator.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="indicator">Indicator.</param>
		/// <returns>The current value.</returns>
		public static T GetCurrentValue<T>(this IIndicator indicator)
		{
			if (indicator == null)
				throw new ArgumentNullException(nameof(indicator));

			return indicator.GetValue<T>(0);
		}

		/// <summary>
		/// To get the indicator value by the index (0 - last value).
		/// </summary>
		/// <param name="indicator">Indicator.</param>
		/// <param name="index">The value index.</param>
		/// <returns>Indicator value.</returns>
		public static decimal GetValue(this IIndicator indicator, int index)
		{
			return indicator.GetNullableValue(index) ?? 0;
		}

		/// <summary>
		/// To get the indicator value by the index (0 - last value).
		/// </summary>
		/// <param name="indicator">Indicator.</param>
		/// <param name="index">The value index.</param>
		/// <returns>Indicator value.</returns>
		public static decimal? GetNullableValue(this IIndicator indicator, int index)
		{
			if (indicator == null)
				throw new ArgumentNullException(nameof(indicator));

			return indicator.GetValue<decimal?>(index);
		}

		/// <summary>
		/// To get the indicator value by the index (0 - last value).
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="indicator">Indicator.</param>
		/// <param name="index">The value index.</param>
		/// <returns>Indicator value.</returns>
		public static T GetValue<T>(this IIndicator indicator, int index)
		{
			if (indicator == null)
				throw new ArgumentNullException(nameof(indicator));

			var container = indicator.Container;

			if (index >= container.Count)
				return default;

			var value = container.GetValue(index).output;

			if (value.IsEmpty)
			{
				if (value is T t)
					return t;

				return default;
			}

			return typeof(T).Is<IIndicatorValue>() ? value.To<T>() : value.GetValue<T>();
		}

		/// <summary>
		/// To renew the indicator with candle closing price <see cref="ICandleMessage.ClosePrice"/>.
		/// </summary>
		/// <param name="indicator">Indicator.</param>
		/// <param name="candle">Candle.</param>
		/// <returns>The new value of the indicator.</returns>
		public static IIndicatorValue Process(this IIndicator indicator, ICandleMessage candle)
		{
			return indicator.Process(new CandleIndicatorValue(indicator, candle));
		}

		/// <summary>
		/// To renew the indicator with numeric value.
		/// </summary>
		/// <param name="indicator">Indicator.</param>
		/// <param name="value">Numeric value.</param>
		/// <param name="isFinal">Is the value final (the indicator finally forms its value and will not be changed in this point of time anymore). Default is <see langword="true" />.</param>
		/// <returns>The new value of the indicator.</returns>
		public static IIndicatorValue Process(this IIndicator indicator, decimal value, bool isFinal = true)
		{
			return indicator.Process(new DecimalIndicatorValue(indicator, value) { IsFinal = isFinal });
		}

		/// <summary>
		/// To renew the indicator with numeric pair.
		/// </summary>
		/// <typeparam name="TValue">Value type.</typeparam>
		/// <param name="indicator">Indicator.</param>
		/// <param name="value">The pair of values.</param>
		/// <param name="isFinal">If the pair final (the indicator finally forms its value and will not be changed in this point of time anymore). Default is <see langword="true" />.</param>
		/// <returns>The new value of the indicator.</returns>
		public static IIndicatorValue Process<TValue>(this IIndicator indicator, Tuple<TValue, TValue> value, bool isFinal = true)
		{
			return indicator.Process(new PairIndicatorValue<TValue>(indicator, value) { IsFinal = isFinal });
		}

		/// <summary>
		/// Get value type for specified indicator.
		/// </summary>
		/// <param name="indicatorType">Indicator type.</param>
		/// <param name="isInput">Is input.</param>
		/// <returns>Value type.</returns>
		public static Type GetValueType(this Type indicatorType, bool isInput)
		{
			if (indicatorType == null)
				throw new ArgumentNullException(nameof(indicatorType));

			if (!indicatorType.Is<IIndicator>())
				throw new ArgumentException(LocalizedStrings.TypeNotImplemented.Put(indicatorType.Name, nameof(IIndicator)), nameof(indicatorType));

			return (isInput
					? (IndicatorValueAttribute)indicatorType.GetAttribute<IndicatorInAttribute>()
					: indicatorType.GetAttribute<IndicatorOutAttribute>()
				)?.Type ?? typeof(DecimalIndicatorValue);
		}

		/// <summary>
		/// Does value support data type, required for the indicator.
		/// </summary>
		/// <typeparam name="T">The data type, operated by indicator.</typeparam>
		/// <param name="value"><see cref="IIndicatorValue"/></param>
		/// <returns><see langword="true" />, if data type is supported, otherwise, <see langword="false" />.</returns>
		public static bool IsSupport<T>(this IIndicatorValue value)
			=> value.CheckOnNull(nameof(value)).IsSupport(typeof(T));

		/// <summary>
		/// Get OHLC.
		/// </summary>
		/// <param name="value"><see cref="IIndicatorValue"/></param>
		/// <returns>OHLC.</returns>
		public static (decimal o, decimal h, decimal l, decimal c) GetOhlc(this IIndicatorValue value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (value.IsSupport<ICandleMessage>())
			{
				var candle = value.GetValue<ICandleMessage>();
				return (candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
			}
			else
			{
				var dec = value.GetValue<decimal>();
				return (dec, dec, dec, dec);
			}
		}

		/// <summary>
		/// Get OHLCV.
		/// </summary>
		/// <param name="value"><see cref="IIndicatorValue"/></param>
		/// <returns>OHLCV.</returns>
		public static (decimal o, decimal h, decimal l, decimal c, decimal v) GetOhlcv(this IIndicatorValue value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (value.IsSupport<ICandleMessage>())
			{
				var candle = value.GetValue<ICandleMessage>();
				return (candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume);
			}
			else
			{
				var dec = value.GetValue<decimal>();
				return (dec, dec, dec, dec, 0);
			}
		}
	}
}