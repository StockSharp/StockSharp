namespace StockSharp.Algo.Indicators;

/// <summary>
/// Percentage Volume Oscillator (PVO).
/// </summary>
[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PVOKey,
		Description = LocalizedStrings.PercentageVolumeOscillatorKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/percentage_volume_oscillator.html")]
[IndicatorOut(typeof(PercentageVolumeOscillatorValue))]
public class PercentageVolumeOscillator : BaseComplexIndicator<PercentageVolumeOscillatorValue>
{
	/// <summary>
	/// Short EMA.
	/// </summary>
	[Browsable(false)]
	public ExponentialMovingAverage ShortEma { get; }

	/// <summary>
	/// Long EMA.
	/// </summary>
	[Browsable(false)]
	public ExponentialMovingAverage LongEma { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="PercentageVolumeOscillator"/>.
	/// </summary>
	public PercentageVolumeOscillator()
		: this(new(), new())
	{
		ShortPeriod = 12;
		LongPeriod = 26;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PercentageVolumeOscillator"/>.
	/// </summary>
	/// <param name="shortEma">The short-term EMA.</param>
	/// <param name="longEma">The long-term EMA.</param>
	public PercentageVolumeOscillator(ExponentialMovingAverage shortEma, ExponentialMovingAverage longEma)
		: base(shortEma, longEma)
	{
		ShortEma = shortEma;
		LongEma = longEma;
	}

	/// <summary>
	/// Short period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortPeriodKey,
		Description = LocalizedStrings.ShortMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int ShortPeriod
	{
		get => ShortEma.Length;
		set => ShortEma.Length = value;
	}

	/// <summary>
	/// Long period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LongPeriodKey,
		Description = LocalizedStrings.LongMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int LongPeriod
	{
		get => LongEma.Length;
		set => LongEma.Length = value;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Volume;

	/// <inheritdoc />
	public override int NumValuesToInitialize => ShortEma.NumValuesToInitialize.Max(LongEma.NumValuesToInitialize);

	/// <inheritdoc />
	protected override bool CalcIsFormed() => ShortEma.IsFormed && LongEma.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var volume = input.ToCandle().TotalVolume;

		var result = new PercentageVolumeOscillatorValue(this, input.Time);

		var shortValue = ShortEma.Process(input, volume);
		var longValue = LongEma.Process(input, volume);

		result.Add(ShortEma, shortValue);
		result.Add(LongEma, longValue);

		if (LongEma.IsFormed)
		{
			var den = longValue.ToDecimal();
			var pvo = den == 0 ? 0 : ((shortValue.ToDecimal() - den) / den) * 100;
			result.Add(this, new DecimalIndicatorValue(this, pvo, input.Time) { IsFinal = input.IsFinal });
		}

		return result;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(ShortPeriod), ShortPeriod);
		storage.SetValue(nameof(LongPeriod), LongPeriod);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		ShortPeriod = storage.GetValue<int>(nameof(ShortPeriod));
		LongPeriod = storage.GetValue<int>(nameof(LongPeriod));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" S={ShortPeriod},L={LongPeriod}";

	/// <inheritdoc />
	protected override PercentageVolumeOscillatorValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="PercentageVolumeOscillator"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PercentageVolumeOscillatorValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="PercentageVolumeOscillator"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class PercentageVolumeOscillatorValue(PercentageVolumeOscillator indicator, DateTimeOffset time) : ComplexIndicatorValue<PercentageVolumeOscillator>(indicator, time)
{
	/// <summary>
	/// Gets the short EMA value.
	/// </summary>
	public IIndicatorValue ShortEmaValue => this[TypedIndicator.ShortEma];

	/// <summary>
	/// Gets the short EMA value.
	/// </summary>
	[Browsable(false)]
	public decimal? ShortEma => ShortEmaValue.ToNullableDecimal();

	/// <summary>
	/// Gets the long EMA value.
	/// </summary>
	public IIndicatorValue LongEmaValue => this[TypedIndicator.LongEma];

	/// <summary>
	/// Gets the long EMA value.
	/// </summary>
	[Browsable(false)]
	public decimal? LongEma => LongEmaValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"ShortEma={ShortEma}, LongEma={LongEma}";
}
