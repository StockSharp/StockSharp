namespace StockSharp.Algo.Indicators;

/// <summary>
/// Oscillator of Moving Average indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OMAKey,
	Description = LocalizedStrings.OscillatorOfMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/oscillator_of_moving_average.html")]
public class OscillatorOfMovingAverage : BaseIndicator
{
	private readonly SimpleMovingAverage _shortMa;
	private readonly SimpleMovingAverage _longMa;

	/// <summary>
	/// Initializes a new instance of the <see cref="OscillatorOfMovingAverage"/>.
	/// </summary>
	public OscillatorOfMovingAverage()
	{
		_shortMa = new() { Length = 10 };
		_longMa = new() { Length = 30 };
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

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
		get => _shortMa.Length;
		set
		{
			_shortMa.Length = value;
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
		get => _longMa.Length;
		set
		{
			_longMa.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _shortMa.IsFormed && _longMa.IsFormed;

	/// <inheritdoc />
	public override int NumValuesToInitialize => _shortMa.NumValuesToInitialize.Max(_longMa.NumValuesToInitialize);

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var shortValue = _shortMa.Process(input);
		var longValue = _longMa.Process(input);

		if (_shortMa.IsFormed && _longMa.IsFormed)
		{
			var shortMa = shortValue.ToDecimal();
			var longMa = longValue.ToDecimal();
			var result = longMa != 0 ? (shortMa - longMa) / longMa * 100 : 0;
			return new DecimalIndicatorValue(this, result, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_shortMa.Reset();
		_longMa.Reset();
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
	public override string ToString() => $"{base.ToString()} S={ShortPeriod},L={LongPeriod}";
}