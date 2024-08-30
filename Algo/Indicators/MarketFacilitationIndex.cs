namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

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
		var candle = input.ToCandle();

		if (candle.TotalVolume == 0)
			return new DecimalIndicatorValue(this, input.Time);

		if (input.IsFinal)
			IsFormed = true;

		return new DecimalIndicatorValue(this, candle.GetLength() / candle.TotalVolume, input.Time);
	}
}
