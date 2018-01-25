namespace StockSharp.Algo.Strategies.Analytics
{
	/// <summary>
	/// The interface for work with result panel.
	/// </summary>
	public interface IAnalyticsPanel
	{
		/// <summary>
		/// Delete all controls.
		/// </summary>
		void ClearControls();

		/// <summary>
		/// Create table to show analytics result.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <returns>Table.</returns>
		IAnalyticsGrid CreateGrid(string title);

		/// <summary>
		/// Create bubble chart to show analytics result.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <returns>Bubble chart.</returns>
		IAnalyticsChart CreateBubbleChart(string title);

		/// <summary>
		/// Create histogram chart to show analytics result.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <returns>Histogram chart.</returns>
		IAnalyticsChart CreateHistogramChart(string title);

		/// <summary>
		/// Create heatmap to show analytics result.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <returns>Heatmap.</returns>
		IAnalyticsChart CreateHeatmap(string title);
	}
}