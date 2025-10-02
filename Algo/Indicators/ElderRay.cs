namespace StockSharp.Algo.Indicators;

/// <summary>
/// Elder Ray Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/elder_ray.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ElderRayKey,
	Description = LocalizedStrings.ElderRayDescKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/elder_ray.html")]
[IndicatorOut(typeof(ElderRayValue))]
public class ElderRay : BaseComplexIndicator<ElderRayValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ElderRay"/>.
	/// </summary>
	public ElderRay()
		: this(new BullPower(), new BearPower())
	{
		Length = 13;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ElderRay"/>.
	/// </summary>
	/// <param name="bullPower">Bull Power.</param>
	/// <param name="bearPower">Bear Power.</param>
	public ElderRay(BullPower bullPower, BearPower bearPower)
		: base(bullPower, bearPower)
	{
		BullPower = bullPower;
		BearPower = bearPower;
	}

	/// <summary>
	/// EMA period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => BullPower.Length;
		set
		{
			BullPower.Length = BearPower.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// Bull Power.
	/// </summary>
	[Browsable(false)]
	public BullPower BullPower { get; }

	/// <summary>
	/// Bear Power.
	/// </summary>
	[Browsable(false)]
	public BearPower BearPower { get; }

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.Set(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" L={Length}";

	/// <inheritdoc />
	protected override ElderRayValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="ElderRay"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ElderRayValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="ElderRay"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class ElderRayValue(ElderRay indicator, DateTimeOffset time) : ComplexIndicatorValue<ElderRay>(indicator, time)
{
	/// <summary>
	/// Gets the <see cref="ElderRay.BullPower"/> value.
	/// </summary>
	public IIndicatorValue BullPowerValue => this[TypedIndicator.BullPower];

	/// <summary>
	/// Gets the <see cref="ElderRay.BullPower"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? BullPower => BullPowerValue.ToNullableDecimal(TypedIndicator.Source);

	/// <summary>
	/// Gets the <see cref="ElderRay.BearPower"/> value.
	/// </summary>
	public IIndicatorValue BearPowerValue => this[TypedIndicator.BearPower];

	/// <summary>
	/// Gets the <see cref="ElderRay.BearPower"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? BearPower => BearPowerValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"Bull={BullPower}, Bear={BearPower}";
}