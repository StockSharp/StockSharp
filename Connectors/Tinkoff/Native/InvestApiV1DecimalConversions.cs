namespace StockSharp.Tinkoff.Native;

static class InvestApiDecimal
{
	private const decimal _nanoFactor = 1_000_000_000m;
	private const int _nanoMax = 1_000_000_000;

	public static decimal ToDecimal(long units, int nano)
		=> units + nano / _nanoFactor;

	public static void Split(decimal value, out long units, out int nano)
	{
		units = decimal.ToInt64(decimal.Truncate(value));

		var nanoDecimal = (value - units) * _nanoFactor;
		nano = decimal.ToInt32(decimal.Round(nanoDecimal, 0, MidpointRounding.AwayFromZero));

		if (nano == _nanoMax)
		{
			units += 1;
			nano = 0;
		}
		else if (nano == -_nanoMax)
		{
			units -= 1;
			nano = 0;
		}
	}
}

internal sealed partial class Quotation
{
	/// <summary>
	/// Converts <see cref="decimal"/> to <see cref="Quotation"/>.
	/// </summary>
	public static implicit operator Quotation(decimal value)
	{
		InvestApiDecimal.Split(value, out var units, out var nano);
		return new() { Units = units, Nano = nano };
	}

	/// <summary>
	/// Converts <see cref="Quotation"/> to <see cref="decimal"/>.
	/// </summary>
	public static explicit operator decimal(Quotation value)
		=> value is null
			? throw new ArgumentNullException(nameof(value))
			: InvestApiDecimal.ToDecimal(value.Units, value.Nano);
}

internal sealed partial class MoneyValue
{
	/// <summary>
	/// Converts <see cref="MoneyValue"/> to <see cref="decimal"/> (currency is ignored).
	/// </summary>
	public static implicit operator decimal(MoneyValue value)
		=> value is null
			? throw new ArgumentNullException(nameof(value))
			: InvestApiDecimal.ToDecimal(value.Units, value.Nano);
}
