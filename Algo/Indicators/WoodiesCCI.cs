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
[IndicatorOut(typeof(WoodiesCCIValue))]
public class WoodiesCCI : BaseComplexIndicator<WoodiesCCIValue>
{
	/// <summary>
	/// CCI line.
	/// </summary>
	[Browsable(false)]
	public CommodityChannelIndex Cci { get; }

	/// <summary>
	/// SMA line.
	/// </summary>
	[Browsable(false)]
	public SimpleMovingAverage Sma { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="WoodiesCCI"/>.
	/// </summary>
	public WoodiesCCI()
	{
		Cci = new() { Length = 14 };
		Sma = new() { Length = 6 };

		AddInner(Cci);
		AddInner(Sma);

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
		get => Cci.Length;
		set => Cci.Length = value;
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
		get => Sma.Length;
		set => Sma.Length = value;
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" CCI({Length}), SMA({SMALength})";

	/// <inheritdoc />
	protected override WoodiesCCIValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="WoodiesCCI"/> indicator value.
/// </summary>
public class WoodiesCCIValue : ComplexIndicatorValue<WoodiesCCI>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="WoodiesCCIValue"/>.
	/// </summary>
	/// <param name="indicator"><see cref="WoodiesCCI"/></param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public WoodiesCCIValue(WoodiesCCI indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <summary>
	/// Gets the CCI value.
	/// </summary>
	public decimal Cci => InnerValues[TypedIndicator.Cci].ToDecimal();

	/// <summary>
	/// Gets the SMA value.
	/// </summary>
	public decimal Sma => InnerValues[TypedIndicator.Sma].ToDecimal();
}
