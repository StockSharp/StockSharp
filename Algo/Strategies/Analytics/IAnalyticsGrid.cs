namespace StockSharp.Algo.Strategies.Analytics
{
	using System.Collections.Generic;
	using System.ComponentModel;

	/// <summary>
	/// The interface for work with the grid column.
	/// </summary>
	public interface IAnalyticsGridColumn
	{
		/// <summary>
		/// Width of the column.
		/// </summary>
		double Width { get; set; }
	}

	/// <summary>
	/// The interface for work with the grid.
	/// </summary>
	public interface IAnalyticsGrid
	{
		/// <summary>
		/// Remove all columns.
		/// </summary>
		void ClearColumns();

		/// <summary>
		/// Add new column.
		/// </summary>
		/// <param name="fieldName">Field name to bind.</param>
		/// <param name="header">Header text.</param>
		/// <returns>The new column.</returns>
		IAnalyticsGridColumn AddColumn(string fieldName, string header);

		/// <summary>
		/// Create item source.
		/// </summary>
		/// <typeparam name="T">Data type.</typeparam>
		/// <returns>Items source.</returns>
		ICollection<T> CreateSource<T>();

		/// <summary>
		/// Set sorting mode.
		/// </summary>
		/// <param name="column">The column.</param>
		/// <param name="direction">The direction.</param>
		void SetSort(IAnalyticsGridColumn column, ListSortDirection direction);
	}
}