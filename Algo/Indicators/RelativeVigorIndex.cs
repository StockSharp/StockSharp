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
[IndicatorOut(typeof(RelativeVigorIndexValue))]
public class RelativeVigorIndex : BaseComplexIndicator<RelativeVigorIndexValue>
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
	protected override RelativeVigorIndexValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="RelativeVigorIndex"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RelativeVigorIndexValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="RelativeVigorIndex"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class RelativeVigorIndexValue(RelativeVigorIndex indicator, DateTimeOffset time) : ComplexIndicatorValue<RelativeVigorIndex>(indicator, time)
{
	/// <summary>
	/// Gets the <see cref="RelativeVigorIndex.Average"/> value.
	/// </summary>
	public IIndicatorValue AverageValue => this[TypedIndicator.Average];

	/// <summary>
	/// Gets the <see cref="RelativeVigorIndex.Average"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Average => AverageValue.ToNullableDecimal();

	/// <summary>
	/// Gets the <see cref="RelativeVigorIndex.Signal"/> value.
	/// </summary>
	public IIndicatorValue SignalValue => this[TypedIndicator.Signal];

	/// <summary>
	/// Gets the <see cref="RelativeVigorIndex.Signal"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Signal => SignalValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"Average={Average}, Signal={Signal}";
}
