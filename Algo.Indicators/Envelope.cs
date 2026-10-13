namespace StockSharp.Algo.Indicators;

/// <summary>
/// Envelope indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.EnvelopeKey,
	Description = LocalizedStrings.EnvelopeDescKey)]
[Doc("topics/api/indicators/list_of_indicators/envelope.html")]
[IndicatorOut(typeof(IEnvelopeValue))]
public class Envelope : BaseComplexIndicator<IEnvelopeValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Envelope"/>.
	/// </summary>
	public Envelope()
		: this(new SimpleMovingAverage(), new SimpleMovingAverage(), new SimpleMovingAverage())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Envelope"/>.
	/// </summary>
	/// <param name="ma">Middle line.</param>
	[Obsolete("Blocking sync-over-async wrapper. Use CreateAsync instead.")]
	public Envelope(DecimalLengthIndicator ma)
		: this(ma, (DecimalLengthIndicator)ma.Clone(), (DecimalLengthIndicator)ma.Clone())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Envelope"/>.
	/// </summary>
	/// <param name="middle">Middle line.</param>
	/// <param name="upper">Upper line.</param>
	/// <param name="lower">Lower line.</param>
	public Envelope(DecimalLengthIndicator middle, DecimalLengthIndicator upper, DecimalLengthIndicator lower)
	{
		AddInner(Middle = middle ?? throw new ArgumentNullException(nameof(middle)));
		AddInner(Upper = upper ?? throw new ArgumentNullException(nameof(upper)));
		AddInner(Lower = lower ?? throw new ArgumentNullException(nameof(lower)));

		Upper.Name = nameof(Upper);
		Lower.Name = nameof(Lower);
	}

	/// <summary>
	/// Create <see cref="Envelope"/> whose upper and lower lines are copies of the middle one.
	/// </summary>
	/// <param name="ma">Middle line.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Envelope"/></returns>
	public static async ValueTask<Envelope> CreateAsync(DecimalLengthIndicator ma, CancellationToken cancellationToken)
	{
		if (ma is null)
			throw new ArgumentNullException(nameof(ma));

		var upper = (DecimalLengthIndicator)await ma.CloneAsync(cancellationToken);
		var lower = (DecimalLengthIndicator)await ma.CloneAsync(cancellationToken);

		return new(ma, upper, lower);
	}

	/// <summary>
	/// Middle line.
	/// </summary>
	[Browsable(false)]
	public DecimalLengthIndicator Middle { get; }

	/// <summary>
	/// Upper line.
	/// </summary>
	[Browsable(false)]
	public DecimalLengthIndicator Upper { get; }

	/// <summary>
	/// Lower line.
	/// </summary>
	[Browsable(false)]
	public DecimalLengthIndicator Lower { get; }

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

		value.SetInnerDecimal(Upper, input.Time, value.Upper * (1 + Shift), input.IsFinal);
		value.SetInnerDecimal(Lower, input.Time, value.Lower * (1 - Shift), input.IsFinal);

		return value;
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.LoadAsync(storage, cancellationToken);
		Shift = storage.GetValue<decimal>(nameof(Shift));
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.SaveAsync(storage, cancellationToken);
		storage.SetValue(nameof(Shift), Shift);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;

	/// <inheritdoc />
	protected override IEnvelopeValue CreateValue(DateTime time)
		=> new EnvelopeValue(this, time);
}

/// <summary>
/// <see cref="Envelope"/> indicator value.
/// </summary>
public interface IEnvelopeValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the <see cref="Envelope.Middle"/> value.
	/// </summary>
	IIndicatorValue MiddleValue { get; }

	/// <summary>
	/// Gets the <see cref="Envelope.Middle"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Middle { get; }

	/// <summary>
	/// Gets the <see cref="Envelope.Upper"/> value.
	/// </summary>
	IIndicatorValue UpperValue { get; }

	/// <summary>
	/// Gets the <see cref="Envelope.Upper"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Upper { get; }

	/// <summary>
	/// Gets the <see cref="Envelope.Lower"/> value.
	/// </summary>
	IIndicatorValue LowerValue { get; }

	/// <summary>
	/// Gets the <see cref="Envelope.Lower"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Lower { get; }
}

/// <summary>
/// Envelope indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EnvelopeValue"/> class.
/// </remarks>
/// <param name="indicator">The parent Envelope indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class EnvelopeValue(Envelope indicator, DateTime time) : ComplexIndicatorValue<Envelope>(indicator, time), IEnvelopeValue
{
	/// <inheritdoc />
	public IIndicatorValue MiddleValue => this[TypedIndicator.Middle];
	/// <inheritdoc />
	public decimal? Middle => MiddleValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue UpperValue => this[TypedIndicator.Upper];
	/// <inheritdoc />
	public decimal? Upper => UpperValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue LowerValue => this[TypedIndicator.Lower];
	/// <inheritdoc />
	public decimal? Lower => LowerValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"Middle={Middle}, Upper={Upper}, Lower={Lower}";
}
