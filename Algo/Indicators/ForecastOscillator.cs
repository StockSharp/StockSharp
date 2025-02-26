namespace StockSharp.Algo.Indicators;

/// <summary>
/// Forecast Oscillator (FOSC) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FOSCKey,
	Description = LocalizedStrings.ForecastOscillatorKey)]
[Doc("topics/api/indicators/list_of_indicators/forecast_oscillator.html")]
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
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var baseResult = base.OnProcessDecimal(input);

		if (IsFormed)
		{
			var price = input.ToDecimal();
			var forecastValue = baseResult.Value;
			var fosc = ((price - forecastValue) / price) * 100;
			return fosc;
		}

		return null;
	}
}