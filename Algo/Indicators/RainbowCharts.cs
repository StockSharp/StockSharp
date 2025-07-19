namespace StockSharp.Algo.Indicators;

/// <summary>
/// Rainbow Charts.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RCKey,
	Description = LocalizedStrings.RainbowChartsKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/rainbow_charts.html")]
[IndicatorOut(typeof(RainbowChartsValue))]
public class RainbowCharts : BaseComplexIndicator<RainbowChartsValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RainbowCharts"/>.
	/// </summary>
	public RainbowCharts()
	{
		Lines = 10;
	}

	private int _lines;

	/// <summary>
	/// Number of SMA lines.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SMAKey,
		Description = LocalizedStrings.SimpleMovingAverageKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Lines
	{
		get => _lines;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_lines = value;

			ClearInner();

			for (var i = 1; i < Lines; i++)
				AddInner(new SimpleMovingAverage { Length = i * 2 });

			Reset();
		}
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		//base.Save(storage);
		SaveValues(storage);

		storage.SetValue(nameof(Lines), Lines);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		//base.Load(storage);
		LoadValues(storage);

		Lines = storage.GetValue<int>(nameof(Lines));
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()} L={Lines}";

	/// <inheritdoc />
	protected override RainbowChartsValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="RainbowCharts"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RainbowChartsValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="RainbowCharts"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class RainbowChartsValue(RainbowCharts indicator, DateTimeOffset time) : ComplexIndicatorValue<RainbowCharts>(indicator, time)
{
	/// <summary>
	/// Gets values of all moving averages.
	/// </summary>
	public IIndicatorValue[] AveragesValues => [.. TypedIndicator.InnerIndicators.Select(ind => this[ind])];

	/// <summary>
	/// Gets values of all moving averages.
	/// </summary>
	[Browsable(false)]
	public decimal?[] Averages => [.. AveragesValues.Select(v => v.ToNullableDecimal())];

	/// <inheritdoc />
	public override string ToString() => $"Averages=[{string.Join(", ", Averages)}]";
}
