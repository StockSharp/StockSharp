namespace StockSharp.Charting;

/// <summary>
/// The chart element representing orders.
/// </summary>
public interface IChartOrderElement : IChartTransactionElement
{
	/// <summary>
	/// Fill color of transaction errors.
	/// </summary>
	Color ErrorColor { get; set; }

	/// <summary>
	/// Stroke color of transaction errors.
	/// </summary>
	Color ErrorStrokeColor { get; set; }

	/// <summary>
	/// Orders display filter.
	/// </summary>
	ChartOrderDisplayFilter Filter { get; set; }
}
