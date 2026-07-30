namespace StockSharp.Fix.Native;

using System.Globalization;

/// <summary>
/// Scaled numbers, like floating point numbers are represented as a mantissa and an exponent.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ScaledNumber"/>.
/// </remarks>
/// <param name="exponent">Exponent.</param>
/// <param name="mantissa">Mantissa.</param>
public readonly struct ScaledNumber(int exponent, long mantissa)
{

	/// <summary>
	/// Mantissa.
	/// </summary>
	public readonly long Mantissa = mantissa;

	/// <summary>
	/// Exponent.
	/// </summary>
	public readonly int Exponent = exponent;

	/// <summary>
	/// The numerical value is obtained by multiplying the mantissa with the base-10 power of the exponent.
	/// </summary>
	public double AsDouble => Mantissa * Math.Pow(10, Exponent);

	/// <summary>
	/// The numerical value is obtained by multiplying the mantissa with the base-10 power of the exponent.
	/// </summary>
	public decimal AsDecimal => new(AsDouble);

	/// <inheritdoc />
	public override string ToString() => Exponent == 0 ? Mantissa.ToString() : AsDouble.ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Add the two objects <see cref="ScaledNumber"/>.
	/// </summary>
	/// <param name="n1">First object <see cref="ScaledNumber"/>.</param>
	/// <param name="n2">Second object <see cref="ScaledNumber"/>.</param>
	/// <returns>The result of addition.</returns>
	public static ScaledNumber operator +(ScaledNumber n1, ScaledNumber n2)
	{
		return new ScaledNumber(n1.Exponent + n2.Exponent, n1.Mantissa + n2.Mantissa);
	}
}
