namespace StockSharp.Algo.Indicators;

/// <summary>
/// Williams Variable Accumulation Distribution (WVAD) indicator.
/// Cumulative indicator: WVAD += ((Close - Open) / (High - Low)) * Volume.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.WVADKey,
	Description = LocalizedStrings.WilliamsVariableAccumulationDistributionKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/williams_variable_accumulation_distribution.html")]
public class WilliamsVariableAccumulationDistribution : BaseIndicator
{
	private decimal _wvad;

	/// <summary>
	/// Initializes a new instance of the <see cref="WilliamsVariableAccumulationDistribution"/>.
	/// </summary>
	public WilliamsVariableAccumulationDistribution()
	{
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Volume;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var currentValue = _wvad;
		var range = candle.HighPrice - candle.LowPrice;

		if (range != 0)
			currentValue += (candle.ClosePrice - candle.OpenPrice) / range * candle.TotalVolume;

		if (input.IsFinal)
		{
			_wvad = currentValue;
			IsFormed = true;
		}

		return new DecimalIndicatorValue(this, currentValue, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_wvad = 0;
		base.Reset();
	}
}
