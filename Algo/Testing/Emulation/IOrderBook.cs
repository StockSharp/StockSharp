namespace StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Represents a price level in the order book.
/// </summary>
/// <param name="Price">The price of the level.</param>
/// <param name="Volume">Total volume at this level.</param>
/// <param name="Orders">Orders at this level.</param>
public record OrderBookLevel(decimal Price, decimal Volume, IReadOnlyList<EmulatorOrder> Orders);

/// <summary>
/// Order stored in the emulator order book.
/// </summary>
public class EmulatorOrder
{
	/// <summary>
	/// Transaction ID of the order.
	/// </summary>
	public long TransactionId { get; init; }

	/// <summary>
	/// Order side.
	/// </summary>
	public Sides Side { get; init; }

	/// <summary>
	/// Order price.
	/// </summary>
	public decimal Price { get; init; }

	/// <summary>
	/// Remaining order volume.
	/// </summary>
	public decimal Balance { get; set; }

	/// <summary>
	/// Original order volume.
	/// </summary>
	public decimal Volume { get; init; }

	/// <summary>
	/// Portfolio name. Null for market quotes.
	/// </summary>
	public string PortfolioName { get; init; }

	/// <summary>
	/// Time in force.
	/// </summary>
	public TimeInForce? TimeInForce { get; init; }

	/// <summary>
	/// Order type.
	/// </summary>
	public OrderTypes? OrderType { get; init; }

	/// <summary>
	/// Post-only flag.
	/// </summary>
	public bool PostOnly { get; init; }

	/// <summary>
	/// Expiry date.
	/// </summary>
	public DateTime? ExpiryDate { get; init; }

	/// <summary>
	/// Server time when order was registered.
	/// </summary>
	public DateTime ServerTime { get; init; }

	/// <summary>
	/// Local time when order was registered.
	/// </summary>
	public DateTime LocalTime { get; init; }

	/// <summary>
	/// Margin price used for blocking funds (may differ from order price).
	/// </summary>
	public decimal MarginPrice { get; init; }

	/// <summary>
	/// Indicates if this is a user order (has portfolio) or market quote.
	/// </summary>
	public bool IsUserOrder => PortfolioName is not null;
}

/// <summary>
/// Interface for order book management.
/// </summary>
public interface IOrderBook
{
	/// <summary>
	/// Security ID.
	/// </summary>
	SecurityId SecurityId { get; }

	/// <summary>
	/// Get best bid price and volume.
	/// </summary>
	(decimal price, decimal volume)? BestBid { get; }

	/// <summary>
	/// Get best ask price and volume.
	/// </summary>
	(decimal price, decimal volume)? BestAsk { get; }

	/// <summary>
	/// Total volume of all bids.
	/// </summary>
	decimal TotalBidVolume { get; }

	/// <summary>
	/// Total volume of all asks.
	/// </summary>
	decimal TotalAskVolume { get; }

	/// <summary>
	/// Number of bid levels.
	/// </summary>
	int BidLevels { get; }

	/// <summary>
	/// Number of ask levels.
	/// </summary>
	int AskLevels { get; }

	/// <summary>
	/// Add a quote to the order book.
	/// </summary>
	/// <param name="order">Order or market quote to add. If <see cref="EmulatorOrder.TransactionId"/> is default, it is treated as a market quote.</param>
	void AddQuote(EmulatorOrder order);

	/// <summary>
	/// Remove a quote from the order book.
	/// </summary>
	/// <param name="transactionId">Transaction id of the order to remove.</param>
	/// <param name="side">Side (buy/sell) where the order is placed.</param>
	/// <param name="price">Price level of the order.</param>
	/// <returns>True if removed, false if not found.</returns>
	bool RemoveQuote(long transactionId, Sides side, decimal price);

	/// <summary>
	/// Update volume at a price level (for market quotes).
	/// </summary>
	/// <param name="side">Side to update.</param>
	/// <param name="price">Price level to update.</param>
	/// <param name="volume">New market volume at the level.</param>
	void UpdateLevel(Sides side, decimal price, decimal volume);

	/// <summary>
	/// Remove entire level at price.
	/// </summary>
	/// <param name="side">Side to remove level from.</param>
	/// <param name="price">Price level to remove.</param>
	/// <returns>Removed orders that were present at that level.</returns>
	IEnumerable<EmulatorOrder> RemoveLevel(Sides side, decimal price);

	/// <summary>
	/// Get quotes for a side, ordered by price (best first).
	/// </summary>
	/// <param name="side">Side to retrieve levels for.</param>
	IEnumerable<OrderBookLevel> GetLevels(Sides side);

	/// <summary>
	/// Get volume at a specific price level.
	/// </summary>
	/// <param name="side">Side to query.</param>
	/// <param name="price">Price level to query.</param>
	decimal GetVolumeAtPrice(Sides side, decimal price);

	/// <summary>
	/// Get all user orders at a specific price level.
	/// </summary>
	/// <param name="side">Side to query.</param>
	/// <param name="price">Price level to query.</param>
	IEnumerable<EmulatorOrder> GetOrdersAtPrice(Sides side, decimal price);

	/// <summary>
	/// Check if there's a quote at the specified price.
	/// </summary>
	/// <param name="side">Side to check.</param>
	/// <param name="price">Price level to check.</param>
	bool HasLevel(Sides side, decimal price);

	/// <summary>
	/// Clear all quotes.
	/// </summary>
	void Clear();

	/// <summary>
	/// Clear all quotes for a specific side.
	/// </summary>
	/// <param name="side">Side to clear.</param>
	void Clear(Sides side);

	/// <summary>
	/// Set the order book from a snapshot (replaces all non-user quotes).
	/// </summary>
	/// <param name="bids">Sequence of bid quote changes to set.</param>
	/// <param name="asks">Sequence of ask quote changes to set.</param>
	void SetSnapshot(IEnumerable<QuoteChange> bids, IEnumerable<QuoteChange> asks);

	/// <summary>
	/// Create a QuoteChangeMessage from current state.
	/// </summary>
	/// <param name="localTime">Local time to set on the generated message.</param>
	/// <param name="serverTime">Server time to set on the generated message.</param>
	QuoteChangeMessage ToMessage(DateTime localTime, DateTime serverTime);

	/// <summary>
	/// Get total volume for a side.
	/// </summary>
	/// <param name="side">Side to get total volume for.</param>
	decimal GetTotalVolume(Sides side);

	/// <summary>
	/// Remove worst levels that exceed max depth.
	/// </summary>
	/// <param name="side">Side to trim.</param>
	/// <param name="maxDepth">Maximum allowed depth to keep.</param>
	/// <returns>Removed orders from trimmed levels.</returns>
	IEnumerable<EmulatorOrder> TrimToDepth(Sides side, int maxDepth);
}
