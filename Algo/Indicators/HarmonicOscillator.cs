namespace StockSharp.Algo.Indicators;

/// <summary>
/// Harmonic Oscillator indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.HOKey,
	Description = LocalizedStrings.HarmonicOscillatorKey)]
[Doc("topics/api/indicators/list_of_indicators/harmonic_oscillator.html")]
public class HarmonicOscillator : LengthIndicator<decimal>
{
	private decimal[] _sinValues;

	/// <summary>
	/// Initializes a new instance of the <see cref="HarmonicOscillator"/>.
	/// </summary>
	public HarmonicOscillator()
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(price);
		}

		if (IsFormed)
		{
			var sum = 0m;
			var count = Buffer.Count.Min(Length);

			for (var i = 0; i < count; i++)
			{
				var value = (i == 0 && !input.IsFinal) ? price : Buffer[Buffer.Count - 1 - i];
				sum += value * _sinValues[i];
			}

			var result = sum / Length;
			return result;
		}

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_sinValues = new decimal[Length];

		for (var i = 0; i < Length; i++)
			_sinValues[i] = (decimal)Math.Sin(2 * Math.PI * i / Length);
	}
}
