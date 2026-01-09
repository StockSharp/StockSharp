namespace StockSharp.Algo.Indicators;

/// <summary>
/// Weighted Close Price indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.WeightedClosePriceKey,
	Description = LocalizedStrings.WeightedClosePriceDescriptionKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/weighted_close_price.html")]
public class WeightedClosePrice : BaseIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="WeightedClosePrice"/>.
	/// </summary>
	public WeightedClosePrice()
	{
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (input.IsFinal)
			IsFormed = true;

		// Weighted Close = (High + Low + 2*Close) / 4
		var weightedClose = (candle.HighPrice + candle.LowPrice + 2 * candle.ClosePrice) / 4m;
		return new DecimalIndicatorValue(this, weightedClose, input.Time);
	}
}