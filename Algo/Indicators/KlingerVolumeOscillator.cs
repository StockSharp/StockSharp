namespace StockSharp.Algo.Indicators;

/// <summary>
/// Klinger Volume Oscillator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.KVOKey,
	Description = LocalizedStrings.KlingerVolumeOscillatorKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/klinger_volume_oscillator.html")]
[IndicatorOut(typeof(IKlingerVolumeOscillatorValue))]
public class KlingerVolumeOscillator : BaseComplexIndicator<IKlingerVolumeOscillatorValue>
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
	private decimal _prevHlc;

	/// <summary>
	/// Initializes a new instance of the <see cref="KlingerVolumeOscillator"/>.
	/// </summary>
	public KlingerVolumeOscillator()
		: this(new() { Length = 34 }, new() { Length = 55 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="KlingerVolumeOscillator"/>.
	/// </summary>
	/// <param name="shortEma">The short-term EMA.</param>
	/// <param name="longEma">The long-term EMA.</param>
	public KlingerVolumeOscillator(ExponentialMovingAverage shortEma, ExponentialMovingAverage longEma)
		: base(shortEma, longEma)
	{
		ShortEma = shortEma ?? throw new ArgumentNullException(nameof(shortEma));
		LongEma = longEma ?? throw new ArgumentNullException(nameof(longEma));
	}

	/// <summary>
	/// Short period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortKey,
		Description = LocalizedStrings.ShortPeriodKey,
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
		Name = LocalizedStrings.LongKey,
		Description = LocalizedStrings.LongPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int LongPeriod
	{
		get => LongEma.Length;
		set => LongEma.Length = value;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var hlc = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3;
		var sv = candle.TotalVolume * (hlc > _prevHlc ? 1 : -1);

		var result = new KlingerVolumeOscillatorValue(this, input.Time);

		var shortValue = ShortEma.Process(input, sv);
		var longValue = LongEma.Process(input, sv);

		result.Add(ShortEma, shortValue);
		result.Add(LongEma, longValue);

		if (LongEma.IsFormed)
		{
			var kvo = shortValue.ToDecimal(Source) - longValue.ToDecimal(Source);
			result.Add(this, new DecimalIndicatorValue(this, kvo, input.Time) { IsFinal = input.IsFinal });
		}

		if (input.IsFinal)
		{
			_prevHlc = hlc;

			if (!IsFormed && ShortEma.IsFormed && LongEma.IsFormed)
				IsFormed = true;
		}

		return result;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_prevHlc = default;
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" S={ShortPeriod},L={LongPeriod}";

	/// <inheritdoc />
	protected override IKlingerVolumeOscillatorValue CreateValue(DateTimeOffset time)
		=> new KlingerVolumeOscillatorValue(this, time);
}

/// <summary>
/// <see cref="KlingerVolumeOscillator"/> indicator value.
/// </summary>
public interface IKlingerVolumeOscillatorValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the short EMA value.
	/// </summary>
	IIndicatorValue ShortEmaValue { get; }

	/// <summary>
	/// Gets the short EMA value.
	/// </summary>
	[Browsable(false)]
	decimal? ShortEma { get; }

	/// <summary>
	/// Gets the long EMA value.
	/// </summary>
	IIndicatorValue LongEmaValue { get; }

	/// <summary>
	/// Gets the long EMA value.
	/// </summary>
	[Browsable(false)]
	decimal? LongEma { get; }

	/// <summary>
	/// Gets the oscillator value.
	/// </summary>
	IIndicatorValue OscillatorValue { get; }

	/// <summary>
	/// Gets the oscillator value.
	/// </summary>
	[Browsable(false)]
	decimal? Oscillator { get; }
}

class KlingerVolumeOscillatorValue(KlingerVolumeOscillator indicator, DateTimeOffset time) : ComplexIndicatorValue<KlingerVolumeOscillator>(indicator, time), IKlingerVolumeOscillatorValue
{
	public IIndicatorValue ShortEmaValue => this[TypedIndicator.ShortEma];
	public decimal? ShortEma => ShortEmaValue.ToNullableDecimal(TypedIndicator.Source);

	public IIndicatorValue LongEmaValue => this[TypedIndicator.LongEma];
	public decimal? LongEma => LongEmaValue.ToNullableDecimal(TypedIndicator.Source);

	public IIndicatorValue OscillatorValue => this[TypedIndicator];
	public decimal? Oscillator => OscillatorValue.ToNullableDecimal(TypedIndicator.Source);

	public override string ToString() => $"ShortEma={ShortEma}, LongEma={LongEma}, Oscillator={Oscillator}";
}
