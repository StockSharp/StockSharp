namespace StockSharp.Algo.Indicators;

/// <summary>
/// Typical Price indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TypicalPriceKey,
	Description = LocalizedStrings.TypicalPriceDescriptionKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/typical_price.html")]
public class TypicalPrice : BaseIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TypicalPrice"/>.
	/// </summary>
	public TypicalPrice()
	{
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (input.IsFinal)
			IsFormed = true;

		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		return new DecimalIndicatorValue(this, typicalPrice, input.Time);
	}
}