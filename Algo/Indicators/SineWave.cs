namespace StockSharp.Algo.Indicators;

/// <summary>
/// Sine Wave indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SWKey,
	Description = LocalizedStrings.SineWaveKey)]
[Doc("topics/api/indicators/list_of_indicators/sine_wave.html")]
public class SineWave : BaseComplexIndicator
{
	private readonly SineWaveLine _lead = new();
	private readonly SineWaveLine _main = new();
	private int _currentBar;

	/// <summary>
	/// Initializes a new instance of the <see cref="SineWave"/>.
	/// </summary>
	public SineWave()
	{
		AddInner(_main);
		AddInner(_lead);
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
			_length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override void Reset()
	{
		_currentBar = 0;
		base.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new ComplexIndicatorValue(this, input.Time);

		var sineValue = (decimal)Math.Sin(2 * Math.PI * _currentBar / Length);
		var leadSineValue = (decimal)Math.Sin(2 * Math.PI * (_currentBar + 0.5) / Length);

		result.Add(_main, _main.Process(input, sineValue));
		result.Add(_lead, _lead.Process(input, leadSineValue));

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