namespace StockSharp.Algo.Indicators;

/// <summary>
/// Sine Wave indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SWKey,
	Description = LocalizedStrings.SineWaveKey)]
[Doc("topics/api/indicators/list_of_indicators/sine_wave.html")]
[IndicatorOut(typeof(SineWaveValue))]
public class SineWave : BaseComplexIndicator<SineWaveValue>
{
	/// <summary>
	/// Lead line.
	/// </summary>
	[Browsable(false)]
	public SineWaveLine Lead { get; } = new();

	/// <summary>
	/// Main line.
	/// </summary>
	[Browsable(false)]
	public SineWaveLine Main { get; } = new();
	private int _currentBar;

	/// <summary>
	/// Initializes a new instance of the <see cref="SineWave"/>.
	/// </summary>
	public SineWave()
	{
		AddInner(Main);
		AddInner(Lead);
		Length = 14;
	}

	private int _length;

	/// <summary>
	/// Period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => _length;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override int NumValuesToInitialize => Length;

	/// <inheritdoc />
	public override void Reset()
	{
		_currentBar = 0;
		base.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new SineWaveValue(this, input.Time);

		var sineValue = (decimal)Math.Sin(2 * Math.PI * _currentBar / Length);
		var leadSineValue = (decimal)Math.Sin(2 * Math.PI * (_currentBar + 0.5) / Length);

		result.Add(Main, Main.Process(input, sineValue));
		result.Add(Lead, Lead.Process(input, leadSineValue));

		if (input.IsFinal)
			_currentBar++;

		return result;
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _currentBar >= Length;

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" L={Length}";

	/// <inheritdoc />
	protected override SineWaveValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="SineWave"/> line.
/// </summary>
[IndicatorHidden]
public class SineWaveLine : BaseIndicator
{
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		return input;
	}
}

/// <summary>
/// <see cref="SineWave"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SineWaveValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="SineWave"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class SineWaveValue(SineWave indicator, DateTimeOffset time) : ComplexIndicatorValue<SineWave>(indicator, time)
{
	/// <summary>
	/// Gets the main line value.
	/// </summary>
	public IIndicatorValue MainValue => this[TypedIndicator.Main];

	/// <summary>
	/// Gets the main line value.
	/// </summary>
	[Browsable(false)]
	public decimal? Main => MainValue.ToNullableDecimal();

	/// <summary>
	/// Gets the lead line value.
	/// </summary>
	public IIndicatorValue LeadValue => this[TypedIndicator.Lead];

	/// <summary>
	/// Gets the lead line value.
	/// </summary>
	[Browsable(false)]
	public decimal? Lead => LeadValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"Main={Main}, Lead={Lead}";
}
