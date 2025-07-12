namespace StockSharp.Algo.Indicators;

/// <summary>
/// Hurst Exponent indicator.
/// </summary>
/// <remarks>
/// Measures the tendency of a time series to regress strongly to the mean or to cluster in a direction.
/// https://en.wikipedia.org/wiki/Hurst_exponent
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.HurstExponentKey,
	Description = LocalizedStrings.HurstExponentDescKey)]
[Doc("topics/api/indicators/list_of_indicators/hurst_exponent.html")]
public class HurstExponent : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="HurstExponent"/> class.
	/// </summary>
	public HurstExponent()
	{
		Length = 100;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var value = input.ToDecimal();

		if (input.IsFinal)
			Buffer.PushBack(value);

		if (!IsFormed)
			return null;

		IList<decimal> values = input.IsFinal
			? Buffer
			: [..Buffer.Skip(1), value];

		var n = values.Count;

		var mean = values.Average();

		var dev = new decimal[n];
		var cumdev = new decimal[n];

		for (var i = 0; i < n; i++)
			dev[i] = values[i] - mean;

		cumdev[0] = dev[0];

		for (var i = 1; i < n; i++)
			cumdev[i] = cumdev[i - 1] + dev[i];

		var maxCum = cumdev.Max();
		var minCum = cumdev.Min();
		var range = maxCum - minCum;

		var sumSqr = 0m;
		for (var i = 0; i < n; i++)
			sumSqr += (values[i] - mean) * (values[i] - mean);

		var std = (decimal)Math.Sqrt((double)(sumSqr / n));

		if (std == 0)
			return null;

		var RS = range / std;
		var H = RS.Log() / ((decimal)n).Log();

		return H;
	}
}