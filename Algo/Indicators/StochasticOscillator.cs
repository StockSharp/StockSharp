namespace StockSharp.Algo.Indicators;

/// <summary>
/// The stochastic oscillator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/stochastic_oscillator.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.STOCHKey,
	Description = LocalizedStrings.StochasticOscillatorKey)]
[Doc("topics/api/indicators/list_of_indicators/stochastic_oscillator.html")]
[IndicatorOut(typeof(IStochasticOscillatorValue))]
public class StochasticOscillator : BaseComplexIndicator<IStochasticOscillatorValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticOscillator"/>.
	/// </summary>
	public StochasticOscillator()
	{
		AddInner(K = new());
		AddInner(D = new() { Length = 3 });

		Mode = ComplexIndicatorModes.Sequence;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <summary>
	/// %K.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.KKey,
		Description = LocalizedStrings.KKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public StochasticK K { get; }

	/// <summary>
	/// %D.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DKey,
		Description = LocalizedStrings.DKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SimpleMovingAverage D { get; }

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" %K={K.Length} %D={D.Length}";

	/// <inheritdoc />
	protected override IStochasticOscillatorValue CreateValue(DateTime time)
		=> new StochasticOscillatorValue(this, time);
}

/// <summary>
/// <see cref="StochasticOscillator"/> indicator value.
/// </summary>
public interface IStochasticOscillatorValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the %K value.
	/// </summary>
	IIndicatorValue KValue { get; }

	/// <summary>
	/// Gets the %K value.
	/// </summary>
	[Browsable(false)]
	decimal? K { get; }

	/// <summary>
	/// Gets the %D value.
	/// </summary>
	IIndicatorValue DValue { get; }

	/// <summary>
	/// Gets the %D value.
	/// </summary>
	[Browsable(false)]
	decimal? D { get; }
}

/// <summary>
/// Stochastic Oscillator indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StochasticOscillatorValue"/> class.
/// </remarks>
/// <param name="indicator">The parent Stochastic Oscillator indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class StochasticOscillatorValue(StochasticOscillator indicator, DateTime time) : ComplexIndicatorValue<StochasticOscillator>(indicator, time), IStochasticOscillatorValue
{
	/// <inheritdoc />
	public IIndicatorValue KValue => this[TypedIndicator.K];
	/// <inheritdoc />
	public decimal? K => KValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue DValue => this[TypedIndicator.D];
	/// <inheritdoc />
	public decimal? D => DValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"K={K}, D={D}";
}
