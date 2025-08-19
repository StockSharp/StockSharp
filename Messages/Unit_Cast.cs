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
	/// Cast <see cref="short"/> object to the type <see cref="Unit"/>.
	/// </summary>
	/// <param name="value"><see cref="short"/> value.</param>
	/// <returns>Object <see cref="Unit"/>.</returns>
	public static implicit operator Unit(short value) => new(value);

	/// <summary>
	/// Cast <see cref="int"/> object to the type <see cref="Unit"/>.
	/// </summary>
	/// <param name="value"><see cref="int"/> value.</param>
	/// <returns>Object <see cref="Unit"/>.</returns>
	public static implicit operator Unit(int value) => new(value);

	/// <summary>
	/// Cast <see cref="long"/> object to the type <see cref="Unit"/>.
	/// </summary>
	/// <param name="value"><see cref="long"/> value.</param>
	/// <returns>Object <see cref="Unit"/>.</returns>
	public static implicit operator Unit(long value) => new(value);

	/// <summary>
	/// Cast <see cref="float"/> object to the type <see cref="Unit"/>.
	/// </summary>
	/// <param name="value"><see cref="float"/> value.</param>
	/// <returns>Object <see cref="Unit"/>.</returns>
	public static implicit operator Unit(float value) => (decimal)value;

	/// <summary>
	/// Cast <see cref="double"/> object to the type <see cref="Unit"/>.
	/// </summary>
	/// <param name="value"><see cref="double"/> value.</param>
	/// <returns>Object <see cref="Unit"/>.</returns>
	public static implicit operator Unit(double value) => (decimal)value;

	/// <summary>
	/// Cast object from <see cref="Unit"/> to <see cref="short"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="short"/> value.</returns>
	public static explicit operator short(Unit unit)
	{
		if (unit is null)
			throw new ArgumentNullException(nameof(unit));

		return (short)(decimal)unit;
	}

	/// <summary>
	/// Cast object from <see cref="Unit"/> to nullable <see cref="short"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="short"/> value.</returns>
	public static explicit operator short?(Unit unit)
	{
		if (unit is null)
			return null;

		return (short)unit;
	}

	/// <summary>
	/// Cast object from <see cref="Unit"/> to <see cref="int"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="int"/> value.</returns>
	public static explicit operator int(Unit unit)
	{
		if (unit is null)
			throw new ArgumentNullException(nameof(unit));

		return (int)(decimal)unit;
	}

	/// <summary>
	/// Cast object from <see cref="Unit"/> to nullable <see cref="int"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="int"/> value.</returns>
	public static explicit operator int?(Unit unit)
	{
		if (unit is null)
			return null;

		return (int)unit;
	}

	/// <summary>
	/// Cast object from <see cref="Unit"/> to <see cref="long"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="long"/> value.</returns>
	public static explicit operator long(Unit unit)
	{
		if (unit is null)
			throw new ArgumentNullException(nameof(unit));

		return (long)(decimal)unit;
	}

	/// <summary>
	/// Cast object from <see cref="Unit"/> to nullable <see cref="long"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="long"/> value.</returns>
	public static explicit operator long?(Unit unit)
	{
		if (unit is null)
			return null;

		return (long)unit;
	}

	/// <summary>
	/// Cast object from <see cref="Unit"/> to <see cref="float"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="float"/> value.</returns>
	public static explicit operator float(Unit unit)
	{
		if (unit is null)
			throw new ArgumentNullException(nameof(unit));

		return (float)(decimal)unit;
	}

	/// <summary>
	/// Cast object from <see cref="Unit"/> to nullable <see cref="float"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="float"/> value.</returns>
	public static explicit operator float?(Unit unit)
	{
		if (unit is null)
			return null;

		return (float)unit;
	}

	/// <summary>
	/// Cast object from <see cref="Unit"/> to <see cref="decimal"/>.
	/// </summary>
	/// <param name="unit">Object <see cref="Unit"/>.</param>
	/// <returns><see cref="decimal"/> value.</returns>
	public static explicit operator decimal(Unit unit)
	{
		if (unit is null)
			throw new ArgumentNullException(nameof(unit));

		return unit.ToDecimal();
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
	public static Unit operator +(decimal d, Unit u) => (Unit)d + u;

	/// <summary>
	/// Add the two objects <see cref="Unit"/> and <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="decimal"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(Unit u, decimal d) => u + (Unit)d;

	/// <summary>
	/// Add the two objects <see cref="Unit"/> and <see cref="int"/>.
	/// </summary>
	/// <param name="i">First object <see cref="int"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(int i, Unit u) => (Unit)i + u;

	/// <summary>
	/// Add the two objects <see cref="Unit"/> and <see cref="int"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="i">Second object <see cref="int"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(Unit u, int i) => u + (Unit)i;

	/// <summary>
	/// Add the two objects <see cref="Unit"/> and <see cref="long"/>.
	/// </summary>
	/// <param name="l">First object <see cref="long"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(long l, Unit u) => (Unit)l + u;

	/// <summary>
	/// Add the two objects <see cref="Unit"/> and <see cref="long"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="l">Second object <see cref="long"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(Unit u, long l) => u + (Unit)l;

	/// <summary>
	/// Add the two objects <see cref="Unit"/> and <see cref="double"/>.
	/// </summary>
	/// <param name="d">First object <see cref="double"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(double d, Unit u) => (Unit)d + u;

	/// <summary>
	/// Add the two objects <see cref="Unit"/> and <see cref="double"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="double"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(Unit u, double d) => u + (Unit)d;

	/// <summary>
	/// Subtract the <see cref="Unit"/> from a <see cref="decimal"/>.
	/// </summary>
	/// <param name="d">First object <see cref="decimal"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(decimal d, Unit u) => (Unit)d - u;

	/// <summary>
	/// Subtract a <see cref="decimal"/> from a <see cref="Unit"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="decimal"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(Unit u, decimal d) => u - (Unit)d;

	/// <summary>
	/// Subtract the <see cref="Unit"/> from an <see cref="int"/>.
	/// </summary>
	/// <param name="i">First object <see cref="int"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(int i, Unit u) => (Unit)i - u;

	/// <summary>
	/// Subtract an <see cref="int"/> from a <see cref="Unit"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="i">Second object <see cref="int"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(Unit u, int i) => u - (Unit)i;

	/// <summary>
	/// Subtract the <see cref="Unit"/> from a <see cref="long"/>.
	/// </summary>
	/// <param name="l">First object <see cref="long"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(long l, Unit u) => (Unit)l - u;

	/// <summary>
	/// Subtract a <see cref="long"/> from a <see cref="Unit"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="l">Second object <see cref="long"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(Unit u, long l) => u - (Unit)l;

	/// <summary>
	/// Subtract the <see cref="Unit"/> from a <see cref="double"/>.
	/// </summary>
	/// <param name="d">First object <see cref="double"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(double d, Unit u) => (Unit)d - u;

	/// <summary>
	/// Subtract a <see cref="double"/> from a <see cref="Unit"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="double"/>.</param>
	/// <returns>The result of subtraction.</returns>
	public static Unit operator -(Unit u, double d) => u - (Unit)d;

	/// <summary>
	/// Multiply a <see cref="decimal"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">First object <see cref="decimal"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(decimal d, Unit u) => (Unit)d * u;

	/// <summary>
	/// Multiply a <see cref="Unit"/> by a <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="decimal"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(Unit u, decimal d) => u * (Unit)d;

	/// <summary>
	/// Multiply an <see cref="int"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="i">First object <see cref="int"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(int i, Unit u) => (Unit)i * u;

	/// <summary>
	/// Multiply a <see cref="Unit"/> by an <see cref="int"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="i">Second object <see cref="int"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(Unit u, int i) => u * (Unit)i;

	/// <summary>
	/// Multiply a <see cref="long"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="l">First object <see cref="long"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(long l, Unit u) => (Unit)l * u;

	/// <summary>
	/// Multiply a <see cref="Unit"/> by a <see cref="long"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="l">Second object <see cref="long"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(Unit u, long l) => u * (Unit)l;

	/// <summary>
	/// Multiply a <see cref="double"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">First object <see cref="double"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(double d, Unit u) => (Unit)d * u;

	/// <summary>
	/// Multiply a <see cref="Unit"/> by a <see cref="double"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="double"/>.</param>
	/// <returns>The result of multiplication.</returns>
	public static Unit operator *(Unit u, double d) => u * (Unit)d;

	/// <summary>
	/// Divide a <see cref="decimal"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">First object <see cref="decimal"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(decimal d, Unit u) => (Unit)d / u;

	/// <summary>
	/// Divide a <see cref="Unit"/> by a <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="decimal"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(Unit u, decimal d) => u / (Unit)d;

	/// <summary>
	/// Divide an <see cref="int"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="i">First object <see cref="int"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(int i, Unit u) => (Unit)i / u;

	/// <summary>
	/// Divide a <see cref="Unit"/> by an <see cref="int"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="i">Second object <see cref="int"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(Unit u, int i) => u / (Unit)i;

	/// <summary>
	/// Divide a <see cref="long"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="l">First object <see cref="long"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(long l, Unit u) => (Unit)l / u;

	/// <summary>
	/// Divide a <see cref="Unit"/> by a <see cref="long"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="l">Second object <see cref="long"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(Unit u, long l) => u / (Unit)l;

	/// <summary>
	/// Divide a <see cref="double"/> by a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">First object <see cref="double"/>.</param>
	/// <param name="u">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(double d, Unit u) => (Unit)d / u;

	/// <summary>
	/// Divide a <see cref="Unit"/> by a <see cref="double"/>.
	/// </summary>
	/// <param name="u">First object <see cref="Unit"/>.</param>
	/// <param name="d">Second object <see cref="double"/>.</param>
	/// <returns>The result of division.</returns>
	public static Unit operator /(Unit u, double d) => u / (Unit)d;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is greater than or equal to a <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is greater than or equal to the <see cref="decimal"/>, <see langword="false" />.</returns>
	public static bool operator >=(Unit u, decimal d) => u >= (Unit)d;

	/// <summary>
	/// Check whether the <see cref="decimal"/> is greater than or equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="decimal"/> is greater than or equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator >=(decimal d, Unit u) => (Unit)d >= u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is greater than or equal to an <see cref="int"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is greater than or equal to the <see cref="int"/>, <see langword="false" />.</returns>
	public static bool operator >=(Unit u, int i) => u >= (Unit)i;

	/// <summary>
	/// Check whether the <see cref="int"/> is greater than or equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="int"/> is greater than or equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator >=(int i, Unit u) => (Unit)i >= u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is greater than or equal to a <see cref="long"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is greater than or equal to the <see cref="long"/>, <see langword="false" />.</returns>
	public static bool operator >=(Unit u, long l) => u >= (Unit)l;

	/// <summary>
	/// Check whether the <see cref="long"/> is greater than or equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="long"/> is greater than or equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator >=(long l, Unit u) => (Unit)l >= u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is greater than or equal to a <see cref="double"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is greater than or equal to the <see cref="double"/>, <see langword="false" />.</returns>
	public static bool operator >=(Unit u, double d) => u >= (Unit)d;

	/// <summary>
	/// Check whether the <see cref="double"/> is greater than or equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="double"/> is greater than or equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator >=(double d, Unit u) => (Unit)d >= u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is greater than a <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is greater than the <see cref="decimal"/>, <see langword="false" />.</returns>
	public static bool operator >(Unit u, decimal d) => u > (Unit)d;

	/// <summary>
	/// Check whether the <see cref="decimal"/> is greater than a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="decimal"/> is greater than the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator >(decimal d, Unit u) => (Unit)d > u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is greater than an <see cref="int"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is greater than the <see cref="int"/>, <see langword="false" />.</returns>
	public static bool operator >(Unit u, int i) => u > (Unit)i;

	/// <summary>
	/// Check whether the <see cref="int"/> is greater than a <see cref="Unit"/>.
	/// </summary>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="int"/> is greater than the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator >(int i, Unit u) => (Unit)i > u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is greater than a <see cref="long"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is greater than the <see cref="long"/>, <see langword="false" />.</returns>
	public static bool operator >(Unit u, long l) => u > (Unit)l;

	/// <summary>
	/// Check whether the <see cref="long"/> is greater than a <see cref="Unit"/>.
	/// </summary>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="long"/> is greater than the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator >(long l, Unit u) => (Unit)l > u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is greater than a <see cref="double"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is greater than the <see cref="double"/>, <see langword="false" />.</returns>
	public static bool operator >(Unit u, double d) => u > (Unit)d;

	/// <summary>
	/// Check whether the <see cref="double"/> is greater than a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="double"/> is greater than the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator >(double d, Unit u) => (Unit)d > u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is less than a <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is less than the <see cref="decimal"/>, <see langword="false" />.</returns>
	public static bool operator <(Unit u, decimal d) => u < (Unit)d;

	/// <summary>
	/// Check whether the <see cref="decimal"/> is less than a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="decimal"/> is less than the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator <(decimal d, Unit u) => (Unit)d < u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is less than an <see cref="int"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is less than the <see cref="int"/>, <see langword="false" />.</returns>
	public static bool operator <(Unit u, int i) => u < (Unit)i;

	/// <summary>
	/// Check whether the <see cref="int"/> is less than a <see cref="Unit"/>.
	/// </summary>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="int"/> is less than the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator <(int i, Unit u) => (Unit)i < u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is less than a <see cref="long"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is less than the <see cref="long"/>, <see langword="false" />.</returns>
	public static bool operator <(Unit u, long l) => u < (Unit)l;

	/// <summary>
	/// Check whether the <see cref="long"/> is less than a <see cref="Unit"/>.
	/// </summary>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="long"/> is less than the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator <(long l, Unit u) => (Unit)l < u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is less than a <see cref="double"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is less than the <see cref="double"/>, <see langword="false" />.</returns>
	public static bool operator <(Unit u, double d) => u < (Unit)d;

	/// <summary>
	/// Check whether the <see cref="double"/> is less than a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="double"/> is less than the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator <(double d, Unit u) => (Unit)d < u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is less than or equal to a <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is less than or equal to the <see cref="decimal"/>, <see langword="false" />.</returns>
	public static bool operator <=(Unit u, decimal d) => u <= (Unit)d;

	/// <summary>
	/// Check whether the <see cref="decimal"/> is less than or equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="decimal"/> is less than or equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator <=(decimal d, Unit u) => (Unit)d <= u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is less than or equal to an <see cref="int"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is less than or equal to the <see cref="int"/>, <see langword="false" />.</returns>
	public static bool operator <=(Unit u, int i) => u <= (Unit)i;

	/// <summary>
	/// Check whether the <see cref="int"/> is less than or equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="int"/> is less than or equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator <=(int i, Unit u) => (Unit)i <= u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is less than or equal to a <see cref="long"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is less than or equal to the <see cref="long"/>, <see langword="false" />.</returns>
	public static bool operator <=(Unit u, long l) => u <= (Unit)l;

	/// <summary>
	/// Check whether the <see cref="long"/> is less than or equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="long"/> is less than or equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator <=(long l, Unit u) => (Unit)l <= u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is less than or equal to a <see cref="double"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is less than or equal to the <see cref="double"/>, <see langword="false" />.</returns>
	public static bool operator <=(Unit u, double d) => u <= (Unit)d;

	/// <summary>
	/// Check whether the <see cref="double"/> is less than or equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="double"/> is less than or equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator <=(double d, Unit u) => (Unit)d <= u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is equal to a <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is equal to the <see cref="decimal"/>, <see langword="false" />.</returns>
	public static bool operator ==(Unit u, decimal d) => u == (Unit)d;

	/// <summary>
	/// Check whether the <see cref="decimal"/> is equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="decimal"/> is equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator ==(decimal d, Unit u) => (Unit)d == u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is equal to an <see cref="int"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is equal to the <see cref="int"/>, <see langword="false" />.</returns>
	public static bool operator ==(Unit u, int i) => u == (Unit)i;

	/// <summary>
	/// Check whether the <see cref="int"/> is equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="int"/> is equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator ==(int i, Unit u) => (Unit)i == u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is equal to a <see cref="long"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is equal to the <see cref="long"/>, <see langword="false" />.</returns>
	public static bool operator ==(Unit u, long l) => u == (Unit)l;

	/// <summary>
	/// Check whether the <see cref="long"/> is equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="long"/> is equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator ==(long l, Unit u) => (Unit)l == u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is equal to a <see cref="double"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is equal to the <see cref="double"/>, <see langword="false" />.</returns>
	public static bool operator ==(Unit u, double d) => u == (Unit)d;

	/// <summary>
	/// Check whether the <see cref="double"/> is equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="double"/> is equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator ==(double d, Unit u) => (Unit)d == u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is not equal to a <see cref="decimal"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is not equal to the <see cref="decimal"/>, <see langword="false" />.</returns>
	public static bool operator !=(Unit u, decimal d) => u != (Unit)d;

	/// <summary>
	/// Check whether the <see cref="decimal"/> is not equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="decimal"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="decimal"/> is not equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator !=(decimal d, Unit u) => (Unit)d != u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is not equal to an <see cref="int"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is not equal to the <see cref="int"/>, <see langword="false" />.</returns>
	public static bool operator !=(Unit u, int i) => u != (Unit)i;

	/// <summary>
	/// Check whether the <see cref="int"/> is not equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="i">The <see cref="int"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="int"/> is not equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator !=(int i, Unit u) => (Unit)i != u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is not equal to a <see cref="long"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is not equal to the <see cref="long"/>, <see langword="false" />.</returns>
	public static bool operator !=(Unit u, long l) => u != (Unit)l;

	/// <summary>
	/// Check whether the <see cref="long"/> is not equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="l">The <see cref="long"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="long"/> is not equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator !=(long l, Unit u) => (Unit)l != u;

	/// <summary>
	/// Check whether the <see cref="Unit"/> is not equal to a <see cref="double"/>.
	/// </summary>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="Unit"/> is not equal to the <see cref="double"/>, <see langword="false" />.</returns>
	public static bool operator !=(Unit u, double d) => u != (Unit)d;

	/// <summary>
	/// Check whether the <see cref="double"/> is not equal to a <see cref="Unit"/>.
	/// </summary>
	/// <param name="d">The <see cref="double"/> to compare.</param>
	/// <param name="u">The <see cref="Unit"/> to compare.</param>
	/// <returns><see langword="true" />, if the <see cref="double"/> is not equal to the <see cref="Unit"/>, <see langword="false" />.</returns>
	public static bool operator !=(double d, Unit u) => (Unit)d != u;
}