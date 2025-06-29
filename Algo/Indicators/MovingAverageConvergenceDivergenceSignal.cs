namespace StockSharp.Algo.Indicators;

/// <summary>
/// Convergence/divergence of moving averages with signal line.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/macd_with_signal_line.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MACDSignalKey,
	Description = LocalizedStrings.MACDSignalDescKey)]
[Doc("topics/api/indicators/list_of_indicators/macd_with_signal_line.html")]
[IndicatorOut(typeof(MovingAverageConvergenceDivergenceSignalValue))]
public class MovingAverageConvergenceDivergenceSignal : BaseComplexIndicator<MovingAverageConvergenceDivergenceSignalValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceSignal"/>.
	/// </summary>
	public MovingAverageConvergenceDivergenceSignal()
		: this(new(), new() { Length = 9 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceSignal"/>.
	/// </summary>
	/// <param name="macd">Convergence/divergence of moving averages.</param>
	/// <param name="signalMa">Signaling Moving Average.</param>
	public MovingAverageConvergenceDivergenceSignal(MovingAverageConvergenceDivergence macd, ExponentialMovingAverage signalMa)
		: base(macd, signalMa)
	{
		Macd = macd;
		SignalMa = signalMa;
		Mode = ComplexIndicatorModes.Sequence;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

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
	protected override MovingAverageConvergenceDivergenceSignalValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="MovingAverageConvergenceDivergenceSignal"/> indicator value.
/// </summary>
public class MovingAverageConvergenceDivergenceSignalValue : ComplexIndicatorValue<MovingAverageConvergenceDivergenceSignal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceSignalValue"/>.
	/// </summary>
	/// <param name="indicator"><see cref="MovingAverageConvergenceDivergenceSignal"/></param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public MovingAverageConvergenceDivergenceSignalValue(MovingAverageConvergenceDivergenceSignal indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <summary>
	/// Gets the MACD value.
	/// </summary>
	public decimal Macd => GetInnerDecimal(TypedIndicator.Macd);

	/// <summary>
	/// Gets the signal line value.
	/// </summary>
	public decimal Signal => GetInnerDecimal(TypedIndicator.SignalMa);
}
