namespace StockSharp.Charting;

/// <summary>
/// Base interface for all chart components.
/// </summary>
public interface IThemeableChart : IPersistable
{
	/// <summary>
	/// The name of the graphic theme.
	/// </summary>
	string ChartTheme { get; set; }

	/// <summary>
	/// Create <see cref="IChartDrawData"/> instance.
	/// </summary>
	/// <returns><see cref="IChartDrawData"/> instance.</returns>
	IChartDrawData CreateData();

	/// <summary>
	/// To process the new data.
	/// </summary>
	/// <param name="data">New data.</param>
	void Draw(IChartDrawData data);
}