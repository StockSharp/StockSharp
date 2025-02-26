namespace StockSharp.Algo.Indicators;

/// <summary>
/// Balance of Market Power (BMP) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BMPKey,
	Description = LocalizedStrings.BalanceOfMarketPowerKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/balance_of_market_power.html")]
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
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var bmp = candle.TotalVolume != 0
			? ((candle.ClosePrice - candle.OpenPrice) / (candle.HighPrice == candle.LowPrice ? 0.01m : candle.HighPrice - candle.LowPrice))
			: 0;

		var smaValue = base.OnProcessDecimal(input.SetValue(this, bmp));

		return IsFormed
			? smaValue.Value
			: null;
	}
}
