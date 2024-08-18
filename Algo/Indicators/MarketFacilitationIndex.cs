namespace StockSharp.Algo.Indicators;

/// <summary>
/// Market Facilitation Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/market_facilitation_index.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MFIKey,
	Description = LocalizedStrings.MarketFacilitationIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/market_facilitation_index.html")]
public class MarketFacilitationIndex : BaseIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MarketFacilitationIndex"/>.
	/// </summary>
	public MarketFacilitationIndex()
	{
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var (_, high, low, _, vol) = input.GetOhlcv();

		if (vol == 0)
			return new DecimalIndicatorValue(this);

		if (input.IsFinal)
			IsFormed = true;

		return new DecimalIndicatorValue(this, (high - low) / vol);
	}
}
