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
[IndicatorOut(typeof(DecimalIndicatorValue))]
public class PercentagePriceOscillator : BaseIndicator
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
	{
		ShortEma = shortEma;
		LongEma = longEma;

		AddResetTracking(shortEma);
		AddResetTracking(longEma);
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
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => ShortEma.IsFormed && LongEma.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var shortVal = ShortEma.Process(input);
		var longVal = LongEma.Process(input);

		if (IsFormed)
		{
			var den = longVal.ToDecimal(Source);
			var ppo = den == 0 ? 0 : ((shortVal.ToDecimal(Source) - den) / den) * 100m;
			IsFormed = true;
			return new DecimalIndicatorValue(this, ppo, input.Time) { IsFinal = input.IsFinal };
		}

		return new DecimalIndicatorValue(this, input.Time) { IsFinal = input.IsFinal };
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
}
