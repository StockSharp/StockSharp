namespace StockSharp.Algo.Indicators;

/// <summary>
/// Bear Power indicator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/bear_power.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BearPowerKey,
	Description = LocalizedStrings.BearPowerDescKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/bear_power.html")]
public class BearPower : ExponentialMovingAverage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BearPower"/>.
	/// </summary>
	public BearPower()
	{
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var emaValue = base.OnProcessDecimal(input);

		if (emaValue is not null)
			return candle.LowPrice - emaValue.Value;

		return null;
	}
}