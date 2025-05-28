namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Accumulation/Distribution Line (A/D Line).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ADLKey,
	Description = LocalizedStrings.AccumulationDistributionLineKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/accumulation_distribution_line.html")]
public class AccumulationDistributionLine : BaseIndicator
{
	private decimal _adLine;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var cl = candle.GetLength();
		var adLine = _adLine;

		if (cl != 0)
		{
			var mfm = ((candle.ClosePrice - candle.LowPrice) - (candle.HighPrice - candle.ClosePrice)) / cl;
			var mfv = mfm * candle.TotalVolume;
			adLine += mfv;
		}

		if (input.IsFinal)
		{
			IsFormed = true;
			_adLine = adLine;
		}

		return new DecimalIndicatorValue(this, adLine, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_adLine = 0;

		base.Reset();
	}
}
