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
}