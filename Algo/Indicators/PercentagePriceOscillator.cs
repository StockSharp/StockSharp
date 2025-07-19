namespace StockSharp.Algo.Indicators;

/// <summary>
/// Percentage Price Oscillator (PPO).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PPOKey,
	Description = LocalizedStrings.PercentagePriceOscillatorKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/percentage_price_oscillator.html")]
[IndicatorOut(typeof(PercentagePriceOscillatorValue))]
public class PercentagePriceOscillator : BaseComplexIndicator<PercentagePriceOscillatorValue>
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
	/// Initializes a new instance of the <see cref="PercentagePriceOscillator"/>.
	/// </summary>
	public PercentagePriceOscillator()
		: this(new(), new())
	{
		ShortPeriod = 12;
		LongPeriod = 26;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PercentagePriceOscillator"/>.
	/// </summary>
	/// <param name="shortEma">The short-term EMA.</param>
	/// <param name="longEma">The long-term EMA.</param>
	public PercentagePriceOscillator(ExponentialMovingAverage shortEma, ExponentialMovingAverage longEma)
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
	public override int NumValuesToInitialize => ShortEma.NumValuesToInitialize.Max(LongEma.NumValuesToInitialize);

	/// <inheritdoc />
	protected override bool CalcIsFormed() => ShortEma.IsFormed && LongEma.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new PercentagePriceOscillatorValue(this, input.Time);

		var shortValue = ShortEma.Process(input);
		var longValue = LongEma.Process(input);

		result.Add(ShortEma, shortValue);
		result.Add(LongEma, longValue);

		if (IsFormed)
		{
			var den = longValue.ToDecimal();
			var ppo = den == 0 ? 0 : ((shortValue.ToDecimal() - den) / den) * 100;
			result.Add(this, new DecimalIndicatorValue(this, ppo, input.Time) { IsFinal = input.IsFinal });
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
	protected override PercentagePriceOscillatorValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="PercentagePriceOscillator"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PercentagePriceOscillatorValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="PercentagePriceOscillator"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class PercentagePriceOscillatorValue(PercentagePriceOscillator indicator, DateTimeOffset time) : ComplexIndicatorValue<PercentagePriceOscillator>(indicator, time)
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
