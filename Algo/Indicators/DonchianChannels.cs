namespace StockSharp.Algo.Indicators;

/// <summary>
/// Donchian Channels indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.DCKey,
	Description = LocalizedStrings.DonchianChannelsKey)]
[Doc("topics/api/indicators/list_of_indicators/donchian_channels.html")]
[IndicatorOut(typeof(IDonchianChannelsValue))]
public class DonchianChannels : BaseComplexIndicator<IDonchianChannelsValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DonchianChannels"/>.
	/// </summary>
	public DonchianChannels()
	{
		UpperBand = new();
		LowerBand = new();
		Middle = new(UpperBand, LowerBand);

		AddInner(UpperBand);
		AddInner(LowerBand);
		AddInner(Middle);

		Length = 20;
	}

	/// <summary>
	/// Upper band.
	/// </summary>
	[Browsable(false)]
	public Highest UpperBand { get; }

	/// <summary>
	/// Lower band.
	/// </summary>
	[Browsable(false)]
	public Lowest LowerBand { get; }

	/// <summary>
	/// Middle band.
	/// </summary>
	[Browsable(false)]
	public DonchianMiddle Middle { get; }

	/// <summary>
	/// Channel length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => UpperBand.Length;
		set => UpperBand.Length = LowerBand.Length = value;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" {Length}";

	/// <inheritdoc />
	protected override IDonchianChannelsValue CreateValue(DateTimeOffset time)
		=> new DonchianChannelsValue(this, time);
}

/// <summary>
/// Represents the middle band of Donchian Channels.
/// </summary>
[IndicatorHidden]
public class DonchianMiddle : BaseIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DonchianMiddle"/>.
	/// </summary>
	/// <param name="upperBand"><see cref="UpperBand"/></param>
	/// <param name="lowerBand"><see cref="LowerBand"/></param>
	public DonchianMiddle(Highest upperBand, Lowest lowerBand)
	{
		UpperBand = upperBand ?? throw new ArgumentNullException(nameof(upperBand));
		LowerBand = lowerBand ?? throw new ArgumentNullException(nameof(lowerBand));
	}

	/// <summary>
	/// Upper band indicator.
	/// </summary>
	public Highest UpperBand { get; }

	/// <summary>
	/// Lower band indicator.
	/// </summary>
	public Lowest LowerBand { get; }

	/// <inheritdoc />
	protected override bool CalcIsFormed() => UpperBand.IsFormed && LowerBand.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var upperValue = UpperBand.GetCurrentValue();
		var lowerValue = LowerBand.GetCurrentValue();

		return new DecimalIndicatorValue(this, (upperValue + lowerValue) / 2, input.Time);
	}
}

/// <summary>
/// <see cref="DonchianChannels"/> indicator value.
/// </summary>
public interface IDonchianChannelsValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the <see cref="DonchianChannels.UpperBand"/> value.
	/// </summary>
	IIndicatorValue UpperBandValue { get; }

	/// <summary>
	/// Gets the <see cref="DonchianChannels.UpperBand"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? UpperBand { get; }

	/// <summary>
	/// Gets the <see cref="DonchianChannels.LowerBand"/> value.
	/// </summary>
	IIndicatorValue LowerBandValue { get; }

	/// <summary>
	/// Gets the <see cref="DonchianChannels.LowerBand"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? LowerBand { get; }

	/// <summary>
	/// Gets the <see cref="DonchianChannels.Middle"/> value.
	/// </summary>
	IIndicatorValue MiddleValue { get; }

	/// <summary>
	/// Gets the <see cref="DonchianChannels.Middle"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Middle { get; }
}

class DonchianChannelsValue(DonchianChannels indicator, DateTimeOffset time) : ComplexIndicatorValue<DonchianChannels>(indicator, time), IDonchianChannelsValue
{
	public IIndicatorValue UpperBandValue => this[TypedIndicator.UpperBand];
	public decimal? UpperBand => UpperBandValue.ToNullableDecimal(TypedIndicator.Source);

	public IIndicatorValue LowerBandValue => this[TypedIndicator.LowerBand];
	public decimal? LowerBand => LowerBandValue.ToNullableDecimal(TypedIndicator.Source);

	public IIndicatorValue MiddleValue => this[TypedIndicator.Middle];
	public decimal? Middle => MiddleValue.ToNullableDecimal(TypedIndicator.Source);

	public override string ToString() => $"UpperBand={UpperBand}, LowerBand={LowerBand}, Middle={Middle}";
}
