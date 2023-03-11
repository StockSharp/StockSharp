namespace StockSharp.Algo.Analytics;

/// <summary>
/// The interface for work with result panel.
/// </summary>
public interface IAnalyticsPanel
{
	/// <summary>
	/// Create table to show analytics result.
	/// </summary>
	/// <param name="columns">Columns.</param>
	/// <returns>Table.</returns>
	IAnalyticsGrid CreateGrid(params string[] columns);

	/// <summary>
	/// Create bubble chart to show analytics result.
	/// </summary>
	/// <returns>Bubble chart.</returns>
	IAnalyticsChart CreateBubbleChart();

	/// <summary>
	/// Create histogram chart to show analytics result.
	/// </summary>
	/// <returns>Histogram chart.</returns>
	IAnalyticsChart CreateHistogramChart();

	/// <summary>
	/// Create heatmap to show analytics result.
	/// </summary>
	/// <returns>Heatmap.</returns>
	IAnalyticsChart CreateHeatmap();
}