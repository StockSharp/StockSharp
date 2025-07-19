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
[IndicatorOut(typeof(MovingAverageConvergenceDivergenceHistogramValue))]
public class MovingAverageConvergenceDivergenceHistogram : BaseComplexIndicator<MovingAverageConvergenceDivergenceHistogramValue>
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
	protected override MovingAverageConvergenceDivergenceHistogramValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="MovingAverageConvergenceDivergenceHistogram"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceHistogramValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="MovingAverageConvergenceDivergenceHistogram"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class MovingAverageConvergenceDivergenceHistogramValue(MovingAverageConvergenceDivergenceHistogram indicator, DateTimeOffset time) : ComplexIndicatorValue<MovingAverageConvergenceDivergenceHistogram>(indicator, time)
{
	/// <summary>
	/// Gets the MACD value.
	/// </summary>
	public IIndicatorValue MacdValue => this[TypedIndicator.Macd];

	/// <summary>
	/// Gets the MACD value.
	/// </summary>
	[Browsable(false)]
	public decimal? Macd => MacdValue.ToNullableDecimal();

	/// <summary>
	/// Gets the signal line value.
	/// </summary>
	public IIndicatorValue SignalValue => this[TypedIndicator.SignalMa];

	/// <summary>
	/// Gets the signal line value.
	/// </summary>
	[Browsable(false)]
	public decimal? Signal => SignalValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"Macd={Macd}, Signal={Signal}";
}
