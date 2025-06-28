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
public class StochasticOscillator : BaseComplexIndicator
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
	protected override ComplexIndicatorValue CreateValue(DateTimeOffset time)
		=> new StochasticOscillatorValue(this, time);
}

/// <summary>
/// <see cref="StochasticOscillator"/> indicator value.
/// </summary>
public class StochasticOscillatorValue : ComplexIndicatorValue<StochasticOscillator>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticOscillatorValue"/>.
	/// </summary>
	/// <param name="indicator"><see cref="StochasticOscillator"/></param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public StochasticOscillatorValue(StochasticOscillator indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <summary>
	/// Gets the %K value.
	/// </summary>
	public decimal KValue => InnerValues[Indicator.K].ToDecimal();

	/// <summary>
	/// Gets the %D value.
	/// </summary>
	public decimal DValue => InnerValues[Indicator.D].ToDecimal();
}
