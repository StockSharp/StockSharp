namespace StockSharp.Algo.Indicators;

using Ecng.ComponentModel;

/// <summary>
/// Gator oscillator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/gator_oscillator.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.GatorKey,
	Description = LocalizedStrings.GatorOscillatorKey)]
[Doc("topics/api/indicators/list_of_indicators/gator_oscillator.html")]
public class GatorOscillator : BaseComplexIndicator
{
	private readonly Alligator _alligator;

	/// <summary>
	/// Initializes a new instance of the <see cref="GatorOscillator"/>.
	/// </summary>
	public GatorOscillator()
	{
		_alligator = new Alligator();
		AddInner(Histogram1 = new(_alligator.Jaw, _alligator.Lips, false));
		AddInner(Histogram2 = new(_alligator.Lips, _alligator.Teeth, true));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GatorOscillator"/>.
	/// </summary>
	/// <param name="alligator">Alligator.</param>
	/// <param name="histogram1">Top histogram.</param>
	/// <param name="histogram2">Lower histogram.</param>
	public GatorOscillator(Alligator alligator, GatorHistogram histogram1, GatorHistogram histogram2)
		: base(histogram1, histogram2)
	{
		_alligator = alligator ?? throw new ArgumentNullException(nameof(alligator));
		Histogram1 = histogram1;
		Histogram2 = histogram2;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <summary>
	/// Top histogram.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UpKey,
		Description = LocalizedStrings.TopHistogramKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public GatorHistogram Histogram1 { get; }

	/// <summary>
	/// Lower histogram.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DownKey,
		Description = LocalizedStrings.LowHistogramKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public GatorHistogram Histogram2 { get; }

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _alligator.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		_alligator.Process(input);

		return base.OnProcess(input);
	}
}
