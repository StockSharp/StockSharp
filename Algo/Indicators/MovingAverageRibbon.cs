namespace StockSharp.Algo.Indicators;

/// <summary>
/// Moving Average Ribbon indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MARKey,
	Description = LocalizedStrings.MovingAverageRibbonKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/moving_average_ribbon.html")]
public class MovingAverageRibbon : BaseComplexIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageRibbon"/>.
	/// </summary>
	public MovingAverageRibbon()
	{
		ShortPeriod = 10;
		LongPeriod = 100;
		RibbonCount = 10;

		Mode = ComplexIndicatorModes.Sequence;
	}

	private int _shortPeriod;

	/// <summary>
	/// Shortest Moving Average period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortKey,
		Description = LocalizedStrings.ShortPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int ShortPeriod
	{
		get => _shortPeriod;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_shortPeriod = value;
			Reset();
		}
	}

	private int _longPeriod;

	/// <summary>
	/// Longest Moving Average period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LongKey,
		Description = LocalizedStrings.LongPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int LongPeriod
	{
		get => _longPeriod;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_longPeriod = value;
			Reset();
		}
	}

	private int _ribbonCount;

	/// <summary>
	/// Number of Moving Averages in the ribbon.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RibbonKey,
		Description = LocalizedStrings.RibbonCountKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int RibbonCount
	{
		get => _ribbonCount;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_ribbonCount = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override void Reset()
	{
		ClearInner();

		var step = (LongPeriod - ShortPeriod) / (RibbonCount - 1);

		for (var i = 0; i < RibbonCount; i++)
			AddInner(new SimpleMovingAverage { Length = ShortPeriod + i * step });

		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		//base.Save(storage);
		SaveValues(storage);

		storage.SetValue(nameof(ShortPeriod), ShortPeriod);
		storage.SetValue(nameof(LongPeriod), LongPeriod);
		storage.SetValue(nameof(RibbonCount), RibbonCount);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		//base.Load(storage);
		LoadValues(storage);

		ShortPeriod = storage.GetValue<int>(nameof(ShortPeriod));
		LongPeriod = storage.GetValue<int>(nameof(LongPeriod));
		RibbonCount = storage.GetValue<int>(nameof(RibbonCount));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" S={ShortPeriod} L={LongPeriod} C={RibbonCount}";
}