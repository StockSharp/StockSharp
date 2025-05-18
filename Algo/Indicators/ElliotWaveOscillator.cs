namespace StockSharp.Algo.Indicators;

/// <summary>
/// Elliot Wave Oscillator indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.EWOKey,
	Description = LocalizedStrings.ElliotWaveOscillatorKey)]
[Doc("topics/api/indicators/list_of_indicators/elliot_wave_oscillator.html")]
public class ElliotWaveOscillator : BaseIndicator
{
	private readonly SimpleMovingAverage _shortSma;
	private readonly SimpleMovingAverage _longSma;

	/// <summary>
	/// Initializes a new instance of the <see cref="ElliotWaveOscillator"/>.
	/// </summary>
	public ElliotWaveOscillator()
	{
		_shortSma = new() { Length = 5 };
		_longSma = new() { Length = 34 };
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
		get => _shortSma.Length;
		set
		{
			_shortSma.Length = value;
			Reset();
		}
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
		get => _longSma.Length;
		set
		{
			_longSma.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override int NumValuesToInitialize => _shortSma.NumValuesToInitialize.Max(_longSma.NumValuesToInitialize);

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _shortSma.IsFormed && _longSma.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var shortValue = _shortSma.Process(input);
		var longValue = _longSma.Process(input);

		if (!IsFormed)
			return new DecimalIndicatorValue(this, input.Time);

		var ewo = shortValue.ToDecimal() - longValue.ToDecimal();
		return new DecimalIndicatorValue(this, ewo, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_shortSma.Reset();
		_longSma.Reset();

		base.Reset();
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
	public override string ToString() => base.ToString() + $" S={ShortPeriod} L={LongPeriod}";
}