namespace StockSharp.Charting;

/// <summary>
/// The interface that describes the Orders Or Trades chart element.
/// </summary>
public interface IChartTransactionElement : IChartElement
{
	/// <summary>
	/// Color of graphics element on chart, indicating buy.
	/// </summary>
	Color BuyColor { get; set; }

	/// <summary>
	/// Border color of graphics element on chart, indicating buy.
	/// </summary>
	Color BuyStrokeColor { get; set; }

	/// <summary>
	/// Color of graphics element on chart, indicating sell.
	/// </summary>
	Color SellColor { get; set; }

	/// <summary>
	/// Border color of graphics element on chart, indicating sell.
	/// </summary>
	Color SellStrokeColor { get; set; }

	/// <summary>
	/// Use alternative icons.
	/// </summary>
	bool UseAltIcon { get; set; }

	/// <summary>
	/// Draw size.
	/// </summary>
	double DrawSize { get; set; }
}
