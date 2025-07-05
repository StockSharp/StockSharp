namespace StockSharp.Algo.Indicators;

/// <summary>
/// Linear Regression Forecast indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LinearRegressionForecastKey,
	Description = LocalizedStrings.LinearRegressionForecastDescriptionKey)]
[Doc("topics/indicators/linear_regression_forecast.html")]
public class LinearRegressionForecast : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LinearRegressionForecast"/>.
	/// </summary>
	public LinearRegressionForecast()
	{
		Length = 14;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(newValue);
		}

		if (IsFormed)
		{
			var buff = input.IsFinal ? Buffer : (IList<decimal>)[.. Buffer.Skip(1), newValue];

			// Linear regression calculation
			var sumX = 0m;
			var sumY = 0m;
			var sumXY = 0m;
			var sumX2 = 0m;

			for (var i = 0; i < Length; i++)
			{
				sumX += i;
				sumY += buff[i];
				sumXY += i * buff[i];
				sumX2 += i * i;
			}

			var divisor = Length * sumX2 - sumX * sumX;
			if (divisor != 0)
			{
				var slope = (Length * sumXY - sumX * sumY) / divisor;
				var intercept = (sumY - slope * sumX) / Length;

				// Forecast next value (Length position)
				var forecast = slope * Length + intercept;
				return new DecimalIndicatorValue(this, forecast, input.Time);
			}
		}

		return new DecimalIndicatorValue(this, input.Time);
	}
}