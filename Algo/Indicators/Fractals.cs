namespace StockSharp.Algo.Indicators;

/// <summary>
/// <see cref="Fractals"/> indicator value.
/// </summary>
public interface IFractalsValue : IComplexIndicatorValue
{
	/// <summary>
	/// Has pattern.
	/// </summary>
	[Browsable(false)]
	bool HasPattern { get; }

	/// <summary>
	/// Has up.
	/// </summary>
	[Browsable(false)]
	bool HasUp { get; }

	/// <summary>
	/// Has down.
	/// </summary>
	[Browsable(false)]
	bool HasDown { get; }

	/// <summary>
	/// Gets the <see cref="Fractals.Up"/> value.
	/// </summary>
	IIndicatorValue UpValue { get; }

	/// <summary>
	/// Gets the <see cref="Fractals.Down"/> value.
	/// </summary>
	IIndicatorValue DownValue { get; }
}

/// <summary>
/// Fractals indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FractalsValue"/> class.
/// </remarks>
/// <param name="fractals">The parent Fractals indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class FractalsValue(Fractals fractals, DateTimeOffset time) : ComplexIndicatorValue<Fractals>(fractals, time), IFractalsValue
{
	/// <inheritdoc />
	public bool HasPattern => HasUp || HasDown;
	/// <inheritdoc />
	public bool HasUp => ((FractalPartIndicatorValue)UpValue).HasPattern;
	/// <inheritdoc />
	public bool HasDown => ((FractalPartIndicatorValue)DownValue).HasPattern;

	/// <inheritdoc />
	public IIndicatorValue UpValue => this[TypedIndicator.Up];
	/// <inheritdoc />
	public IIndicatorValue DownValue => this[TypedIndicator.Down];

	/// <summary>
	/// Cast object from <see cref="FractalsValue"/> to <see cref="bool"/>.
	/// </summary>
	/// <param name="value">Object <see cref="FractalsValue"/>.</param>
	/// <returns><see cref="bool"/> value.</returns>
	public static explicit operator bool(FractalsValue value)
		=> value.CheckOnNull(nameof(value)).HasPattern;

	/// <inheritdoc />
	public override string ToString() => $"UP={HasUp}, DOWN={HasDown}";
}

/// <summary>
/// Fractals.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/fractals.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FractalsKey,
	Description = LocalizedStrings.FractalsKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[IndicatorOut(typeof(IFractalsValue))]
[Doc("topics/api/indicators/list_of_indicators/fractals.html")]
public class Fractals : BaseComplexIndicator<IFractalsValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Fractals"/>.
	/// </summary>
	public Fractals()
		: this(5, new(true) { Name = nameof(Up) }, new(false) { Name = nameof(Down) })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Fractals"/>.
	/// </summary>
	/// <param name="length">Period length.</param>
	/// <param name="up">Fractal up.</param>
	/// <param name="down">Fractal down.</param>
	public Fractals(int length, FractalPart up, FractalPart down)
		: base(up, down)
	{
		Up = up;
		Down = down;
		Length = length;
	}

	/// <summary>
	/// Period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.PeriodLengthKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => Up.Length;
		set
		{
			Up.Length = Down.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// Fractal up.
	/// </summary>
	//[TypeConverter(typeof(ExpandableObjectConverter))]
	[Browsable(false)]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UpKey,
		Description = LocalizedStrings.FractalUpKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public FractalPart Up { get; }

	/// <summary>
	/// Fractal down.
	/// </summary>
	//[TypeConverter(typeof(ExpandableObjectConverter))]
	[Browsable(false)]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DownKey,
		Description = LocalizedStrings.FractalDownKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public FractalPart Down { get; }

	/// <inheritdoc />
	protected override IFractalsValue CreateValue(DateTimeOffset time)
		=> new FractalsValue(this, time);

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;
}