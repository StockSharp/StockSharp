namespace StockSharp.Algo.Indicators;

/// <summary>
/// Disparity Index indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.DPIKey,
	Description = LocalizedStrings.DisparityIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/disparity_index.html")]
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
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var smaValue = base.OnProcessDecimal(input);

		if (IsFormed)
		{
			var price = input.ToDecimal();
			var sma = smaValue.Value;
			var disparityIndex = (price - sma) / sma * 100;
			return disparityIndex;
		}

		return null;
	}
}


