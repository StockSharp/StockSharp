namespace StockSharp.Algo.Indicators;

/// <summary>
/// Composite Momentum indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CMKey,
	Description = LocalizedStrings.CompositeMomentumKey)]
[Doc("topics/api/indicators/list_of_indicators/composite_momentum.html")]
[IndicatorOut(typeof(ICompositeMomentumValue))]
public class CompositeMomentum : BaseComplexIndicator<ICompositeMomentumValue>
{
	/// <summary>
	/// Short-term Rate of Change.
	/// </summary>
	[Browsable(false)]
	public RateOfChange ShortRoc { get; }

	/// <summary>
	/// Long-term Rate of Change.
	/// </summary>
	[Browsable(false)]
	public RateOfChange LongRoc { get; }

	/// <summary>
	/// Relative Strength Index.
	/// </summary>
	[Browsable(false)]
	public RelativeStrengthIndex Rsi { get; }

	/// <summary>
	/// Fast Exponential Moving Average.
	/// </summary>
	[Browsable(false)]
	public ExponentialMovingAverage EmaFast { get; }

	/// <summary>
	/// Slow Exponential Moving Average.
	/// </summary>
	[Browsable(false)]
	public ExponentialMovingAverage EmaSlow { get; }

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
		ShortRoc = shortRoc ?? throw new ArgumentNullException(nameof(shortRoc));
		LongRoc = longRoc ?? throw new ArgumentNullException(nameof(longRoc));
		Rsi = rsi ?? throw new ArgumentNullException(nameof(rsi));
		EmaFast = emaFast ?? throw new ArgumentNullException(nameof(emaFast));
		EmaSlow = emaSlow ?? throw new ArgumentNullException(nameof(emaSlow));
		Sma = sma ?? throw new ArgumentNullException(nameof(sma));
		CompositeLine = new();

		AddInner(Sma);
		AddInner(CompositeLine);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		ShortRoc.Reset();
		LongRoc.Reset();
		Rsi.Reset();
		EmaFast.Reset();
		EmaSlow.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override int NumValuesToInitialize
		=> ShortRoc.NumValuesToInitialize
		.Max(LongRoc.NumValuesToInitialize)
		.Max(Rsi.NumValuesToInitialize)
		.Max(EmaFast.NumValuesToInitialize)
		.Max(EmaSlow.NumValuesToInitialize)
		+ Sma.NumValuesToInitialize - 1;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new CompositeMomentumValue(this, input.Time);

		var shortRocValue = ShortRoc.Process(input);
		var longRocValue = LongRoc.Process(input);
		var rsiValue = Rsi.Process(input);
		var emaFastValue = EmaFast.Process(input);
		var emaSlowValue = EmaSlow.Process(input);

		if (ShortRoc.IsFormed && LongRoc.IsFormed && Rsi.IsFormed && EmaFast.IsFormed && EmaSlow.IsFormed)
		{
			var normalizedShortRoc = shortRocValue.ToDecimal(Source) / 100m;
			var normalizedLongRoc = longRocValue.ToDecimal(Source) / 100m;
			var normalizedRsi = (rsiValue.ToDecimal(Source) - 50m) / 50m;

			var emaSlow = emaSlowValue.ToDecimal(Source);
			var macdLine = emaSlow != 0 ? (emaFastValue.ToDecimal(Source) - emaSlow) / emaSlow : 0;

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
	protected override ICompositeMomentumValue CreateValue(DateTime time)
		=> new CompositeMomentumValue(this, time);
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
public interface ICompositeMomentumValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the SMA value.
	/// </summary>
	IIndicatorValue SmaValue { get; }
	/// <summary>
	/// Gets the SMA value.
	/// </summary>
	[Browsable(false)]
	decimal? Sma { get; }
	/// <summary>
	/// Gets the composite momentum line.
	/// </summary>
	IIndicatorValue CompositeLineValue { get; }
	/// <summary>
	/// Gets the composite momentum line.
	/// </summary>
	[Browsable(false)]
	decimal? CompositeLine { get; }
}

/// <summary>
/// CompositeMomentum indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CompositeMomentumValue"/> class.
/// </remarks>
/// <param name="indicator">The parent CompositeMomentum indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class CompositeMomentumValue(CompositeMomentum indicator, DateTime time) : ComplexIndicatorValue<CompositeMomentum>(indicator, time), ICompositeMomentumValue
{
	/// <inheritdoc />
	public IIndicatorValue SmaValue => this[TypedIndicator.Sma];
	/// <inheritdoc />
	public decimal? Sma => SmaValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue CompositeLineValue => this[TypedIndicator.CompositeLine];
	/// <inheritdoc />
	public decimal? CompositeLine => CompositeLineValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"Sma={Sma}, CompositeLine={CompositeLine}";
}
