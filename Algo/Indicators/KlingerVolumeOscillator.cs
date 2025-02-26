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
public class KlingerVolumeOscillator : BaseComplexIndicator
{
	private readonly ExponentialMovingAverage _shortEma;
	private readonly ExponentialMovingAverage _longEma;
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
		_shortEma = shortEma ?? throw new ArgumentNullException(nameof(shortEma));
		_longEma = longEma ?? throw new ArgumentNullException(nameof(longEma));
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
		get => _shortEma.Length;
		set => _shortEma.Length = value;
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
		get => _longEma.Length;
		set => _longEma.Length = value;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var hlc = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3;
		var sv = candle.TotalVolume * (hlc > _prevHlc ? 1 : -1);

		var result = new ComplexIndicatorValue(this, input.Time);

		var shortValue = _shortEma.Process(input, sv);
		var longValue = _longEma.Process(input, sv);

		result.Add(_shortEma, shortValue);
		result.Add(_longEma, longValue);

		if (_longEma.IsFormed)
		{
			var kvo = shortValue.ToDecimal() - longValue.ToDecimal();
			result.Add(this, new DecimalIndicatorValue(this, kvo, input.Time));
		}

		if (input.IsFinal)
		{
			_prevHlc = hlc;
			IsFormed = _shortEma.IsFormed && _longEma.IsFormed;
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
}