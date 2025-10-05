namespace StockSharp.Algo.Indicators;

/// <summary>
/// Convergence/divergence of moving averages. Histogram.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/macd_histogram.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MACDHistogramKey,
	Description = LocalizedStrings.HistogramDescKey)]
[Doc("topics/api/indicators/list_of_indicators/macd_histogram.html")]
[IndicatorOut(typeof(IMacdHistogramValue))]
public class MovingAverageConvergenceDivergenceHistogram : BaseComplexIndicator<IMacdHistogramValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceHistogram"/>.
	/// </summary>
	public MovingAverageConvergenceDivergenceHistogram()
		: this(new(), new() { Length = 9 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceHistogram"/>.
	/// </summary>
	/// <param name="macd">Convergence/divergence of moving averages.</param>
	/// <param name="signalMa">Signaling Moving Average.</param>
	public MovingAverageConvergenceDivergenceHistogram(MovingAverageConvergenceDivergence macd, ExponentialMovingAverage signalMa)
		: base(macd, signalMa)
	{
		Macd = macd;
		SignalMa = signalMa;
		Mode = ComplexIndicatorModes.Sequence;
	}

	/// <summary>
	/// Convergence/divergence of moving averages.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MACDKey,
		Description = LocalizedStrings.MACDDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public MovingAverageConvergenceDivergence Macd { get; }

	/// <summary>
	/// Signaling Moving Average.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SignalMaKey,
		Description = LocalizedStrings.SignalMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public ExponentialMovingAverage SignalMa { get; }

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" L={Macd.LongMa.Length} S={Macd.ShortMa.Length} Sig={SignalMa.Length}";

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var macdValue = Macd.Process(input);
		var signalValue = Macd.IsFormed ? SignalMa.Process(macdValue) : new DecimalIndicatorValue(SignalMa, 0, input.Time) { IsFinal = input.IsFinal };

		var value = new MovingAverageConvergenceDivergenceHistogramValue(this, input.Time);
		value.Add(Macd, macdValue);
		value.Add(SignalMa, signalValue);
		return value;
	}

	/// <inheritdoc />
	protected override IMacdHistogramValue CreateValue(DateTimeOffset time)
		=> new MovingAverageConvergenceDivergenceHistogramValue(this, time);
}

/// <summary>
/// <see cref="MovingAverageConvergenceDivergenceHistogram"/> indicator value.
/// </summary>
public interface IMacdHistogramValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the MACD value.
	/// </summary>
	IIndicatorValue MacdValue { get; }

	/// <summary>
	/// Gets the MACD value.
	/// </summary>
	[Browsable(false)]
	decimal? Macd { get; }

	/// <summary>
	/// Gets the signal line value.
	/// </summary>
	IIndicatorValue SignalValue { get; }

	/// <summary>
	/// Gets the signal line value.
	/// </summary>
	[Browsable(false)]
	decimal? Signal { get; }
}

/// <summary>
/// MovingAverageConvergenceDivergenceHistogram indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceHistogramValue"/> class.
/// </remarks>
/// <param name="indicator">The parent MovingAverageConvergenceDivergenceHistogram indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class MovingAverageConvergenceDivergenceHistogramValue(MovingAverageConvergenceDivergenceHistogram indicator, DateTimeOffset time) : ComplexIndicatorValue<MovingAverageConvergenceDivergenceHistogram>(indicator, time), IMacdHistogramValue
{
	/// <inheritdoc />
	public IIndicatorValue MacdValue => this[TypedIndicator.Macd];
	/// <inheritdoc />
	public decimal? Macd => MacdValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue SignalValue => this[TypedIndicator.SignalMa];
	/// <inheritdoc />
	public decimal? Signal => SignalValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"Macd={Macd}, Signal={Signal}";
}
