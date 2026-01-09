namespace StockSharp.Algo.Indicators;

/// <summary>
/// Bull Power indicator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/bull_power.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BullPowerKey,
	Description = LocalizedStrings.BullPowerDescKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/bull_power.html")]
public class BullPower : ExponentialMovingAverage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BullPower"/>.
	/// </summary>
	public BullPower()
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
			return candle.HighPrice - emaValue.Value;

		return null;
	}
}