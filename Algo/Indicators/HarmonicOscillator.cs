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

		IList<decimal> buffer;

		if (input.IsFinal)
		{
			Buffer.PushBack(price);
			buffer = Buffer;
		}
		else
		{
			buffer = [.. Buffer.Skip(1), price];
		}

		if (IsFormed)
		{
			var sum = 0m;
			var count = buffer.Count.Min(Length);

			for (var i = 0; i < count; i++)
			{
				var value = (i == 0 && !input.IsFinal) ? price : buffer[buffer.Count - 1 - i];
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
