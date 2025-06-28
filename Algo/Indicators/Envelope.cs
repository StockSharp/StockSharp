namespace StockSharp.Algo.Indicators;

/// <summary>
/// Envelope.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/envelope.html
/// </remarks>
[Display(ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.EnvelopeKey)]
[Doc("topics/api/indicators/list_of_indicators/envelope.html")]
[IndicatorOut(typeof(EnvelopeValue))]
public class Envelope : BaseComplexIndicator
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
		var value = (ComplexIndicatorValue)base.OnProcess(input);

		var upper = value[Upper];
		value[Upper] = upper.SetValue(Upper, upper.ToDecimal() * (1 + Shift));

		var lower = value[Lower];
		value[Lower] = lower.SetValue(Lower, lower.ToDecimal() * (1 - Shift));

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
	protected override ComplexIndicatorValue CreateValue(DateTimeOffset time)
		=> new EnvelopeValue(this, time);
}

/// <summary>
/// <see cref="Envelope"/> indicator value.
/// </summary>
public class EnvelopeValue : ComplexIndicatorValue<Envelope>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="EnvelopeValue"/>.
	/// </summary>
	/// <param name="indicator"><see cref="Envelope"/></param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public EnvelopeValue(Envelope indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <summary>
	/// Gets the <see cref="Envelope.Middle"/> value.
	/// </summary>
	public decimal Middle => InnerValues[Indicator.Middle].ToDecimal();

	/// <summary>
	/// Gets the <see cref="Envelope.Upper"/> value.
	/// </summary>
	public decimal Upper => InnerValues[Indicator.Upper].ToDecimal();

	/// <summary>
	/// Gets the <see cref="Envelope.Lower"/> value.
	/// </summary>
	public decimal Lower => InnerValues[Indicator.Lower].ToDecimal();
}
