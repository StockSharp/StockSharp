namespace StockSharp.Algo.Indicators;

using System.Reflection;

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
	public static IIndicatorValue Process(this IIndicator indicator, decimal value, DateTime time, bool isFinal)
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
	public static IIndicatorValue Process<TValue>(this IIndicator indicator, (TValue, TValue) value, DateTime time, bool isFinal)
		=> indicator.Process(new PairIndicatorValue<TValue>(indicator, value, time) { IsFinal = isFinal });

	/// <summary>
	/// To renew the indicator with new value.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="inputValue">Input value.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	/// <param name="isFinal"><see cref="IIndicatorValue.IsFinal"/></param>
	/// <returns><see cref="IIndicatorValue"/>.</returns>
	public static IIndicatorValue Process(this IIndicator indicator, object inputValue, DateTime time, bool isFinal)
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
	/// <param name="source">Field specified value source.</param>
	/// <returns><see cref="decimal"/></returns>
	public static decimal ToDecimal(this IIndicatorValue value, Level1Fields? source = default)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		return value.GetValue<decimal>(source ?? value.Indicator.Source);
	}

	/// <summary>
	/// Convert <see cref="IIndicatorValue"/> to <see cref="decimal"/> or <see langword="null"/> if the value is empty..
	/// </summary>
	/// <param name="value"><see cref="IIndicatorValue"/></param>
	/// <param name="source">Field specified value source.</param>
	/// <returns><see cref="decimal"/> or <see langword="null"/> if the value is empty.</returns>
	public static decimal? ToNullableDecimal(this IIndicatorValue value, Level1Fields? source = default)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		return value.IsEmpty ? null : value.ToDecimal(source);
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
	public static IIndicatorValue CreateEmptyValue(this IIndicator indicator, DateTime time)
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

	/// <summary>
	/// Bulk preload using primitive tuples. Implementations may override; default throws <see cref="NotSupportedException"/>.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <param name="items">Sequence of (time, values) where values correspond to <see cref="IIndicatorValue.ToValues"/> output.</param>
	public static void Preload(this IIndicator indicator, IEnumerable<(DateTime time, object[] values)> items)
	{
		ArgumentNullException.ThrowIfNull(indicator);
		ArgumentNullException.ThrowIfNull(items);

		indicator.Preload(items.Select(t =>
		{
			var time = t.time;
			var values = t.values;

			var input = indicator.CreateValue(time, []);
			var output = indicator.CreateValue(time, values);

			input.IsFinal = output.IsFinal = true;

			return (input, output);
		}));
	}

	// What every indicator has regardless of its kind is the contract itself; a caller sets what the
	// indicator declares on top of it.
	private static readonly HashSet<string> _contractProps = [.. typeof(IIndicator)
		.GetProperties()
		.Select(p => p.Name)];

	/// <summary>
	/// The series <paramref name="indicator"/> produces: one entry for a plain indicator, one per
	/// innermost part for a composite.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <returns>Series name paired with the part whose value it carries, in a stable order.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="indicator"/> is <see langword="null"/>.</exception>
	public static IReadOnlyList<(string Name, IIndicator Part)> GetOutputs(this IIndicator indicator)
	{
		if (indicator is null)
			throw new ArgumentNullException(nameof(indicator));

		var outputs = new List<(string, IIndicator)>();
		CollectOutputs(indicator, null, outputs);
		return outputs;
	}

	/// <summary>
	/// Reads one number per series out of <paramref name="value"/>.
	/// </summary>
	/// <param name="value">What <see cref="IIndicator.Process"/> returned. May be <see langword="null"/>.</param>
	/// <param name="parts">The parts from <see cref="GetOutputs"/>, in the same order.</param>
	/// <returns>
	/// One entry per part, <see langword="null"/> where that series has not formed yet, and the
	/// bars-back offset when a sparse indicator reports its value later than the candle it belongs to.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="parts"/> is <see langword="null"/>.</exception>
	public static (decimal?[] Values, int? Shift) GetOutputValues(this IIndicatorValue value, IReadOnlyList<IIndicator> parts)
	{
		if (parts is null)
			throw new ArgumentNullException(nameof(parts));

		var values = new decimal?[parts.Count];

		if (value is null || value.IsEmpty)
			return (values, null);

		var byIndicator = new Dictionary<IIndicator, IIndicatorValue>(ReferenceEqualityComparer.Instance);
		CollectValues(value, byIndicator);

		int? shift = null;

		for (var i = 0; i < parts.Count; i++)
		{
			// Not formed means the warm-up window has not filled. The value is not empty at that
			// point - a ten-bar average over three bars is a real number - it is just not the
			// average that was asked for, and a caller plotting it plots something else.
			if (!byIndicator.TryGetValue(parts[i], out var part) || part is null || part.IsEmpty || !part.IsFormed)
				continue;

			values[i] = TryToDecimal(part);

			if (shift is null && part is IShiftedIndicatorValue shifted && shifted.Shift > 0)
				shift = shifted.Shift;
		}

		return (values, shift);
	}

	/// <summary>
	/// The properties of <paramref name="indicatorType"/> a caller may set: what the indicator
	/// declares itself, as opposed to the identity and drawing hints it inherits.
	/// </summary>
	/// <param name="indicatorType">Indicator type.</param>
	/// <returns>Settable parameter properties.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="indicatorType"/> is <see langword="null"/>.</exception>
	public static IEnumerable<PropertyInfo> GetParameters(this Type indicatorType)
	{
		if (indicatorType is null)
			throw new ArgumentNullException(nameof(indicatorType));

		foreach (var prop in indicatorType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (prop.GetSetMethod() is null || prop.GetIndexParameters().Length > 0 || _contractProps.Contains(prop.Name))
				continue;

			var type = prop.PropertyType.GetUnderlyingType() ?? prop.PropertyType;

			if (type == typeof(int) || type == typeof(long) || type == typeof(decimal)
				|| type == typeof(double) || type == typeof(float) || type == typeof(bool))
				yield return prop;
		}
	}

	/// <summary>
	/// Applies <paramref name="parameters"/> to <paramref name="indicator"/>, keyed by property name
	/// and matched case-insensitively. A part of a composite is reached by naming the path to it -
	/// <c>K.Length</c>, <c>Macd.LongMa.Length</c>. A key the indicator does not have, or a value it
	/// refuses, leaves its own default in place.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="parameters">Parameters.</param>
	/// <exception cref="ArgumentNullException"><paramref name="indicator"/> is <see langword="null"/>.</exception>
	public static void ApplyParameters(this IIndicator indicator, IEnumerable<KeyValuePair<string, object>> parameters)
	{
		if (indicator is null)
			throw new ArgumentNullException(nameof(indicator));

		if (parameters is null)
			return;

		foreach (var (key, raw) in parameters)
		{
			if (raw is null || key.IsEmptyOrWhiteSpace())
				continue;

			var path = key.Split('.');
			var target = indicator;

			// Everything but the last segment names a part to descend into.
			for (var i = 0; i < path.Length - 1 && target is not null; i++)
				target = FindPart(target, path[i]);

			if (target is null)
				continue;

			var prop = target.GetType().GetParameters().FirstOrDefault(p => p.Name.EqualsIgnoreCase(path[^1]));

			if (prop is null)
				continue;

			try
			{
				prop.SetValue(target, ConvertParameter(raw, prop.PropertyType.GetUnderlyingType() ?? prop.PropertyType));
			}
			catch (Exception)
			{
				// Out of range, or not convertible to what the property holds.
			}
		}
	}

	private static IIndicator FindPart(IIndicator indicator, string name)
	{
		var prop = indicator
			.GetType()
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.FirstOrDefault(p => p.PropertyType.Is<IIndicator>() && p.GetIndexParameters().Length == 0 && p.Name.EqualsIgnoreCase(name));

		try
		{
			return prop?.GetValue(indicator) as IIndicator;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static object ConvertParameter(object raw, Type type)
	{
		if (raw.GetType() == type)
			return raw;

		try
		{
			return raw.To(type);
		}
		catch (Exception)
		{
			// A value that came in as JSON can be a wrapper convertible only through its text form:
			// System.Text.Json hands over a JsonElement, which is not IConvertible.
			return raw.ToString().To(type);
		}
	}

	private static void CollectOutputs(IIndicator indicator, string path, List<(string, IIndicator)> outputs)
	{
		if (indicator is not IComplexIndicator complex || complex.InnerIndicators.Count == 0)
		{
			outputs.Add((path ?? "value", indicator));
			return;
		}

		var names = NameInnerParts(complex);

		foreach (var inner in complex.InnerIndicators)
		{
			var name = names[inner];
			CollectOutputs(inner, path.IsEmpty() ? name : $"{path}.{name}", outputs);
		}
	}

	/// <summary>
	/// Names the inner parts after the properties exposing them - <c>UpBand</c>, <c>SignalMa</c>,
	/// <c>Dx</c>. Their own <see cref="IIndicator.Name"/> is localized display text, which would
	/// make the series keys change with the language the process is running in.
	/// </summary>
	private static Dictionary<IIndicator, string> NameInnerParts(IComplexIndicator complex)
	{
		var byProperty = new Dictionary<IIndicator, string>(ReferenceEqualityComparer.Instance);

		foreach (var prop in complex.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (!prop.PropertyType.Is<IIndicator>() || prop.GetIndexParameters().Length > 0)
				continue;

			try
			{
				if (prop.GetValue(complex) is IIndicator inner && !byProperty.ContainsKey(inner))
					byProperty.Add(inner, prop.Name);
			}
			catch (Exception)
			{
				// A property that throws describes nothing; the fallback below names the part.
			}
		}

		var names = new Dictionary<IIndicator, string>(ReferenceEqualityComparer.Instance);
		var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		for (var i = 0; i < complex.InnerIndicators.Count; i++)
		{
			var inner = complex.InnerIndicators[i];

			if (!byProperty.TryGetValue(inner, out var name))
				name = inner.GetType().Name;

			if (!taken.Add(name))
			{
				name = $"{name}{i}";
				taken.Add(name);
			}

			names.Add(inner, name);
		}

		return names;
	}

	private static void CollectValues(IIndicatorValue value, Dictionary<IIndicator, IIndicatorValue> map)
	{
		if (value is null)
			return;

		if (value.Indicator is not null)
			map[value.Indicator] = value;

		if (value is IComplexIndicatorValue complex)
		{
			foreach (var inner in complex.InnerValues.Values)
				CollectValues(inner, map);
		}
	}

	private static decimal? TryToDecimal(IIndicatorValue value)
	{
		try
		{
			return value.ToNullableDecimal(value.Indicator?.Source);
		}
		catch (NotSupportedException)
		{
			// A composite holding no number of its own for this bar.
			return null;
		}
	}
}
