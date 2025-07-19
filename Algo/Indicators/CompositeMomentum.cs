namespace StockSharp.Algo.Indicators;

/// <summary>
/// Composite Momentum indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CMKey,
	Description = LocalizedStrings.CompositeMomentumKey)]
[Doc("topics/api/indicators/list_of_indicators/composite_momentum.html")]
[IndicatorOut(typeof(CompositeMomentumValue))]
public class CompositeMomentum : BaseComplexIndicator<CompositeMomentumValue>
{
	private readonly RateOfChange _roc1;
	private readonly RateOfChange _roc2;
	private readonly RelativeStrengthIndex _rsi;
	private readonly ExponentialMovingAverage _emaFast;
	private readonly ExponentialMovingAverage _emaSlow;

	/// <summary>
	/// SMA used for final smoothing.
	/// </summary>
	[Browsable(false)]
	public SimpleMovingAverage Sma { get; }

	/// <summary>
	/// Composite momentum line.
	/// </summary>
	[Browsable(false)]
	public CompositeMomentumLine CompositeLine { get; }

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
		Sma = sma ?? throw new ArgumentNullException(nameof(sma));
		CompositeLine = new();

		AddInner(Sma);
		AddInner(CompositeLine);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_roc1.Reset();
		_roc2.Reset();
		_rsi.Reset();
		_emaFast.Reset();
		_emaSlow.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override int NumValuesToInitialize
		=> _roc1.NumValuesToInitialize
		.Max(_roc2.NumValuesToInitialize)
		.Max(_rsi.NumValuesToInitialize)
		.Max(_emaFast.NumValuesToInitialize)
		.Max(_emaSlow.NumValuesToInitialize)
		+ Sma.NumValuesToInitialize - 1;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new CompositeMomentumValue(this, input.Time);

		var shortRocValue = _roc1.Process(input);
		var longRocValue = _roc2.Process(input);
		var rsiValue = _rsi.Process(input);
		var emaFastValue = _emaFast.Process(input);
		var emaSlowValue = _emaSlow.Process(input);

		if (_roc1.IsFormed && _roc2.IsFormed && _rsi.IsFormed && _emaFast.IsFormed && _emaSlow.IsFormed)
		{
			var normalizedShortRoc = shortRocValue.ToDecimal() / 100m;
			var normalizedLongRoc = longRocValue.ToDecimal() / 100m;
			var normalizedRsi = (rsiValue.ToDecimal() - 50m) / 50m;

			var macdLine = (emaFastValue.ToDecimal() - emaSlowValue.ToDecimal()) / emaSlowValue.ToDecimal();

			var compMomentum = (normalizedShortRoc + normalizedLongRoc + normalizedRsi + macdLine) / 4m;
			compMomentum *= 100m;

			var compositeValue = CompositeLine.Process(compMomentum, input.Time, input.IsFinal);
			result.Add(CompositeLine, compositeValue);
			result.Add(Sma, Sma.Process(compositeValue));

			if (input.IsFinal && Sma.IsFormed)
				IsFormed = true;
		}

		return result;
	}

	/// <inheritdoc />
	protected override CompositeMomentumValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// Composite line for the main <see cref="CompositeMomentum"/> value.
/// </summary>
[IndicatorHidden]
public class CompositeMomentumLine : BaseIndicator
{
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		return input;
	}
}

/// <summary>
/// <see cref="CompositeMomentum"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CompositeMomentumValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="CompositeMomentum"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class CompositeMomentumValue(CompositeMomentum indicator, DateTimeOffset time) : ComplexIndicatorValue<CompositeMomentum>(indicator, time)
{
	/// <summary>
	/// Gets the SMA value.
	/// </summary>
	public IIndicatorValue SmaValue => this[TypedIndicator.Sma];

	/// <summary>
	/// Gets the SMA value.
	/// </summary>
	[Browsable(false)]
	public decimal? Sma => SmaValue.ToNullableDecimal();

	/// <summary>
	/// Gets the composite momentum line.
	/// </summary>
	public IIndicatorValue CompositeLineValue => this[TypedIndicator.CompositeLine];

	/// <summary>
	/// Gets the composite momentum line.
	/// </summary>
	[Browsable(false)]
	public decimal? CompositeLine => CompositeLineValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"Sma={Sma}, CompositeLine={CompositeLine}";
}
