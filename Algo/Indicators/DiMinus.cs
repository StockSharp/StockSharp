namespace StockSharp.Algo.Indicators;

/// <summary>
/// DIMinus is a component of the Directional Movement System developed by Welles Wilder.
/// </summary>
[IndicatorHidden]
public class DiMinus : DiPart
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DiMinus"/>.
	/// </summary>
	public DiMinus()
	{
	}

	/// <inheritdoc />
	protected override decimal GetValue(ICandleMessage current, ICandleMessage prev)
	{
		if (current.LowPrice < prev.LowPrice && current.HighPrice - prev.HighPrice < prev.LowPrice - current.LowPrice)
			return prev.LowPrice - current.LowPrice;
		else
			return 0;
	}
}