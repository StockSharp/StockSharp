namespace StockSharp.Algo.Indicators;

/// <summary>
/// Forecast Oscillator (FOSC) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FOSCKey,
	Description = LocalizedStrings.ForecastOscillatorKey)]
[Doc("topics/indicators/forecast_oscillator.html")]
public class ForecastOscillator : LinearReg
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ForecastOscillator"/>.
	/// </summary>
	public ForecastOscillator()
		: base()
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var baseResult = base.OnProcess(input);

		if (IsFormed)
		{
			var price = input.ToDecimal();
			var forecastValue = baseResult.ToDecimal();
			var fosc = ((price - forecastValue) / price) * 100;
			return new DecimalIndicatorValue(this, fosc, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}
}