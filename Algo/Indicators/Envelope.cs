namespace StockSharp.Algo.Indicators;

/// <summary>
/// Envelope indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.EnvelopeKey,
	Description = LocalizedStrings.EnvelopeDescKey)]
[Doc("topics/api/indicators/list_of_indicators/envelope.html")]
[IndicatorOut(typeof(EnvelopeValue))]
public class Envelope : BaseComplexIndicator<EnvelopeValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Envelope"/>.
	/// </summary>
	public Envelope()
		: this(new SimpleMovingAverage())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Envelope"/>.
	/// </summary>
	/// <param name="ma">Middle line.</param>
	public Envelope(LengthIndicator<decimal> ma)
	{
		AddInner(Middle = ma);
		AddInner(Upper = ma.TypedClone());
		AddInner(Lower = ma.TypedClone());

		Upper.Name = nameof(Upper);
		Lower.Name = nameof(Lower);
	}

	/// <summary>
	/// Middle line.
	/// </summary>
	[Browsable(false)]
	public LengthIndicator<decimal> Middle { get; }

	/// <summary>
	/// Upper line.
	/// </summary>
	[Browsable(false)]
	public LengthIndicator<decimal> Upper { get; }

	/// <summary>
	/// Lower line.
	/// </summary>
	[Browsable(false)]
	public LengthIndicator<decimal> Lower { get; }

	/// <summary>
	/// Period length. By default equal to 1.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => Middle.Length;
		set
		{
			Middle.Length = Upper.Length = Lower.Length = value;
			Reset();
		}
	}

	private decimal _shift = 0.01m;

	/// <summary>
	/// The shift width. Specified as percentage from 0 to 1. The default equals to 0.01.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ThresholdKey,
		Description = LocalizedStrings.ThresholdDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal Shift
	{
		get => _shift;
		set
		{
			if (value < 0)
				throw new ArgumentNullException(nameof(value));

			_shift = value;
			Reset();
		}
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Middle.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var value = (EnvelopeValue)base.OnProcess(input);

		var upper = value[Upper];
		value[Upper] = upper.SetValue(Upper, value.Upper * (1 + Shift));

		var lower = value[Lower];
		value[Lower] = lower.SetValue(Lower, value.Lower * (1 - Shift));

		return value;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Shift = storage.GetValue<decimal>(nameof(Shift));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Shift), Shift);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;

	/// <inheritdoc />
	protected override EnvelopeValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="Envelope"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EnvelopeValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="Envelope"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class EnvelopeValue(Envelope indicator, DateTimeOffset time) : ComplexIndicatorValue<Envelope>(indicator, time)
{
	/// <summary>
	/// Gets the <see cref="Envelope.Middle"/> value.
	/// </summary>
	public decimal Middle => GetInnerDecimal(TypedIndicator.Middle);

	/// <summary>
	/// Gets the <see cref="Envelope.Upper"/> value.
	/// </summary>
	public decimal Upper => GetInnerDecimal(TypedIndicator.Upper);

	/// <summary>
	/// Gets the <see cref="Envelope.Lower"/> value.
	/// </summary>
	public decimal Lower => GetInnerDecimal(TypedIndicator.Lower);
}
