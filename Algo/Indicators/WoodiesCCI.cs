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
[IndicatorOut(typeof(IWoodiesCCIValue))]
public class WoodiesCCI : BaseComplexIndicator<IWoodiesCCIValue>
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
	protected override IWoodiesCCIValue CreateValue(DateTimeOffset time)
		=> new WoodiesCCIValue(this, time);
}

/// <summary>
/// <see cref="WoodiesCCI"/> indicator value.
/// </summary>
public interface IWoodiesCCIValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the CCI value.
	/// </summary>
	IIndicatorValue CciValue { get; }

	/// <summary>
	/// Gets the CCI value.
	/// </summary>
	[Browsable(false)]
	decimal? Cci { get; }

	/// <summary>
	/// Gets the SMA value.
	/// </summary>
	IIndicatorValue SmaValue { get; }

	/// <summary>
	/// Gets the SMA value.
	/// </summary>
	[Browsable(false)]
	decimal? Sma { get; }
}

class WoodiesCCIValue(WoodiesCCI indicator, DateTimeOffset time) : ComplexIndicatorValue<WoodiesCCI>(indicator, time), IWoodiesCCIValue
{
	public IIndicatorValue CciValue => this[TypedIndicator.Cci];
	public decimal? Cci => CciValue.ToNullableDecimal(TypedIndicator.Source);

	public IIndicatorValue SmaValue => this[TypedIndicator.Sma];
	public decimal? Sma => SmaValue.ToNullableDecimal(TypedIndicator.Source);

	public override string ToString() => $"Cci={Cci}, Sma={Sma}";
}
