namespace StockSharp.Algo.Analytics;

using Ecng.Reflection;

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
	/// Create chart with 2 dimension to show analytics result.
	/// </summary>
	/// <typeparam name="X">Type of X values.</typeparam>
	/// <typeparam name="Y">Type of Y values.</typeparam>
	/// <returns><see cref="IAnalyticsChart{X,Y,VoidType}"/></returns>
	IAnalyticsChart<X, Y, VoidType> CreateChart<X, Y>();

	/// <summary>
	/// Create chart with 3 dimension to show analytics result.
	/// </summary>
	/// <typeparam name="X">Type of X values.</typeparam>
	/// <typeparam name="Y">Type of Y values.</typeparam>
	/// <typeparam name="Z">Type of Z values.</typeparam>
	/// <returns><see cref="IAnalyticsChart{X,Y,Z}"/></returns>
	IAnalyticsChart<X, Y, Z> CreateChart<X, Y, Z>();

	/// <summary>
	/// Draw heatmap to show analytics result.
	/// </summary>
	/// <param name="xTitles">X titles.</param>
	/// <param name="yTitles">Y titles.</param>
	/// <param name="data">Data.</param>
	void DrawHeatmap(IEnumerable<string> xTitles, IEnumerable<string> yTitles, double[,] data);

	/// <summary>
	/// Draw 3D to show analytics result.
	/// </summary>
	/// <param name="xTitles">X titles.</param>
	/// <param name="yTitles">Y titles.</param>
	/// <param name="data">Data.</param>
	/// <param name="xTitle">Title for X axis.</param>
	/// <param name="yTitle">Title for Y axis.</param>
	/// <param name="zTitle">Title for Z axis.</param>
	void Draw3D(IEnumerable<string> xTitles, IEnumerable<string> yTitles, double[,] data, string xTitle = default, string yTitle = default, string zTitle = default);
}