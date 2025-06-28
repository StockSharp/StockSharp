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
public class GuppyMultipleMovingAverage : BaseComplexIndicator
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
	protected override ComplexIndicatorValue CreateValue(DateTimeOffset time)
		=> new GuppyMultipleMovingAverageValue(this, time);
}

/// <summary>
/// <see cref="GuppyMultipleMovingAverage"/> indicator value.
/// </summary>
public class GuppyMultipleMovingAverageValue : ComplexIndicatorValue<GuppyMultipleMovingAverage>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GuppyMultipleMovingAverageValue"/>.
	/// </summary>
	/// <param name="indicator"><see cref="GuppyMultipleMovingAverage"/></param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public GuppyMultipleMovingAverageValue(GuppyMultipleMovingAverage indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <summary>
	/// Gets values of all moving averages.
	/// </summary>
	public decimal[] Averages => Indicator.InnerIndicators.Select(i => InnerValues[i].ToDecimal()).ToArray();
}
