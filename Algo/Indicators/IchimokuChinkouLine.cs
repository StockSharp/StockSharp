namespace StockSharp.Algo.Indicators;

/// <summary>
/// Chinkou line.
/// </summary>
[IndicatorIn(typeof(CandleIndicatorValue))]
[IndicatorHidden]
public class IchimokuChinkouLine : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="IchimokuChinkouLine"/>.
	/// </summary>
	public IchimokuChinkouLine()
	{
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var close = input.ToCandle().ClosePrice;

		if (input.IsFinal)
			Buffer.PushBack(close);

		return close;
	}
}