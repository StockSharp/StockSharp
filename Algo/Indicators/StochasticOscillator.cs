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
[IndicatorOut(typeof(StochasticOscillatorValue))]
public class StochasticOscillator : BaseComplexIndicator<StochasticOscillatorValue>
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
	protected override StochasticOscillatorValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="StochasticOscillator"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StochasticOscillatorValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="StochasticOscillator"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class StochasticOscillatorValue(StochasticOscillator indicator, DateTimeOffset time) : ComplexIndicatorValue<StochasticOscillator>(indicator, time)
{
	/// <summary>
	/// Gets the %K value.
	/// </summary>
	public IIndicatorValue KValue => this[TypedIndicator.K];

	/// <summary>
	/// Gets the %K value.
	/// </summary>
	[Browsable(false)]
	public decimal? K => KValue.ToNullableDecimal();

	/// <summary>
	/// Gets the %D value.
	/// </summary>
	public IIndicatorValue DValue => this[TypedIndicator.D];

	/// <summary>
	/// Gets the %D value.
	/// </summary>
	[Browsable(false)]
	public decimal? D => DValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"K={K}, D={D}";
}
