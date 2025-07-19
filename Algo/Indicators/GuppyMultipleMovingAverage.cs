namespace StockSharp.Algo.Indicators;

/// <summary>
/// Guppy Multiple Moving Average (GMMA).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.GMMAKey,
	Description = LocalizedStrings.GuppyMultipleMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/guppy_multiple_moving_average.html")]
[IndicatorOut(typeof(GuppyMultipleMovingAverageValue))]
public class GuppyMultipleMovingAverage : BaseComplexIndicator<GuppyMultipleMovingAverageValue>
{
	private static readonly int[] _lengths = new[] { 3, 5, 8, 10, 12, 15 }.Concat([30, 35, 40, 45, 50, 60]);

	/// <summary>
	/// Initializes a new instance of the <see cref="GuppyMultipleMovingAverage"/>.
	/// </summary>
	public GuppyMultipleMovingAverage()
	{
		foreach (var length in _lengths)
			AddInner(new ExponentialMovingAverage { Length = length });
	}

	/// <inheritdoc />
	protected override GuppyMultipleMovingAverageValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="GuppyMultipleMovingAverage"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GuppyMultipleMovingAverageValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="GuppyMultipleMovingAverage"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class GuppyMultipleMovingAverageValue(GuppyMultipleMovingAverage indicator, DateTimeOffset time) : ComplexIndicatorValue<GuppyMultipleMovingAverage>(indicator, time)
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
