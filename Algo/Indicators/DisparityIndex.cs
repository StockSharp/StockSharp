namespace StockSharp.Algo.Indicators;

/// <summary>
/// Disparity Index indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.DPIKey,
	Description = LocalizedStrings.DisparityIndexKey)]
[Doc("topics/indicators/disparity_index.html")]
public class DisparityIndex : SimpleMovingAverage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DisparityIndex"/>.
	/// </summary>
	public DisparityIndex()
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var smaValue = base.OnProcess(input);

		if (IsFormed)
		{
			var price = input.ToDecimal();
			var sma = smaValue.ToDecimal();
			var disparityIndex = (price - sma) / sma * 100;
			return new DecimalIndicatorValue(this, disparityIndex, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}
}


