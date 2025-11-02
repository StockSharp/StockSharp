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
[IndicatorOut(typeof(IRainbowChartsValue))]
public class RainbowCharts : BaseComplexIndicator<IRainbowChartsValue>
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
	protected override IRainbowChartsValue CreateValue(DateTime time)
		=> new RainbowChartsValue(this, time);
}

/// <summary>
/// <see cref="RainbowCharts"/> indicator value.
/// </summary>
public interface IRainbowChartsValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets values of all moving averages.
	/// </summary>
	IIndicatorValue[] AveragesValues { get; }

	/// <summary>
	/// Gets values of all moving averages.
	/// </summary>
	[Browsable(false)]
	decimal?[] Averages { get; }
}

/// <summary>
/// Rainbow Charts indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RainbowChartsValue"/> class.
/// </remarks>
/// <param name="indicator">The parent Rainbow Charts indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class RainbowChartsValue(RainbowCharts indicator, DateTime time) : ComplexIndicatorValue<RainbowCharts>(indicator, time), IRainbowChartsValue
{
	/// <inheritdoc />
	public IIndicatorValue[] AveragesValues => [.. TypedIndicator.InnerIndicators.Select(ind => this[ind])];
	/// <inheritdoc />
	public decimal?[] Averages => [.. AveragesValues.Select(v => v.ToNullableDecimal(TypedIndicator.Source))];

	/// <inheritdoc />
	public override string ToString() => $"Averages=[{string.Join(", ", Averages)}]";
}
