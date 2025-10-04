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
	protected override IStochasticOscillatorValue CreateValue(DateTimeOffset time)
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

class StochasticOscillatorValue(StochasticOscillator indicator, DateTimeOffset time) : ComplexIndicatorValue<StochasticOscillator>(indicator, time), IStochasticOscillatorValue
{
	public IIndicatorValue KValue => this[TypedIndicator.K];
	public decimal? K => KValue.ToNullableDecimal(TypedIndicator.Source);

	public IIndicatorValue DValue => this[TypedIndicator.D];
	public decimal? D => DValue.ToNullableDecimal(TypedIndicator.Source);

	public override string ToString() => $"K={K}, D={D}";
}
