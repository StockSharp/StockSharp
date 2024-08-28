namespace StockSharp.Algo.Indicators;

/// <summary>
/// Balance of Power (BOP).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BOPKey,
	Description = LocalizedStrings.BalanceOfPowerKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/balance_of_power.html")]
public class BalanceOfPower : BaseIndicator
{
	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var (open, high, low, close) = input.GetOhlc();

		if (high != low)
		{
			IsFormed = true;

			var bop = (close - open) / (high - low);
			return new DecimalIndicatorValue(this, bop, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}
}
