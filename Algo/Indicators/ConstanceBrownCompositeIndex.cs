namespace StockSharp.Algo.Indicators;

/// <summary>
/// Constance Brown Composite Index indicator.
/// </summary>
[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CBCIKey,
		Description = LocalizedStrings.ConstanceBrownCompositeIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/constance_brown_composite_index.html")]
[IndicatorOut(typeof(ConstanceBrownCompositeIndexValue))]
public class ConstanceBrownCompositeIndex : BaseComplexIndicator<ConstanceBrownCompositeIndexValue>
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

			var rsi = rsiValue.ToDecimal();

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
	protected override ConstanceBrownCompositeIndexValue CreateValue(DateTimeOffset time)
		=> new(this, time);
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
/// <remarks>
/// Initializes a new instance of the <see cref="ConstanceBrownCompositeIndexValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="ConstanceBrownCompositeIndex"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class ConstanceBrownCompositeIndexValue(ConstanceBrownCompositeIndex indicator, DateTimeOffset time) : ComplexIndicatorValue<ConstanceBrownCompositeIndex>(indicator, time)
{
	/// <summary>
	/// Gets the RSI component.
	/// </summary>
	public IIndicatorValue RsiValue => this[TypedIndicator.Rsi];

	/// <summary>
	/// Gets the RSI component.
	/// </summary>
	[Browsable(false)]
	public decimal? Rsi => RsiValue.ToNullableDecimal();

	/// <summary>
	/// Gets the stochastic component.
	/// </summary>
	public IIndicatorValue StochValue => this[TypedIndicator.Stoch];

	/// <summary>
	/// Gets the stochastic component.
	/// </summary>
	[Browsable(false)]
	public decimal? Stoch => StochValue.ToNullableDecimal();

	/// <summary>
	/// Gets the composite index line.
	/// </summary>
	public IIndicatorValue CompositeIndexLineValue => this[TypedIndicator.CompositeIndexLine];

	/// <summary>
	/// Gets the composite index line.
	/// </summary>
	[Browsable(false)]
	public decimal? CompositeIndexLine => CompositeIndexLineValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"Rsi={Rsi}, Stoch={Stoch}, CompositeIndexLine={CompositeIndexLine}";
}
