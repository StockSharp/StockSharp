namespace StockSharp.Fix.Native;

using System.Globalization;
using System.Numerics;

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
	public decimal AsDecimal => MathHelper.ToDecimal(Mantissa, Exponent);

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
		if (n1.Mantissa == 0)
			return n2;

		if (n2.Mantissa == 0)
			return n1;

		var higher = n1.Exponent >= n2.Exponent ? n1 : n2;
		var lower = n1.Exponent >= n2.Exponent ? n2 : n1;
		var exponentDifference = (long)higher.Exponent - lower.Exponent;

		// A long mantissa carries at most 19 decimal digits. Past this gap the lower-scaled
		// operand cannot affect the rounded representation of the higher-scaled operand.
		if (exponentDifference > 38)
			return higher;

		var sum = (BigInteger)higher.Mantissa * BigInteger.Pow(10, (int)exponentDifference) + lower.Mantissa;
		return FromBigInteger(sum, lower.Exponent);
	}

	private static ScaledNumber FromBigInteger(BigInteger value, int exponent)
	{
		if (value.IsZero)
			return new ScaledNumber(0, 0);

		var resultExponent = (long)exponent;
		var divisor = BigInteger.One;

		while (true)
		{
			var mantissa = BigInteger.DivRem(value, divisor, out var remainder);

			if (mantissa >= long.MinValue && mantissa <= long.MaxValue)
			{
				if (divisor > BigInteger.One && BigInteger.Abs(remainder) * 2 >= divisor)
					mantissa += value.Sign;

				if (mantissa >= long.MinValue && mantissa <= long.MaxValue)
					return new ScaledNumber(checked((int)resultExponent), (long)mantissa);
			}

			if (resultExponent == int.MaxValue)
				throw new OverflowException("The scaled number exponent is too large.");

			divisor *= 10;
			resultExponent++;
		}
	}
}
