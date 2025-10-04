namespace StockSharp.Algo.Indicators;

/// <summary>
/// Relative Vigor Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/rvi.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RVIKey,
	Description = LocalizedStrings.RelativeVigorIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/rvi.html")]
[IndicatorOut(typeof(IRelativeVigorIndexValue))]
public class RelativeVigorIndex : BaseComplexIndicator<IRelativeVigorIndexValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RelativeVigorIndex"/>.
	/// </summary>
	public RelativeVigorIndex()
		: this(new(), new())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RelativeVigorIndex"/>.
	/// </summary>
	/// <param name="average">Average indicator part.</param>
	/// <param name="signal">Signaling part of indicator.</param>
	public RelativeVigorIndex(RelativeVigorIndexAverage average, RelativeVigorIndexSignal signal)
		: base(average, signal)
	{
		Average = average;
		Signal = signal;

		Mode = ComplexIndicatorModes.Sequence;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <summary>
	/// Average indicator part.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AverageKey,
		Description = LocalizedStrings.AveragePartKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public RelativeVigorIndexAverage Average { get; }

	/// <summary>
	/// Signaling part of indicator.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SignalKey,
		Description = LocalizedStrings.SignalPartKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public RelativeVigorIndexSignal Signal { get; }

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" A={Average.Length} S={Signal.Length}";

	/// <inheritdoc />
	protected override IRelativeVigorIndexValue CreateValue(DateTimeOffset time)
		=> new RelativeVigorIndexValue(this, time);
}

/// <summary>
/// <see cref="RelativeVigorIndex"/> indicator value.
/// </summary>
public interface IRelativeVigorIndexValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the <see cref="RelativeVigorIndex.Average"/> value.
	/// </summary>
	IIndicatorValue AverageValue { get; }

	/// <summary>
	/// Gets the <see cref="RelativeVigorIndex.Average"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Average { get; }

	/// <summary>
	/// Gets the <see cref="RelativeVigorIndex.Signal"/> value.
	/// </summary>
	IIndicatorValue SignalValue { get; }

	/// <summary>
	/// Gets the <see cref="RelativeVigorIndex.Signal"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Signal { get; }
}

class RelativeVigorIndexValue(RelativeVigorIndex indicator, DateTimeOffset time) : ComplexIndicatorValue<RelativeVigorIndex>(indicator, time), IRelativeVigorIndexValue
{
	public IIndicatorValue AverageValue => this[TypedIndicator.Average];
	public decimal? Average => AverageValue.ToNullableDecimal(TypedIndicator.Source);

	public IIndicatorValue SignalValue => this[TypedIndicator.Signal];
	public decimal? Signal => SignalValue.ToNullableDecimal(TypedIndicator.Source);


	public override string ToString() => $"Average={Average}, Signal={Signal}";
}
