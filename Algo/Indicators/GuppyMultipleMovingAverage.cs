namespace StockSharp.Algo.Indicators;

/// <summary>
/// Guppy Multiple Moving Average (GMMA).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.GMMAKey,
	Description = LocalizedStrings.GuppyMultipleMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/guppy_multiple_moving_average.html")]
[IndicatorOut(typeof(IGuppyMultipleMovingAverageValue))]
public class GuppyMultipleMovingAverage : BaseComplexIndicator<IGuppyMultipleMovingAverageValue>
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
	protected override IGuppyMultipleMovingAverageValue CreateValue(DateTimeOffset time)
		=> new GuppyMultipleMovingAverageValue(this, time);
}

/// <summary>
/// <see cref="GuppyMultipleMovingAverage"/> indicator value.
/// </summary>
public interface IGuppyMultipleMovingAverageValue : IComplexIndicatorValue
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

class GuppyMultipleMovingAverageValue(GuppyMultipleMovingAverage indicator, DateTimeOffset time) : ComplexIndicatorValue<GuppyMultipleMovingAverage>(indicator, time), IGuppyMultipleMovingAverageValue
{
	public IIndicatorValue[] AveragesValues => [.. TypedIndicator.InnerIndicators.Select(ind => this[ind])];
	public decimal?[] Averages => [.. AveragesValues.Select(v => v.ToNullableDecimal(TypedIndicator.Source))];

	public override string ToString() => $"Averages=[{string.Join(", ", Averages)}]";
}
