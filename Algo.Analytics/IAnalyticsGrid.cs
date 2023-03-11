namespace StockSharp.Algo.Analytics;

/// <summary>
/// The interface for work with the grid.
/// </summary>
public interface IAnalyticsGrid
{
	/// <summary>
	/// Set sorting mode.
	/// </summary>
	/// <param name="column">The column.</param>
	/// <param name="asc">The direction.</param>
	void SetSort(string column, bool asc);

	/// <summary>
	/// Set row.
	/// </summary>
	/// <param name="row">Row.</param>
	void SetRow(params object[] row);
}