namespace StockSharp.Algo.Indicators;

/// <summary>
/// Accumulation/Distribution Line (A/D Line).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ADLKey,
	Description = LocalizedStrings.AccumulationDistributionLineKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/accumulation_distribution_line.html")]
public class AccumulationDistributionLine : BaseIndicator
{
	private decimal _adLine;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var (_, high, low, close, volume) = input.GetOhlcv();

		if (high != low)
		{
			var mfm = ((close - low) - (high - close)) / (high - low);
			var mfv = mfm * volume;
			_adLine += mfv;
		}

		if (input.IsFinal)
			IsFormed = true;

		return new DecimalIndicatorValue(this, _adLine, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_adLine = 0;

		base.Reset();
	}
}
