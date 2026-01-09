namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Balance of Power (BOP).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BOPKey,
	Description = LocalizedStrings.BalanceOfPowerKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/balance_of_power.html")]
public class BalanceOfPower : BaseIndicator
{
	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var cl = candle.GetLength();

		if (input.IsFinal && cl != 0)
		{
			IsFormed = true;

			var bop = (candle.ClosePrice - candle.OpenPrice) / cl;
			return new DecimalIndicatorValue(this, bop, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}
}
