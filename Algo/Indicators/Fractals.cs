namespace StockSharp.Algo.Indicators;

/// <summary>
/// <see cref="Fractals"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FractalsValue"/>.
/// </remarks>
/// <param name="fractals"><see cref="Fractals"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class FractalsValue(Fractals fractals, DateTimeOffset time) : ComplexIndicatorValue<Fractals>(fractals, time)
{
	/// <summary>
	/// Has pattern.
	/// </summary>
	[Browsable(false)]
	public bool HasPattern { get; private set; }

	/// <summary>
	/// Has up.
	/// </summary>
	[Browsable(false)]
	public bool HasUp { get; private set; }

	/// <summary>
	/// Has down.
	/// </summary>
	[Browsable(false)]
	public bool HasDown { get; private set; }

	/// <summary>
	/// Gets the <see cref="Fractals.Up"/> value.
	/// </summary>
	public IIndicatorValue UpValue => this[TypedIndicator.Up];

	/// <summary>
	/// Gets the <see cref="Fractals.Up"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Up => UpValue.ToNullableDecimal();

	/// <summary>
	/// Gets the <see cref="Fractals.Down"/> value.
	/// </summary>
	public IIndicatorValue DownValue => this[TypedIndicator.Down];

	/// <summary>
	/// Gets the <see cref="Fractals.Down"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Down => DownValue.ToNullableDecimal();

	/// <summary>
	/// Cast object from <see cref="FractalsValue"/> to <see cref="bool"/>.
	/// </summary>
	/// <param name="value">Object <see cref="FractalsValue"/>.</param>
	/// <returns><see cref="bool"/> value.</returns>
	public static explicit operator bool(FractalsValue value)
		=> value.CheckOnNull(nameof(value)).HasPattern;

	/// <inheritdoc />
	public override void Add(IIndicator indicator, IIndicatorValue value)
	{
		if (indicator is null)	throw new ArgumentNullException(nameof(indicator));
		if (value is null)		throw new ArgumentNullException(nameof(value));

		if (!value.IsEmpty)
		{
			HasPattern = true;

			if (((FractalPart)indicator).IsUp)
				HasUp = true;
			else 
				HasDown = true;
		}

	        base.Add(indicator, value);
	}

	/// <inheritdoc />
	public override string ToString() => $"HasPattern={HasPattern}, HasUp={HasUp}, HasDown={HasDown}, Up={Up}, Down={Down}";
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
[IndicatorOut(typeof(FractalsValue))]
[Doc("topics/api/indicators/list_of_indicators/fractals.html")]
public class Fractals : BaseComplexIndicator<FractalsValue>
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
	protected override FractalsValue CreateValue(DateTimeOffset time)
		=> new(this, time);

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;
}