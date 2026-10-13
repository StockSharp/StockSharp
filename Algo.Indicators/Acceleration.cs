namespace StockSharp.Algo.Indicators;

/// <summary>
/// Acceleration / Deceleration Indicator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/a_d.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ADKey,
	Description = LocalizedStrings.AccDecIndicatorKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/a_d.html")]
public class Acceleration : BaseIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Acceleration"/>.
	/// </summary>
	public Acceleration()
		: this(new AwesomeOscillator(), new SimpleMovingAverage { Length = 5 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Acceleration"/>.
	/// </summary>
	/// <param name="ao">Awesome Oscillator.</param>
	/// <param name="sma">The moving average.</param>
	public Acceleration(AwesomeOscillator ao, SimpleMovingAverage sma)
	{
		Ao = ao ?? throw new ArgumentNullException(nameof(ao));
		Sma = sma ?? throw new ArgumentNullException(nameof(sma));

		AddResetTracking(Ao);
		AddResetTracking(Sma);
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <summary>
	/// The moving average.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MAKey,
		Description = LocalizedStrings.MovingAverageKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SimpleMovingAverage Sma { get; }

	/// <summary>
	/// Awesome Oscillator.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AOKey,
		Description = LocalizedStrings.AwesomeOscillatorKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public AwesomeOscillator Ao { get; }

	/// <inheritdoc />
	public override int NumValuesToInitialize => Ao.NumValuesToInitialize + Sma.NumValuesToInitialize - 1;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Sma.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var aoValue = Ao.Process(input);

		var aoDec = aoValue.ToDecimal(Source);

		if (Ao.IsFormed)
			aoDec -= Sma.Process(aoValue).ToDecimal(Source);

		return new DecimalIndicatorValue(this, aoDec, input.Time);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.LoadAsync(storage, cancellationToken);

		await Sma.LoadIfNotNullAsync(storage, nameof(Sma), cancellationToken);
		await Ao.LoadIfNotNullAsync(storage, nameof(Ao), cancellationToken);
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.SaveAsync(storage, cancellationToken);

		storage.SetValue(nameof(Sma), await Sma.SaveAsync(cancellationToken));
		storage.SetValue(nameof(Ao), await Ao.SaveAsync(cancellationToken));
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()}, Sma={Sma}, Ao={Ao}";
}
