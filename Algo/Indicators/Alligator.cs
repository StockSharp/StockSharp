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
[IndicatorOut(typeof(IAlligatorValue))]
public class Alligator : BaseComplexIndicator<IAlligatorValue>
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
	protected override IAlligatorValue CreateValue(DateTime time)
		=> new AlligatorValue(this, time);
}

/// <summary>
/// <see cref="Alligator"/> indicator value implementation.
/// </summary>
public interface IAlligatorValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the <see cref="Alligator.Jaw"/> value.
	/// </summary>
	IIndicatorValue JawValue { get; }

	/// <summary>
	/// Gets the <see cref="Alligator.Jaw"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Jaw { get; }

	/// <summary>
	/// Gets the <see cref="Alligator.Teeth"/> value.
	/// </summary>
	IIndicatorValue TeethValue { get; }

	/// <summary>
	/// Gets the <see cref="Alligator.Teeth"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Teeth { get; }

	/// <summary>
	/// Gets the <see cref="Alligator.Lips"/> value.
	/// </summary>
	IIndicatorValue LipsValue { get; }

	/// <summary>
	/// Gets the <see cref="Alligator.Lips"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Lips { get; }
}

/// <summary>
/// Alligator indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AlligatorValue"/> class.
/// </remarks>
/// <param name="indicator">The parent Alligator indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class AlligatorValue(Alligator indicator, DateTime time) : ComplexIndicatorValue<Alligator>(indicator, time), IAlligatorValue
{
	/// <inheritdoc />
	public IIndicatorValue JawValue => this[TypedIndicator.Jaw];
	/// <inheritdoc />
	public decimal? Jaw => JawValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue TeethValue => this[TypedIndicator.Teeth];
	/// <inheritdoc />
	public decimal? Teeth => TeethValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue LipsValue => this[TypedIndicator.Lips];
	/// <inheritdoc />
	public decimal? Lips => LipsValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"Jaw={Jaw}, Teeth={Teeth}, Lips={Lips}";
}
