namespace StockSharp.Algo.Indicators;

/// <summary>
/// Fractal Dimension Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/fractal_dimension.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FractalDimensionKey,
	Description = LocalizedStrings.FractalDimensionDescKey)]
[Doc("topics/api/indicators/list_of_indicators/fractal_dimension.html")]
public class FractalDimension : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FractalDimension"/>.
	/// </summary>
	public FractalDimension()
	{
		Length = 30;

		Buffer.MinComparer = new DecimalOperator();
		Buffer.MaxComparer = new DecimalOperator();
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var price = input.GetValue<decimal>();

		var buffer = Buffer;

		if (input.IsFinal)
			buffer.PushBack(price);

		if (buffer.Count < 2)
			return new DecimalIndicatorValue(this, 1.5m, input.Time);

		var maxHigh = buffer.Max.Value;
		var minLow = buffer.Min.Value;

		var startIdx = input.IsFinal ? 1 : 2;

		// Calculate the length of the price path
		var pathLength = 0m;
		for (var i = startIdx; i < buffer.Count; i++)
		{
			var prev = buffer[i - 1];
			var curr = buffer[i];
			pathLength += (curr - prev).Abs();
		}

		if (!input.IsFinal)
		{
			maxHigh = maxHigh.Max(price);
			minLow = minLow.Max(price);

			pathLength += (price - buffer.Last()).Abs();
		}

		// Calculate FDI
		decimal fractalDimension;

		var range = maxHigh - minLow;

		if (pathLength == 0 || range == 0)
		{
			fractalDimension = 1.5m;
		}
		else
		{
			// FDI formula: 1 + (log(pathLength) - log(range)) / log(2 * (Length-1))
			var logPathLength = pathLength.Log();
			var logRange = range.Log();
			var logDenominator = (2m * (Length - 1)).Log();

			if (logDenominator == 0)
			{
				fractalDimension = 1.5m;
			}
			else
			{
				fractalDimension = 1m + (logPathLength - logRange) / logDenominator;
			}
		}

		// Clamp between 1.0 and 2.0
		fractalDimension = 1.0m.Max(2.0m.Min(fractalDimension));

		return new DecimalIndicatorValue(this, fractalDimension, input.Time);
	}
}