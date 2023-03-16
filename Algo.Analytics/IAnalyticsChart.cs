namespace StockSharp.Algo.Analytics;

/// <summary>
/// The interface for work with a chart.
/// </summary>
/// <typeparam name="X">Type of X values.</typeparam>
/// <typeparam name="Y">Type of Y values.</typeparam>
/// <typeparam name="Z">Type of Z values.</typeparam>
public interface IAnalyticsChart<X, Y, Z>
{
	/// <summary>
	/// Append series.
	/// </summary>
	/// <param name="title">Series title.</param>
	/// <param name="xValues">X values.</param>
	/// <param name="yValues">Y values.</param>
	/// <param name="style"><see cref="ChartIndicatorDrawStyles"/></param>
	/// <param name="color">Series color.</param>
	void Append(string title, IEnumerable<X> xValues, IEnumerable<Y> yValues, ChartIndicatorDrawStyles style = ChartIndicatorDrawStyles.Line, Color? color = default);

	/// <summary>
	/// Append series.
	/// </summary>
	/// <param name="title">Series title.</param>
	/// <param name="xValues">X values.</param>
	/// <param name="yValues">Y values.</param>
	/// <param name="zValues">Z values.</param>
	/// <param name="style"><see cref="ChartIndicatorDrawStyles"/></param>
	/// <param name="color">Series color.</param>
	void Append(string title, IEnumerable<X> xValues, IEnumerable<Y> yValues, IEnumerable<Z> zValues, ChartIndicatorDrawStyles style = ChartIndicatorDrawStyles.Bubble, Color? color = default);
}