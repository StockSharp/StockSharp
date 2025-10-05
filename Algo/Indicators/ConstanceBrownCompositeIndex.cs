namespace StockSharp.Algo.Indicators;

/// <summary>
/// Constance Brown Composite Index indicator.
/// </summary>
[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CBCIKey,
		Description = LocalizedStrings.ConstanceBrownCompositeIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/constance_brown_composite_index.html")]
[IndicatorOut(typeof(IConstanceBrownCompositeIndexValue))]
public class ConstanceBrownCompositeIndex : BaseComplexIndicator<IConstanceBrownCompositeIndexValue>
{
	/// <summary>
	/// RSI part.
	/// </summary>
	[Browsable(false)]
	public RelativeStrengthIndex Rsi { get; }

	/// <summary>
	/// Stochastic oscillator part.
	/// </summary>
	[Browsable(false)]
	public StochasticOscillator Stoch { get; }

	private readonly CompositeIndexLine _compositeIndexLine;

	/// <summary>
	/// Composite index line.
	/// </summary>
	[Browsable(false)]
	public CompositeIndexLine CompositeIndexLine => _compositeIndexLine;

	/// <summary>
	/// Initializes a new instance of the <see cref="ConstanceBrownCompositeIndex"/>.
	/// </summary>
	public ConstanceBrownCompositeIndex()
		: this(new() { Length = 14 }, new())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ConstanceBrownCompositeIndex"/>.
	/// </summary>
	/// <param name="rsi">Relative Strength Index.</param>
	/// <param name="stoch">Stochastic Oscillator.</param>
	public ConstanceBrownCompositeIndex(RelativeStrengthIndex rsi, StochasticOscillator stoch)
	{
		Rsi = rsi ?? throw new ArgumentNullException(nameof(rsi));
		Stoch = stoch ?? throw new ArgumentNullException(nameof(stoch));
		_compositeIndexLine = new();

		AddInner(Rsi);
		AddInner(Stoch);
		AddInner(CompositeIndexLine);
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <summary>
	/// Period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => Rsi.Length;
		set => Rsi.Length = value;
	}

	/// <summary>
	/// <see cref="StochasticOscillator.K"/>
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.KKey,
		Description = LocalizedStrings.KKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int StochasticKPeriod
	{
		get => Stoch.K.Length;
		set => Stoch.K.Length = value;
	}

	/// <summary>
	/// <see cref="StochasticOscillator.D"/>
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DKey,
		Description = LocalizedStrings.DKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int StochasticDPeriod
	{
		get => Stoch.D.Length;
		set => Stoch.D.Length = value;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new ConstanceBrownCompositeIndexValue(this, input.Time);

		var rsiValue = Rsi.Process(input);
		var stochValue = (StochasticOscillatorValue)Stoch.Process(input);

		result.Add(Rsi, rsiValue);
		result.Add(Stoch, stochValue[Stoch.K]);

		if (Rsi.IsFormed && Stoch.IsFormed)
		{
			if (input.IsFinal)
				IsFormed = true;

			var rsi = rsiValue.ToDecimal(Source);

			if (stochValue.K is not decimal stochK ||
				stochValue.D is not decimal stochD)
				return result;

			var cbci = (rsi + stochK + stochD) / 3;

			var compositeValue = CompositeIndexLine.Process(input, cbci);
			result.Add(_compositeIndexLine, compositeValue);
		}

		return result;
	}
	/// <inheritdoc />
	protected override IConstanceBrownCompositeIndexValue CreateValue(DateTimeOffset time)
		=> new ConstanceBrownCompositeIndexValue(this, time);
}

/// <summary>
/// Composite Index Line indicator.
/// </summary>
[IndicatorHidden]
public class CompositeIndexLine : BaseIndicator
{
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		return input;
	}
}

/// <summary>
/// <see cref="ConstanceBrownCompositeIndex"/> indicator value.
/// </summary>
public interface IConstanceBrownCompositeIndexValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the RSI component.
	/// </summary>
	IIndicatorValue RsiValue { get; }

	/// <summary>
	/// Gets the RSI component.
	/// </summary>
	[Browsable(false)]
	decimal? Rsi { get; }

	/// <summary>
	/// Gets the stochastic component.
	/// </summary>
	IIndicatorValue StochValue { get; }

	/// <summary>
	/// Gets the stochastic component.
	/// </summary>
	[Browsable(false)]
	decimal? Stoch { get; }

	/// <summary>
	/// Gets the composite index line.
	/// </summary>
	IIndicatorValue CompositeIndexLineValue { get; }

	/// <summary>
	/// Gets the composite index line.
	/// </summary>
	[Browsable(false)]
	decimal? CompositeIndexLine { get; }
}

/// <summary>
/// ConstanceBrownCompositeIndex indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConstanceBrownCompositeIndexValue"/> class.
/// </remarks>
/// <param name="indicator">The parent ConstanceBrownCompositeIndex indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class ConstanceBrownCompositeIndexValue(ConstanceBrownCompositeIndex indicator, DateTimeOffset time) : ComplexIndicatorValue<ConstanceBrownCompositeIndex>(indicator, time), IConstanceBrownCompositeIndexValue
{
	/// <inheritdoc />
	public IIndicatorValue RsiValue => this[TypedIndicator.Rsi];
	/// <inheritdoc />
	public decimal? Rsi => RsiValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue StochValue => this[TypedIndicator.Stoch];
	/// <inheritdoc />
	public decimal? Stoch => StochValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue CompositeIndexLineValue => this[TypedIndicator.CompositeIndexLine];
	/// <inheritdoc />
	public decimal? CompositeIndexLine => CompositeIndexLineValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"Rsi={Rsi}, Stoch={Stoch}, CompositeIndexLine={CompositeIndexLine}";
}
