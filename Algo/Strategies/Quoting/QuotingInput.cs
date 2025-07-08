namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// Input data for the quoting engine.
/// </summary>
public class QuotingInput
{
	/// <summary>
	/// Current order state information.
	/// </summary>
	public class OrderState
	{
		/// <summary>
		/// Order price.
		/// </summary>
		public decimal? Price { get; set; }

		/// <summary>
		/// Order balance (remaining volume).
		/// </summary>
		public decimal Volume { get; set; }

		/// <summary>
		/// Order side.
		/// </summary>
		public Sides Side { get; set; }

		/// <summary>
		/// Order type.
		/// </summary>
		public OrderTypes? Type { get; set; }

		/// <summary>
		/// Whether order is pending (being registered/cancelled).
		/// </summary>
		public bool IsPending { get; set; }
	}

	/// <summary>
	/// Current time.
	/// </summary>
	public DateTimeOffset CurrentTime { get; set; }

	/// <summary>
	/// Current position.
	/// </summary>
	public decimal Position { get; set; }

	/// <summary>
	/// Best bid price from order book.
	/// </summary>
	public decimal? BestBidPrice { get; set; }

	/// <summary>
	/// Best ask price from order book.
	/// </summary>
	public decimal? BestAskPrice { get; set; }

	/// <summary>
	/// Last trade price.
	/// </summary>
	public decimal? LastTradePrice { get; set; }

	/// <summary>
	/// Bid quotes from order book.
	/// </summary>
	public QuoteChange[] Bids { get; set; } = [];

	/// <summary>
	/// Ask quotes from order book.
	/// </summary>
	public QuoteChange[] Asks { get; set; } = [];

	/// <summary>
	/// Current order state (if any).
	/// </summary>
	public OrderState CurrentOrder { get; set; }

	/// <summary>
	/// Whether trading is allowed.
	/// </summary>
	public bool IsTradingAllowed { get; set; }

	/// <summary>
	/// Whether cancellation is allowed.
	/// </summary>
	public bool IsCancellationAllowed { get; set; }
}