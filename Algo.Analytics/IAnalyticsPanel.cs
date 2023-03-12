namespace StockSharp.Algo.Analytics;

using System.Collections.Generic;

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
	/// <typeparam name="X">Type of <paramref name="xValues"/> values.</typeparam>
	/// <typeparam name="Y">Type of <paramref name="yValues"/> values.</typeparam>
	/// <typeparam name="Z">Type of <paramref name="zValues"/> values.</typeparam>
	/// <param name="xValues">X values.</param>
	/// <param name="yValues">Y values.</param>
	/// <param name="zValues">Z values.</param>
	/// <returns><see cref="IAnalyticsChart"/></returns>
	IAnalyticsChart CreateBubbleChart<X, Y, Z>(IEnumerable<X> xValues, IEnumerable<Y> yValues, IEnumerable<Z> zValues);

	/// <summary>
	/// Create histogram chart to show analytics result.
	/// </summary>
	/// <typeparam name="X">Type of <paramref name="xValues"/> values.</typeparam>
	/// <typeparam name="Y">Type of <paramref name="yValues"/> values.</typeparam>
	/// <param name="xValues">X values.</param>
	/// <param name="yValues">Y values.</param>
	/// <returns><see cref="IAnalyticsChart"/></returns>
	IAnalyticsChart CreateHistogramChart<X, Y>(IEnumerable<X> xValues, IEnumerable<Y> yValues);

	/// <summary>
	/// Create heatmap to show analytics result.
	/// </summary>
	/// <param name="xTitles">X titles.</param>
	/// <param name="yTitles">Y titles.</param>
	/// <param name="data">Data.</param>
	/// <returns><see cref="IAnalyticsChart"/></returns>
	IAnalyticsChart CreateHeatmap(string[] xTitles, string[] yTitles, double[,] data);
}