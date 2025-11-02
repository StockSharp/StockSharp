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
[IndicatorOut(typeof(IMovingAverageConvergenceDivergenceSignalValue))]
public class MovingAverageConvergenceDivergenceSignal : BaseComplexIndicator<IMovingAverageConvergenceDivergenceSignalValue>
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
	protected override IMovingAverageConvergenceDivergenceSignalValue CreateValue(DateTime time)
		=> new MovingAverageConvergenceDivergenceSignalValue(this, time);
}

/// <summary>
/// <see cref="MovingAverageConvergenceDivergenceSignal"/> indicator value.
/// </summary>
public interface IMovingAverageConvergenceDivergenceSignalValue : IComplexIndicatorValue
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
/// MovingAverageConvergenceDivergenceSignal indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceSignalValue"/> class.
/// </remarks>
/// <param name="indicator">The parent MovingAverageConvergenceDivergenceSignal indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class MovingAverageConvergenceDivergenceSignalValue(MovingAverageConvergenceDivergenceSignal indicator, DateTime time) : ComplexIndicatorValue<MovingAverageConvergenceDivergenceSignal>(indicator, time), IMovingAverageConvergenceDivergenceSignalValue
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
