namespace StockSharp.Algo.Indicators;

/// <summary>
/// Woodies CCI.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.WCCIKey,
	Description = LocalizedStrings.WoodiesCCIKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/woodies_cci.html")]
public class WoodiesCCI : BaseComplexIndicator
{
	private readonly CommodityChannelIndex _cci;
	private readonly SimpleMovingAverage _sma;

	/// <summary>
	/// Initializes a new instance of the <see cref="WoodiesCCI"/>.
	/// </summary>
	public WoodiesCCI()
	{
		_cci = new() { Length = 14 };
		_sma = new() { Length = 6 };

		AddInner(_cci);
		AddInner(_sma);

		Mode = ComplexIndicatorModes.Sequence;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <summary>
	/// The indicator period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => _cci.Length;
		set => _cci.Length = value;
	}

	/// <summary>
	/// The period length of SMA.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SMAKey,
		Description = LocalizedStrings.SimpleMovingAverageKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int SMALength
	{
		get => _sma.Length;
		set => _sma.Length = value;
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" CCI({Length}), SMA({SMALength})";
}
