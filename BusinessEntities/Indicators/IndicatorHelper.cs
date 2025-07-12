namespace StockSharp.Algo.Indicators;

using Ecng.Reflection;

/// <summary>
/// Extension class for indicators.
/// </summary>
public static class IndicatorHelper
{
	/// <summary>
	/// To get the first value of the indicator.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <returns>The current value.</returns>
	public static decimal GetFirstValue(this IIndicator indicator)
	{
		return indicator.GetNullableFirstValue() ?? 0;
	}

	/// <summary>
	/// To get the first value of the indicator.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <returns>The current value.</returns>
	public static decimal? GetNullableFirstValue(this IIndicator indicator)
	{
		if (!(indicator.Container?.Count > 0))
			return null;

		return indicator.GetValue(indicator.Container.Count - 1);
	}

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
	/// To replace the indicator input value by new one (for example it is received from another indicator).
	/// </summary>
	/// <param name="input"><see cref="IIndicatorValue"/></param>
	/// <param name="indicator">Indicator.</param>
	/// <param name="value">Value.</param>
	/// <returns>New object, containing input value.</returns>
	public static IIndicatorValue SetValue(this IIndicatorValue input, IIndicator indicator, decimal value)
		=> new DecimalIndicatorValue(indicator, value, input.Time) { IsFinal = input.IsFinal };

	/// <summary>
	/// To renew the indicator with numeric value.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="input"><see cref="IIndicatorValue"/></param>
	/// <param name="value">Numeric value.</param>
	/// <returns>The new value of the indicator.</returns>
	public static IIndicatorValue Process(this IIndicator indicator, IIndicatorValue input, decimal value)
		=> indicator.Process(input.SetValue(indicator, value));

	/// <summary>
	/// To renew the indicator with candle closing price <see cref="ICandleMessage.ClosePrice"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="candle">Candle.</param>
	/// <returns>The new value of the indicator.</returns>
	public static IIndicatorValue Process(this IIndicator indicator, ICandleMessage candle)
		=> indicator.Process(new CandleIndicatorValue(indicator, candle));

	/// <summary>
	/// To renew the indicator with numeric value.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="value">Numeric value.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	/// <param name="isFinal">Is the value final (the indicator finally forms its value and will not be changed in this point of time anymore). Default is <see langword="true" />.</param>
	/// <returns>The new value of the indicator.</returns>
	public static IIndicatorValue Process(this IIndicator indicator, decimal value, DateTimeOffset time, bool isFinal)
		=> indicator.Process(new DecimalIndicatorValue(indicator, value, time) { IsFinal = isFinal });

	/// <summary>
	/// To renew the indicator with numeric pair.
	/// </summary>
	/// <typeparam name="TValue">Value type.</typeparam>
	/// <param name="indicator">Indicator.</param>
	/// <param name="value">The pair of values.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	/// <param name="isFinal">If the pair final (the indicator finally forms its value and will not be changed in this point of time anymore). Default is <see langword="true" />.</param>
	/// <returns>The new value of the indicator.</returns>
	public static IIndicatorValue Process<TValue>(this IIndicator indicator, (TValue, TValue) value, DateTimeOffset time, bool isFinal)
		=> indicator.Process(new PairIndicatorValue<TValue>(indicator, value, time) { IsFinal = isFinal });

	/// <summary>
	/// To renew the indicator with new value.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="inputValue">Input value.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	/// <param name="isFinal"><see cref="IIndicatorValue.IsFinal"/></param>
	/// <returns><see cref="IIndicatorValue"/>.</returns>
	public static IIndicatorValue Process(this IIndicator indicator, object inputValue, DateTimeOffset time, bool isFinal)
	{
		if (indicator == null)
			throw new ArgumentNullException(nameof(indicator));

		if (inputValue == null)
			throw new ArgumentNullException(nameof(inputValue));

		IIndicatorValue input = null;

		switch (inputValue)
		{
			case ICandleMessage c:
				input = new CandleIndicatorValue(indicator, c);
				break;
			case IIndicatorValue v:
				input = v;

				if (input.IsEmpty)
					return indicator.CreateEmptyValue(time);

				break;
			case Unit u:
				input = new DecimalIndicatorValue(indicator, u.Value, time) { IsFinal = isFinal };
				break;
			case ValueTuple<decimal, decimal> t:
				input = new PairIndicatorValue<decimal>(indicator, t, time) { IsFinal = isFinal };
				break;
			case IOrderBookMessage d:
				input = new MarketDepthIndicatorValue(indicator, d) { IsFinal = isFinal };
				break;
			case ITickTradeMessage t:
				input = new DecimalIndicatorValue(indicator, t.Price, time) { IsFinal = isFinal };
				break;
			case Level1ChangeMessage l1:
				input = new Level1IndicatorValue(indicator, l1) { IsFinal = isFinal };
				break;
			case bool b:
				input = new DecimalIndicatorValue(indicator, b ? 1 : 0, time) { IsFinal = isFinal };
				break;
		}

		if (input == null && inputValue.GetType().IsNumeric())
			input = new DecimalIndicatorValue(indicator, inputValue.To<decimal>(), time) { IsFinal = isFinal };

		if (input == null)
			throw new ArgumentException(LocalizedStrings.IndicatorNotWorkWithType.Put(inputValue.GetType().Name));

		return indicator.Process(input);
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

		IndicatorValueAttribute attr = isInput
			? indicatorType.GetAttribute<IndicatorInAttribute>()
			: indicatorType.GetAttribute<IndicatorOutAttribute>()
		;

		if (attr is null)
			throw new ArgumentException(LocalizedStrings.TypeNotImplemented.Put(indicatorType.Name, isInput ? nameof(IndicatorInAttribute) : nameof(IndicatorOutAttribute)), nameof(indicatorType));

		return attr.Type;
	}

	/// <summary>
	/// Convert <see cref="IIndicatorValue"/> to <see cref="decimal"/>.
	/// </summary>
	/// <param name="value"><see cref="IIndicatorValue"/></param>
	/// <returns><see cref="decimal"/></returns>
	public static decimal ToDecimal(this IIndicatorValue value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		return value.GetValue<decimal>();
	}

	/// <summary>
	/// Convert <see cref="IIndicatorValue"/> to <see cref="decimal"/> or <see langword="null"/> if the value is empty..
	/// </summary>
	/// <param name="value"><see cref="IIndicatorValue"/></param>
	/// <returns><see cref="decimal"/> or <see langword="null"/> if the value is empty.</returns>
	public static decimal? ToNullableDecimal(this IIndicatorValue value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		return value.IsEmpty ? null : value.ToDecimal();
	}

	/// <summary>
	/// Convert <see cref="IIndicatorValue"/> to <see cref="ICandleMessage"/>.
	/// </summary>
	/// <param name="value"><see cref="IIndicatorValue"/></param>
	/// <returns><see cref="ICandleMessage"/></returns>
	public static ICandleMessage ToCandle(this IIndicatorValue value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		return value.GetValue<ICandleMessage>();
	}

	/// <summary>
	/// Create empty <see cref="IIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	/// <returns>Empty <see cref="IIndicatorValue"/>.</returns>
	public static IIndicatorValue CreateEmptyValue(this IIndicator indicator, DateTimeOffset time)
	{
		if (indicator is null)
			throw new ArgumentNullException(nameof(indicator));

		return indicator.CreateValue(time, []);
	}

	/// <summary>
	/// Create indicator.
	/// </summary>
	/// <param name="type"><see cref="IndicatorType"/></param>
	/// <returns><see cref="IIndicator"/></returns>
	public static IIndicator CreateIndicator(this IndicatorType type)
		=> type.TryCreateIndicator() ?? throw new InvalidOperationException();

	/// <summary>
	/// Create indicator.
	/// </summary>
	/// <param name="type"><see cref="IndicatorType"/></param>
	/// <returns><see cref="IIndicator"/></returns>
	public static IIndicator TryCreateIndicator(this IndicatorType type)
		=> type.CheckOnNull(nameof(type)).Indicator?.CreateInstance<IIndicator>();

	/// <summary>
	/// Exclude obsolete indicators.
	/// </summary>
	/// <param name="types">All indicator types.</param>
	/// <returns>Filtered collection.</returns>
	public static IEnumerable<IndicatorType> ExcludeObsolete(this IEnumerable<IndicatorType> types)
		=> types.CheckOnNull(nameof(types)).Where(t => !t.IsObsolete);

	/// <summary>
	/// Try find <see cref="IndicatorType"/> by identifier.
	/// </summary>
	/// <param name="provider"><see cref="IIndicatorProvider"/></param>
	/// <param name="id">Identifier.</param>
	/// <returns><see cref="IndicatorType"/> or <see langword="null"/>.</returns>
	public static IndicatorType TryGetById(this IIndicatorProvider provider, string id)
		=> provider.CheckOnNull(nameof(provider)).All.FirstOrDefault(it => it.Id == id);

	/// <summary>
	/// Try find <see cref="IndicatorType"/> by type.
	/// </summary>
	/// <param name="provider"><see cref="IIndicatorProvider"/></param>
	/// <param name="type"><see cref="IndicatorType.Indicator"/></param>
	/// <returns><see cref="IndicatorType"/> or <see langword="null"/>.</returns>
	public static IndicatorType TryGetByType(this IIndicatorProvider provider, Type type)
		=> provider.CheckOnNull(nameof(provider)).All.FirstOrDefault(it => it.Indicator == type);

	/// <summary>
	/// Determines whether the indicator is a non-decimal output value.
	/// </summary>
	/// <param name="indicator">Indicator type.</param>
	/// <returns>Check result.</returns>
	public static bool IsNonDecimalOutputValue(this Type indicator)
	{
		if (indicator is null)
			throw new ArgumentNullException(nameof(indicator));

		return indicator.GetValueType(false) != typeof(DecimalIndicatorValue);
	}
}