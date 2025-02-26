namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Intraday Intensity Index indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.IIIKey,
	Description = LocalizedStrings.IntradayIntensityIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/intraday_intensity_index.html")]
public class IntradayIntensityIndex : SimpleMovingAverage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="IntradayIntensityIndex"/>.
	/// </summary>
	public IntradayIntensityIndex()
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var cl = candle.GetLength();

		var iii = 0m;
		var den = cl * candle.TotalVolume;
		if (den != 0)
		{
			iii = 2 * ((candle.ClosePrice - candle.LowPrice) - (candle.HighPrice - candle.ClosePrice)) / den;
		}

		return base.OnProcessDecimal(input.SetValue(this, iii));
	}
}