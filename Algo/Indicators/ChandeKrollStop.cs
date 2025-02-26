namespace StockSharp.Algo.Indicators;

/// <summary>
/// Chande Kroll Stop indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CKSKey,
	Description = LocalizedStrings.ChandeKrollStopKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/chande_kroll_stop.html")]
public class ChandeKrollStop : BaseComplexIndicator
{
	private readonly Highest _highest;
	private readonly Lowest _lowest;
	private readonly SimpleMovingAverage _smaHigh;
	private readonly SimpleMovingAverage _smaLow;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChandeKrollStop"/>.
	/// </summary>
	public ChandeKrollStop()
	{
		_highest = new();
		_lowest = new();
		_smaHigh = new();
		_smaLow = new();

		AddInner(_highest);
		AddInner(_lowest);

		Period = 10;
		Multiplier = 1.5m;
		StopPeriod = 9;
	}

	/// <summary>
	/// Period for Highest and Lowest.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.PeriodDescriptionKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Period
	{
		get => _highest.Length;
		set
		{
			_highest.Length = value;
			_lowest.Length = value;
		}
	}

	private decimal _multiplier;

	/// <summary>
	/// Multiplier.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MultiplierKey,
		Description = LocalizedStrings.MultiplierKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal Multiplier
	{
		get => _multiplier;
		set
		{
			_multiplier = value;
			Reset();
		}
	}

	/// <summary>
	/// Stop Period for SMA.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StopKey,
		Description = LocalizedStrings.StopPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int StopPeriod
	{
		get => _smaHigh.Length;
		set
		{
			_smaHigh.Length = value;
			_smaLow.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var highestValue = _highest.Process(input, candle.HighPrice);
		var lowestValue = _lowest.Process(input, candle.LowPrice);

		var result = new ComplexIndicatorValue(this, input.Time);

		if (_highest.IsFormed && _lowest.IsFormed)
		{
			IsFormed = true;

			var highest = highestValue.ToDecimal();
			var lowest = lowestValue.ToDecimal();

			var highLowDiff = highest - lowest;

			var stopLong = highest - highLowDiff * Multiplier;
			var stopShort = lowest + highLowDiff * Multiplier;

			result.Add(_highest, _smaHigh.Process(input, stopLong));
			result.Add(_lowest, _smaLow.Process(input, stopShort));
		}

		return result;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		//_highest.Reset();
		//_lowest.Reset();
		_smaHigh.Reset();
		_smaLow.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Period), Period);
		storage.SetValue(nameof(Multiplier), Multiplier);
		storage.SetValue(nameof(StopPeriod), StopPeriod);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Period = storage.GetValue<int>(nameof(Period));
		Multiplier = storage.GetValue<decimal>(nameof(Multiplier));
		StopPeriod = storage.GetValue<int>(nameof(StopPeriod));
	}
}
