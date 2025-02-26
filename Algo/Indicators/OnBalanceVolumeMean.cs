namespace StockSharp.Algo.Indicators;

/// <summary>
/// On Balance Volume Mean indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OBVMKey,
	Description = LocalizedStrings.OnBalanceVolumeMeanKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/on_balance_volume_mean.html")]
public class OnBalanceVolumeMean : SimpleMovingAverage
{
	private readonly OnBalanceVolume _obv = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="OnBalanceVolumeMean"/>.
	/// </summary>
	public OnBalanceVolumeMean()
	{
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Volume;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
		=> base.OnProcessDecimal(_obv.Process(input));

	/// <inheritdoc />
	public override void Reset()
	{
		_obv.Reset();
		
		base.Reset();
	}
}