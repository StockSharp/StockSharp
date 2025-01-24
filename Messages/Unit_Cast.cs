namespace StockSharp.Messages;

partial class Unit
{
	/// <summary>
	/// Cast <see cref="decimal"/> object to the type <see cref="Unit"/>.
	/// </summary>
	/// <param name="value"><see cref="decimal"/> value.</param>
	/// <returns>Object <see cref="Unit"/>.</returns>
	public static implicit operator Unit(decimal value) => new(value);

	/// <summary>
	/// Cast <see cref="int"/> object to the type <see cref="Unit"/>.
	/// </summary>
	/// <param name="value"><see cref="int"/> value.</param>
	/// <returns>Object <see cref="Unit"/>.</returns>
	public static implicit operator Unit(int value) => new(value);

	/// <summary>
	/// Cast object from <see cref="Unit"/> to <see cref="decimal"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="decimal"/> value.</returns>
	public static explicit operator decimal(Unit unit)
	{
		if (unit is null)
			throw new ArgumentNullException(nameof(unit));

		switch (unit.Type)
		{
			case UnitTypes.Limit:
			case UnitTypes.Absolute:
				return unit.Value;
			case UnitTypes.Percent:
				throw new ArgumentException(LocalizedStrings.PercentagesConvert, nameof(unit));
			case UnitTypes.Point:
				var point = unit.GetTypeValue?.Invoke(unit.Type) ?? throw new InvalidOperationException(LocalizedStrings.PriceStepNotSpecified);

				return unit.Value * point;
			case UnitTypes.Step:
				var step = unit.GetTypeValue?.Invoke(unit.Type) ?? throw new InvalidOperationException(LocalizedStrings.PriceStepNotSpecified);

				return unit.Value * step;
			default:
				throw new ArgumentOutOfRangeException(nameof(unit), unit.Type, LocalizedStrings.InvalidValue);
		}
	}

	/// <summary>
	/// Cast object from <see cref="Unit"/> to nullable <see cref="decimal"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="decimal"/> value.</returns>
	public static explicit operator decimal?(Unit unit)
	{
		if (unit is null)
			return null;

		return (decimal)unit;
	}

	/// <summary>
	/// Cast <see cref="double"/> object to the type <see cref="Unit"/>.
	/// </summary>
	/// <param name="value"><see cref="double"/> value.</param>
	/// <returns>Object <see cref="Unit"/>.</returns>
	public static implicit operator Unit(double value) => (decimal)value;

	/// <summary>
	/// Cast object from <see cref="Unit"/> to <see cref="double"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="double"/> value.</returns>
	public static explicit operator double(Unit unit)
	{
		if (unit is null)
			throw new ArgumentNullException(nameof(unit));

		return (double)(decimal)unit;
	}

	/// <summary>
	/// Cast object from <see cref="Unit"/> to nullable <see cref="double"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="double"/> value.</returns>
	public static explicit operator double?(Unit unit)
	{
		if (unit is null)
			return null;

		return (double)unit;
	}

	// for languages without auto Unit casting

	/// <summary>
	/// Add the two objects <see cref="Unit"/> and <see cref="decimal"/>.
	/// </summary>
	/// <param name="d">First object <see cref="decimal"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(decimal d, Unit u) => new Unit(d) + u;

	/// <summary>
	/// Add the two objects <see cref="Unit"/> and <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="decimal"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(Unit u, decimal d) => u + new Unit(d);

	/// <summary>
	/// Add the two objects <see cref="Unit"/> and <see cref="int"/>.
	/// </summary>
	/// <param name="i">First object <see cref="int"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(int i, Unit u) => new Unit(i) + u;

	/// <summary>
	/// Add the two objects <see cref="Unit"/> and <see cref="int"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="i">Second object <see cref="int"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(Unit u, int i) => u + new Unit(i);

	/// <summary>
	/// Subtract the <see cref="Unit"/> from a <see cref="decimal"/>.
	/// </summary>
	/// <param name="d">First object <see cref="decimal"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(decimal d, Unit u) => new Unit(d) - u;

	/// <summary>
	/// Subtract a <see cref="decimal"/> from a <see cref="Unit"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="decimal"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(Unit u, decimal d) => u - new Unit(d);

	/// <summary>
	/// Subtract the <see cref="Unit"/> from an <see cref="int"/>.
	/// </summary>
	/// <param name="i">First object <see cref="int"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(int i, Unit u) => new Unit(i) - u;

	/// <summary>
	/// Subtract an <see cref="int"/> from a <see cref="Unit"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="i">Second object <see cref="int"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(Unit u, int i) => u - new Unit(i);

	/// <summary>
	/// Multiply a <see cref="decimal"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">First object <see cref="decimal"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(decimal d, Unit u) => new Unit(d) * u;

	/// <summary>
	/// Multiply a <see cref="Unit"/> by a <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="decimal"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(Unit u, decimal d) => u * new Unit(d);

	/// <summary>
	/// Multiply an <see cref="int"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="i">First object <see cref="int"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(int i, Unit u) => new Unit(i) * u;

	/// <summary>
	/// Multiply a <see cref="Unit"/> by an <see cref="int"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="i">Second object <see cref="int"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(Unit u, int i) => u * new Unit(i);

	/// <summary>
	/// Divide a <see cref="decimal"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">First object <see cref="decimal"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(decimal d, Unit u) => new Unit(d) / u;

	/// <summary>
	/// Divide a <see cref="Unit"/> by a <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="decimal"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(Unit u, decimal d) => u / new Unit(d);

	/// <summary>
	/// Divide an <see cref="int"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="i">First object <see cref="int"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(int i, Unit u) => new Unit(i) / u;

	/// <summary>
	/// Divide a <see cref="Unit"/> by an <see cref="int"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="i">Second object <see cref="int"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(Unit u, int i) => u / new Unit(i);
}