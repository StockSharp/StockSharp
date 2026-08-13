namespace StockSharp.Charting;

/// <summary>
/// Chart axis supporting a fixed numeric range.
/// </summary>
public interface IChartManualRangeAxis : IChartAxis
{
	/// <summary>
	/// Manual numeric range minimum, or <see langword="null"/> when no manual range is configured.
	/// </summary>
	decimal? MinValue { get; set; }

	/// <summary>
	/// Manual numeric range maximum, or <see langword="null"/> when no manual range is configured.
	/// </summary>
	decimal? MaxValue { get; set; }
}
