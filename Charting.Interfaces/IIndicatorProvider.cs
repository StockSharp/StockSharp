namespace StockSharp.Charting
{
	using System.Collections.Generic;

	using StockSharp.Algo.Indicators;

	/// <summary>
	/// Provider <see cref="IndicatorType"/>.
	/// </summary>
	public interface IIndicatorProvider
	{
		/// <summary>
		/// Get all indicator types.
		/// </summary>
		/// <returns>All indicator types.</returns>
		IEnumerable<IndicatorType> GetIndicatorTypes();
	}
}