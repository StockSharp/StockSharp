namespace StockSharp.Algo.Indicators;

/// <summary>
/// Guppy Multiple Moving Average (GMMA).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.GMMAKey,
	Description = LocalizedStrings.GuppyMultipleMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/guppy_multiple_moving_average.html")]
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
}
