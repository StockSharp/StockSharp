namespace StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Interface for controlling margin trading logic.
/// Leverage is per-position (<see cref="PositionInfo.Leverage"/>).
/// Margin call/stop-out levels are per-portfolio (<see cref="IPortfolio.MarginCallLevel"/>, <see cref="IPortfolio.StopOutLevel"/>).
/// </summary>
public interface IMarginController
{
	/// <summary>
	/// Calculate required margin for an order, using position-level leverage.
	/// </summary>
	/// <param name="price">Order price.</param>
	/// <param name="volume">Order volume.</param>
	/// <param name="position">Position info (for leverage). Can be null (uses default leverage 1).</param>
	/// <returns>Required margin amount.</returns>
	decimal GetRequiredMargin(decimal price, decimal volume, PositionInfo position);

	/// <summary>
	/// Validate that portfolio has sufficient funds for order.
	/// </summary>
	/// <param name="portfolio">Portfolio to check.</param>
	/// <param name="price">Order price.</param>
	/// <param name="volume">Order volume.</param>
	/// <param name="position">Position info (for leverage). Can be null.</param>
	/// <returns>Error if insufficient funds, null otherwise.</returns>
	InvalidOperationException ValidateOrder(IPortfolio portfolio, decimal price, decimal volume, PositionInfo position);

	/// <summary>
	/// Calculate current margin level for a portfolio.
	/// </summary>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="unrealizedPnL">Current unrealized PnL.</param>
	/// <returns>Margin level ratio (equity / blocked). Returns <see cref="decimal.MaxValue"/> if no blocked money.</returns>
	decimal CheckMarginLevel(IPortfolio portfolio, decimal unrealizedPnL);

	/// <summary>
	/// Check if portfolio is in margin call state.
	/// Uses <see cref="IPortfolio.MarginCallLevel"/>.
	/// </summary>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="unrealizedPnL">Current unrealized PnL.</param>
	/// <returns>True if margin level is at or below margin call threshold.</returns>
	bool IsMarginCall(IPortfolio portfolio, decimal unrealizedPnL);

	/// <summary>
	/// Check if portfolio is in stop-out state.
	/// Uses <see cref="IPortfolio.EnableStopOut"/> and <see cref="IPortfolio.StopOutLevel"/>.
	/// </summary>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="unrealizedPnL">Current unrealized PnL.</param>
	/// <returns>True if stop-out is enabled and margin level is at or below stop-out threshold.</returns>
	bool IsStopOut(IPortfolio portfolio, decimal unrealizedPnL);
}
