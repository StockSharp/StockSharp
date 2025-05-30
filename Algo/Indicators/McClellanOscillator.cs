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
	private readonly ExponentialMovingAverage _ema19;
	private readonly ExponentialMovingAverage _ema39;

	/// <summary>
	/// Initializes a new instance of the <see cref="McClellanOscillator"/>.
	/// </summary>
	public McClellanOscillator()
	{
		_ema19 = new() { Length = 19 };
		_ema39 = new() { Length = 39 };
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override int NumValuesToInitialize => _ema19.NumValuesToInitialize.Max(_ema39.NumValuesToInitialize);

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var ema19Value = _ema19.Process(input);
		var ema39Value = _ema39.Process(input);

		if (_ema19.IsFormed && _ema39.IsFormed)
		{
			var oscillator = ema19Value.ToDecimal() - ema39Value.ToDecimal();

			if (input.IsFinal)
				IsFormed = true;

			return new DecimalIndicatorValue(this, oscillator, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_ema19.Reset();
		_ema39.Reset();
		base.Reset();
	}
}
