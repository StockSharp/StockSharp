namespace StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Result of order matching.
/// </summary>
public class MatchResult
{
	/// <summary>
	/// Order that was matched.
	/// </summary>
	public EmulatorOrder Order { get; init; }

	/// <summary>
	/// Whether order was fully matched.
	/// </summary>
	public bool IsFullyMatched => RemainingVolume <= 0;

	/// <summary>
	/// Whether any trades were executed.
	/// </summary>
	public bool HasTrades => Trades.Count > 0;

	/// <summary>
	/// Remaining volume after matching.
	/// </summary>
	public decimal RemainingVolume { get; init; }

	/// <summary>
	/// List of trades executed.
	/// </summary>
	public IReadOnlyList<MatchTrade> Trades { get; init; } = [];

	/// <summary>
	/// Whether order should be placed in order book.
	/// </summary>
	public bool ShouldPlaceInBook { get; init; }

	/// <summary>
	/// Whether order was rejected (e.g., post-only crossing).
	/// </summary>
	public bool IsRejected { get; init; }

	/// <summary>
	/// Rejection reason if rejected.
	/// </summary>
	public string RejectionReason { get; init; }

	/// <summary>
	/// Final order state.
	/// </summary>
	public OrderStates FinalState { get; init; }

	/// <summary>
	/// Orders that were matched against (for notification).
	/// </summary>
	public IReadOnlyList<EmulatorOrder> MatchedOrders { get; init; } = [];
}

/// <summary>
/// Trade resulting from order matching.
/// </summary>
public record MatchTrade(
	decimal Price,
	decimal Volume,
	Sides InitiatorSide,
	IReadOnlyList<EmulatorOrder> CounterOrders
);

/// <summary>
/// Settings for order matching.
/// </summary>
public class MatchingSettings
{
	/// <summary>
	/// Price step of the security.
	/// </summary>
	public decimal PriceStep { get; init; } = 0.01m;

	/// <summary>
	/// Volume step of the security.
	/// </summary>
	public decimal VolumeStep { get; init; } = 1m;

	/// <summary>
	/// Spread size in price steps.
	/// </summary>
	public int SpreadSize { get; init; } = 1;

	/// <summary>
	/// Maximum depth of the order book.
	/// </summary>
	public int MaxDepth { get; init; } = 10;

	/// <summary>
	/// Whether to match market orders at any price.
	/// </summary>
	public bool AllowMarketOrdersWithoutBook { get; init; } = true;

	/// <summary>
	/// When true, use order price instead of market price for limit order trades.
	/// This is used for candle-based matching (like V1's MatchOrderByCandle).
	/// </summary>
	public bool UseOrderPriceForLimitTrades { get; init; }
}

/// <summary>
/// Interface for order matching logic.
/// </summary>
public interface IOrderMatcher
{
	/// <summary>
	/// Try to match an order against the order book.
	/// </summary>
	/// <param name="order">Order to match.</param>
	/// <param name="book">Order book to match against.</param>
	/// <param name="settings">Matching settings.</param>
	/// <returns>Match result.</returns>
	MatchResult Match(EmulatorOrder order, IOrderBook book, MatchingSettings settings);

	/// <summary>
	/// Check if order would cross the book (for post-only validation).
	/// </summary>
	/// <param name="order">Order to check.</param>
	/// <param name="book">Order book to check against.</param>
	/// <returns>True if the order would cross the book, otherwise false.</returns>
	bool WouldCross(EmulatorOrder order, IOrderBook book);

	/// <summary>
	/// Get the best execution price for a market order.
	/// </summary>
	/// <param name="side">Side of the market order (buy/sell).</param>
	/// <param name="book">Order book to query.</param>
	/// <returns>Best available price for the market order, or null if book is empty.</returns>
	decimal? GetMarketPrice(Sides side, IOrderBook book);
}
