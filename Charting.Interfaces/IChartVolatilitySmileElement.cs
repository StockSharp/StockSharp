namespace StockSharp.Charting;

/// <summary>
/// The chart element representing a volatility smile.
/// </summary>
public interface IChartVolatilitySmileElement : IChartElement
{
	/// <summary>
	/// Points that displays source volatility values.
	/// </summary>
	IChartLineElement Values { get; }

	/// <summary>
	/// Line that displays approximated volatility smile.
	/// </summary>
	IChartLineElement Smile { get; }
}