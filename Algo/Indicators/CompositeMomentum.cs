namespace StockSharp.Algo.Indicators;

/// <summary>
/// Composite Momentum indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CMKey,
	Description = LocalizedStrings.CompositeMomentumKey)]
[Doc("topics/api/indicators/list_of_indicators/composite_momentum.html")]
public class CompositeMomentum : BaseComplexIndicator
{
	private readonly RateOfChange _roc1;
	private readonly RateOfChange _roc2;
	private readonly RelativeStrengthIndex _rsi;
	private readonly ExponentialMovingAverage _emaFast;
	private readonly ExponentialMovingAverage _emaSlow;
	private readonly SimpleMovingAverage _sma;
	private readonly CompositeMomentumLine _compositeLine;

	/// <summary>
	/// Initializes a new instance of the <see cref="CompositeMomentum"/>.
	/// </summary>
	public CompositeMomentum()
		: this(
			new() { Length = 14 },
			new() { Length = 28 },
			new() { Length = 14 },
			new() { Length = 12 },
			new() { Length = 26 },
			new() { Length = 9 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CompositeMomentum"/>.
	/// </summary>
	public CompositeMomentum(
		RateOfChange shortRoc,
		RateOfChange longRoc,
		RelativeStrengthIndex rsi,
		ExponentialMovingAverage emaFast,
		ExponentialMovingAverage emaSlow,
		SimpleMovingAverage sma)
	{
		_roc1 = shortRoc ?? throw new ArgumentNullException(nameof(shortRoc));
		_roc2 = longRoc ?? throw new ArgumentNullException(nameof(longRoc));
		_rsi = rsi ?? throw new ArgumentNullException(nameof(rsi));
		_emaFast = emaFast ?? throw new ArgumentNullException(nameof(emaFast));
		_emaSlow = emaSlow ?? throw new ArgumentNullException(nameof(emaSlow));
		_sma = sma ?? throw new ArgumentNullException(nameof(sma));
		_compositeLine = new();

		AddInner(_sma);
		AddInner(_compositeLine);
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new ComplexIndicatorValue(this, input.Time);

		var shortRocValue = _roc1.Process(input);
		var longRocValue = _roc2.Process(input);
		var rsiValue = _rsi.Process(input);
		var emaFastValue = _emaFast.Process(input);
		var emaSlowValue = _emaSlow.Process(input);

		if (_roc1.IsFormed && _roc2.IsFormed && _rsi.IsFormed && _emaFast.IsFormed && _emaSlow.IsFormed)
		{
			IsFormed = true;

			var normalizedShortRoc = shortRocValue.ToDecimal() / 100m;
			var normalizedLongRoc = longRocValue.ToDecimal() / 100m;
			var normalizedRsi = (rsiValue.ToDecimal() - 50m) / 50m;

			var macdLine = (emaFastValue.ToDecimal() - emaSlowValue.ToDecimal()) / emaSlowValue.ToDecimal();

			var compMomentum = (normalizedShortRoc + normalizedLongRoc + normalizedRsi + macdLine) / 4m;
			compMomentum *= 100m;

			var compositeValue = new DecimalIndicatorValue(this, compMomentum, input.Time) { IsFinal = input.IsFinal };
			result.Add(_compositeLine, compositeValue);
			result.Add(_sma, _sma.Process(compositeValue));
		}

		return result;
	}

	/// <summary>
	/// Composite line for the main <see cref="CompositeMomentum"/> value.
	/// </summary>
	private class CompositeMomentumLine : BaseIndicator
	{
		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			return input;
		}
	}
}
