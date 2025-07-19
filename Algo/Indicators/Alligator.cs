namespace StockSharp.Algo.Indicators;

/// <summary>
/// Alligator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/alligator.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.AlligatorKey,
	Description = LocalizedStrings.AlligatorKey)]
[Doc("topics/api/indicators/list_of_indicators/alligator.html")]
[IndicatorOut(typeof(AlligatorValue))]
public class Alligator : BaseComplexIndicator<AlligatorValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Alligator"/>.
	/// </summary>
	public Alligator()
		: this(new() { Name = nameof(Jaw), Length = 13, Shift = 8 }, new() { Name = nameof(Teeth), Length = 8, Shift = 5 }, new() { Name = nameof(Lips), Length = 5, Shift = 3 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Alligator"/>.
	/// </summary>
	/// <param name="jaw">Jaw.</param>
	/// <param name="teeth">Teeth.</param>
	/// <param name="lips">Lips.</param>
	public Alligator(AlligatorLine jaw, AlligatorLine teeth, AlligatorLine lips)
		: base(jaw, teeth, lips)
	{
		Jaw = jaw;
		Teeth = teeth;
		Lips = lips;
	}

	/// <summary>
	/// Jaw.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.JawKey,
		Description = LocalizedStrings.JawKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public AlligatorLine Jaw { get; }

	/// <summary>
	/// Teeth.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TeethKey,
		Description = LocalizedStrings.TeethKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public AlligatorLine Teeth { get; }

	/// <summary>
	/// Lips.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LipsKey,
		Description = LocalizedStrings.LipsKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public AlligatorLine Lips { get; }

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Jaw.IsFormed;

	/// <inheritdoc />
	public override int NumValuesToInitialize => Jaw.NumValuesToInitialize;

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" J={Jaw.Length} T={Teeth.Length} L={Lips.Length}";
	/// <inheritdoc />
	protected override AlligatorValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="Alligator"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AlligatorValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="Alligator"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class AlligatorValue(Alligator indicator, DateTimeOffset time) : ComplexIndicatorValue<Alligator>(indicator, time)
{
	/// <summary>
	/// Gets the <see cref="Alligator.Jaw"/> value.
	/// </summary>
	public IIndicatorValue JawValue => this[TypedIndicator.Jaw];

	/// <summary>
	/// Gets the <see cref="Alligator.Jaw"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Jaw => JawValue.ToNullableDecimal();

	/// <summary>
	/// Gets the <see cref="Alligator.Teeth"/> value.
	/// </summary>
	public IIndicatorValue TeethValue => this[TypedIndicator.Teeth];

	/// <summary>
	/// Gets the <see cref="Alligator.Teeth"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Teeth => TeethValue.ToNullableDecimal();

	/// <summary>
	/// Gets the <see cref="Alligator.Lips"/> value.
	/// </summary>
	public IIndicatorValue LipsValue => this[TypedIndicator.Lips];

	/// <summary>
	/// Gets the <see cref="Alligator.Lips"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Lips => LipsValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"Jaw={Jaw}, Teeth={Teeth}, Lips={Lips}";
}
