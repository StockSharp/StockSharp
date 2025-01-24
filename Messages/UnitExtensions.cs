namespace StockSharp.Messages;

/// <summary>
/// Extension class for <see cref="Unit"/>.
/// </summary>
public static class UnitExtensions
{
	/// <summary>
	/// Convert the <see cref="int"/> to percents.
	/// </summary>
	/// <param name="value"><see cref="int"/> value.</param>
	/// <returns>Percents.</returns>
	public static Unit Percents(this int value) => Percents((decimal)value);

	/// <summary>
	/// Convert the <see cref="long"/> to percents.
	/// </summary>
	/// <param name="value"><see cref="long"/> value.</param>
	/// <returns>Percents.</returns>
	public static Unit Percents(this long value) => Percents((decimal)value);

	/// <summary>
	/// Convert the <see cref="double"/> to percents.
	/// </summary>
	/// <param name="value"><see cref="double"/> value.</param>
	/// <returns>Percents.</returns>
	public static Unit Percents(this double value) => Percents((decimal)value);

	/// <summary>
	/// Convert the <see cref="decimal"/> to percents.
	/// </summary>
	/// <param name="value"><see cref="decimal"/> value.</param>
	/// <returns>Percents.</returns>
	public static Unit Percents(this decimal value) => new(value, UnitTypes.Percent);

	/// <summary>
	/// Convert string to <see cref="Unit"/>.
	/// </summary>
	/// <param name="str">String value of <see cref="Unit"/>.</param>
	/// <param name="throwIfNull">Throw <see cref="ArgumentNullException"/> if the specified string is empty.</param>
	/// <param name="getTypeValue">The handler returns a value associated with <see cref="Type"/> (price or volume steps).</param>
	/// <returns>Object <see cref="Unit"/>.</returns>
	public static Unit ToUnit(this string str, bool throwIfNull = true, Func<UnitTypes, decimal?> getTypeValue = null)
	{
		if (str.IsEmpty())
		{
			if (throwIfNull)
				throw new ArgumentNullException(nameof(str));

			return null;
		}

		var lastSymbol = str.Last();

		if (char.IsDigit(lastSymbol))
			return new Unit(str.To<decimal>(), UnitTypes.Absolute);

		var value = str.Substring(0, str.Length - 1).To<decimal>();

		UnitTypes type;

		switch (char.ToLowerInvariant(lastSymbol))
		{
			case 'ш':
			case 's':
				if (getTypeValue is null)
					throw new ArgumentNullException(nameof(getTypeValue));

				type = UnitTypes.Step;
				break;
			case 'п':
			case 'p':
				if (getTypeValue is null)
					throw new ArgumentNullException(nameof(getTypeValue));

				type = UnitTypes.Point;
				break;
			case '%':
				type = UnitTypes.Percent;
				break;
			case 'л':
			case 'l':
				type = UnitTypes.Limit;
				break;
			default:
				throw new ArgumentException(LocalizedStrings.UnknownUnitMeasurement.Put(lastSymbol), nameof(str));
		}

		return new Unit(value, type, getTypeValue);
	}

	/// <summary>
	/// Multiple <see cref="Unit.Value"/> on the specified times.
	/// </summary>
	/// <param name="unit">Unit.</param>
	/// <param name="times">Multiply value.</param>
	/// <returns>Result.</returns>
	public static Unit Times(this Unit unit, int times)
	{
		if (unit is null)
			throw new ArgumentNullException(nameof(unit));

		return new Unit(unit.Value * times, unit.Type, unit.GetTypeValue);
	}
}