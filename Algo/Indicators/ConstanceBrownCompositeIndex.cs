namespace StockSharp.Algo.Indicators;

/// <summary>
/// Constance Brown Composite Index indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CBCIKey,
	Description = LocalizedStrings.ConstanceBrownCompositeIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/constance_brown_composite_index.html")]
public class ConstanceBrownCompositeIndex : BaseComplexIndicator
{
	private class CompositeIndexLine : BaseIndicator
	{
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			return input;
		}
	}

	private readonly RelativeStrengthIndex _rsi;
	private readonly StochasticOscillator _stoch;
	private readonly CompositeIndexLine _compositeIndexLine;

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
		_rsi = rsi ?? throw new ArgumentNullException(nameof(rsi));
		_stoch = stoch ?? throw new ArgumentNullException(nameof(stoch));
		_compositeIndexLine = new();

		AddInner(_rsi);
		AddInner(_stoch);
		AddInner(_compositeIndexLine);
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
		get => _rsi.Length;
		set => _rsi.Length = value;
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
		get => _stoch.K.Length;
		set => _stoch.K.Length = value;
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
		get => _stoch.D.Length;
		set => _stoch.D.Length = value;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new ComplexIndicatorValue(this, input.Time);

		var rsiValue = _rsi.Process(input);
		var stochValue = (ComplexIndicatorValue)_stoch.Process(input);

		result.Add(_rsi, rsiValue);
		result.Add(_stoch, stochValue[_stoch.K]);

		if (_rsi.IsFormed && _stoch.IsFormed)
		{
			IsFormed = true;

			var rsi = rsiValue.ToDecimal();
			var stochK = stochValue[_stoch.K].ToDecimal();
			var stochD = stochValue[_stoch.D].ToDecimal();

			var cbci = (rsi + stochK + stochD) / 3;

			var compositeValue = _compositeIndexLine.Process(input, cbci);
			result.Add(_compositeIndexLine, compositeValue);
		}

		return result;
	}
}
