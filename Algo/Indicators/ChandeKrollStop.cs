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
[IndicatorOut(typeof(ChandeKrollStopValue))]
public class ChandeKrollStop : BaseComplexIndicator<ChandeKrollStopValue>
{
	private readonly SimpleMovingAverage _smaHigh;
	private readonly SimpleMovingAverage _smaLow;

	/// <summary>
	/// Highest line.
	/// </summary>
	[Browsable(false)]
	public Highest Highest { get; }

	/// <summary>
	/// Lowest line.
	/// </summary>
	[Browsable(false)]
	public Lowest Lowest { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChandeKrollStop"/>.
	/// </summary>
	public ChandeKrollStop()
	{
		Highest = new();
		Lowest = new();
		_smaHigh = new();
		_smaLow = new();

		AddInner(Highest);
		AddInner(Lowest);

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
		get => Highest.Length;
		set
		{
			Highest.Length = value;
			Lowest.Length = value;
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

		var highestValue = Highest.Process(input, candle.HighPrice);
		var lowestValue = Lowest.Process(input, candle.LowPrice);

		var result = new ChandeKrollStopValue(this, input.Time);

		if (Highest.IsFormed && Lowest.IsFormed)
		{
			if (input.IsFinal)
				IsFormed = true;

			var highest = highestValue.ToDecimal();
			var lowest = lowestValue.ToDecimal();

			var highLowDiff = highest - lowest;

			var stopLong = highest - highLowDiff * Multiplier;
			var stopShort = lowest + highLowDiff * Multiplier;

			result.Add(Highest, _smaHigh.Process(input, stopLong));
			result.Add(Lowest, _smaLow.Process(input, stopShort));
		}

		return result;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		//Highest.Reset();
		//Lowest.Reset();
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

	/// <inheritdoc />
	protected override ChandeKrollStopValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="ChandeKrollStop"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChandeKrollStopValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="ChandeKrollStop"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class ChandeKrollStopValue(ChandeKrollStop indicator, DateTimeOffset time) : ComplexIndicatorValue<ChandeKrollStop>(indicator, time)
{
	/// <summary>
	/// Gets the highest stop line.
	/// </summary>
	public IIndicatorValue HighestValue => this[TypedIndicator.Highest];

	/// <summary>
	/// Gets the highest stop line.
	/// </summary>
	[Browsable(false)]
	public decimal? Highest => HighestValue.ToNullableDecimal();

	/// <summary>
	/// Gets the lowest stop line.
	/// </summary>
	public IIndicatorValue LowestValue => this[TypedIndicator.Lowest];

	/// <summary>
	/// Gets the lowest stop line.
	/// </summary>
	[Browsable(false)]
	public decimal? Lowest => LowestValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"Highest={Highest}, Lowest={Lowest}";
}
