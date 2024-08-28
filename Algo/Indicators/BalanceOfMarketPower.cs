namespace StockSharp.Algo.Indicators;

/// <summary>
/// Balance of Market Power (BMP) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BMPKey,
	Description = LocalizedStrings.BalanceOfMarketPowerKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/balance_of_market_power.html")]
public class BalanceOfMarketPower : SimpleMovingAverage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BalanceOfMarketPower"/>.
	/// </summary>
	public BalanceOfMarketPower()
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var (open, high, low, close, volume) = input.GetOhlcv();

		var bmp = volume != 0
			? ((close - open) / (high == low ? 0.01m : high - low))
			: 0;

		var smaValue = base.OnProcess(input.SetValue(this, bmp));

		return IsFormed
			? new DecimalIndicatorValue(this, smaValue.GetValue<decimal>(), input.Time)
			: new DecimalIndicatorValue(this, input.Time);
	}
}
