namespace StockSharp.Algo.Indicators;

/// <summary>
/// McClellan Oscillator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MCOKey,
	Description = LocalizedStrings.McClellanOscillatorKey)]
[Doc("topics/api/indicators/list_of_indicators/mcclellan_oscillator.html")]
public class McClellanOscillator : BaseIndicator
{
	/// <summary>
	/// Exponential Moving Average with length 19.
	/// </summary>
	[Browsable(false)]
	public ExponentialMovingAverage Ema19 { get; } = new() { Length = 19 };

	/// <summary>
	/// Exponential Moving Average with length 39.
	/// </summary>
	[Browsable(false)]
	public ExponentialMovingAverage Ema39 { get; } = new() { Length = 39 };

	/// <summary>
	/// Initializes a new instance of the <see cref="McClellanOscillator"/>.
	/// </summary>
	public McClellanOscillator()
	{
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override int NumValuesToInitialize => Ema19.NumValuesToInitialize.Max(Ema39.NumValuesToInitialize);

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var ema19Value = Ema19.Process(input);
		var ema39Value = Ema39.Process(input);

		if (Ema19.IsFormed && Ema39.IsFormed)
		{
			var oscillator = ema19Value.ToDecimal(Source) - ema39Value.ToDecimal(Source);

			if (input.IsFinal)
				IsFormed = true;

			return new DecimalIndicatorValue(this, oscillator, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		Ema19.Reset();
		Ema39.Reset();
		base.Reset();
	}
}
