namespace StockSharp.Algo.Indicators;

/// <summary>
/// Elder Impulse System indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.EISKey,
	Description = LocalizedStrings.ElderImpulseSystemKey)]
[Doc("topics/api/indicators/list_of_indicators/elder_impulse_system.html")]
public class ElderImpulseSystem : BaseIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ElderImpulseSystem"/>.
	/// </summary>
	public ElderImpulseSystem()
		: this(new() { Length = 13 }, new())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ElderImpulseSystem"/>.
	/// </summary>
	/// <param name="ema"><see cref="ExponentialMovingAverage"/></param>
	/// <param name="macd"><see cref="MovingAverageConvergenceDivergence"/></param>
	public ElderImpulseSystem(ExponentialMovingAverage ema, MovingAverageConvergenceDivergence macd)
	{
		Ema = ema ?? throw new ArgumentNullException(nameof(ema));
		Macd = macd ?? throw new ArgumentNullException(nameof(macd));

		AddResetTracking(Ema);
		AddResetTracking(Macd);
	}

	/// <summary>
	/// <see cref="ExponentialMovingAverage"/>
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.EMAKey,
		Description = LocalizedStrings.ExponentialMovingAverageKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public ExponentialMovingAverage Ema { get; }

	/// <summary>
	/// <see cref="MovingAverageConvergenceDivergence"/>
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MACDKey,
		Description = LocalizedStrings.MACDDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public MovingAverageConvergenceDivergence Macd { get; }

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override int NumValuesToInitialize
		=> Ema.NumValuesToInitialize.Max(Macd.NumValuesToInitialize);

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var prevEma = Ema.GetCurrentValue();
		var prevMacd = Macd.GetCurrentValue();

		var emaValue = Ema.Process(input);
		var macdValue = Macd.Process(input);

		if (Ema.IsFormed && Macd.IsFormed)
		{
			if (input.IsFinal)
				IsFormed = true;

			var ema = emaValue.ToDecimal();
			var macd = macdValue.ToDecimal();

			int impulse;

			if (ema > prevEma && macd > prevMacd)
				impulse = 1;  // Green
			else if (ema < prevEma && macd < prevMacd)
				impulse = -1; // Red
			else
				impulse = 0;  // Blue

			return new DecimalIndicatorValue(this, impulse, input.Time)
			{
				IsFinal = input.IsFinal,
				IsFormed = true
			};
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
			.Set(nameof(Ema), Ema.Save())
			.Set(nameof(Macd), Macd.Save())
		;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Ema.Load(storage, nameof(Ema));
		Macd.Load(storage, nameof(Macd));
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()}, EMA={Ema}, MACD={Macd}";
}