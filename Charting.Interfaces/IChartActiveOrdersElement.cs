namespace StockSharp.Charting;

/// <summary>
/// The chart element representing active orders.
/// </summary>
public interface IChartActiveOrdersElement : IChartElement
{
	/// <summary>
	/// Color of Buy order in non-active state.
	/// </summary>
	Color BuyPendingColor { get; set; }

	/// <summary>
	/// Color of Buy order in active state.
	/// </summary>
	Color BuyColor { get; set; }

	/// <summary>
	/// Color of blinking in partially filled state (Buy).
	/// </summary>
	Color BuyBlinkColor { get; set; }

	/// <summary>
	/// Color of Sell order in non-active state.
	/// </summary>
	Color SellPendingColor { get; set; }

	/// <summary>
	/// Color of Sell order in active state.
	/// </summary>
	Color SellColor { get; set; }

	/// <summary>
	/// Color of blinking in partially filled state (Sell).
	/// </summary>
	Color SellBlinkColor { get; set; }

	/// <summary>
	/// Cancel order button color.
	/// </summary>
	Color CancelButtonColor { get; set; }

	/// <summary>
	/// Cancel order button background color.
	/// </summary>
	Color CancelButtonBackground { get; set; }

	/// <summary>
	/// Text color.
	/// </summary>
	Color ForegroundColor { get; set; }

	/// <summary>
	/// Whether animation is enabled.
	/// </summary>
	bool IsAnimationEnabled { get; set; }
}