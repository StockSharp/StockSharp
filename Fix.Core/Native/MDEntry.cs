namespace StockSharp.Fix.Native;

/// <summary>
/// The market data string.
/// </summary>
public struct MDEntry
{
	/// <summary>
	/// Data type. Available types <see cref="MDEntryType"/>.
	/// </summary>
	public char Type { get; set; }

	/// <summary>
	/// Time.
	/// </summary>
	public DateTime Time { get; set; }

	/// <summary>
	/// Price.
	/// </summary>
	public decimal? Price { get; set; }

	/// <summary>
	/// Volume.
	/// </summary>
	public decimal? Size { get; set; }

	/// <summary>
	/// Action. Available types <see cref="MDUpdateAction"/>.
	/// </summary>
	public char? Action { get; set; }

	/// <summary>
	/// Position.
	/// </summary>
	public int? Position { get; set; }

	/// <summary>
	/// Position (extra).
	/// </summary>
	public int? PositionExtra { get; set; }

	/// <summary>
	/// Identifier.
	/// </summary>
	public string EntryId { get; set; }

	/// <summary>
	/// The tick direction.
	/// </summary>
	public char? TickDirection { get; set; }

	/// <summary>
	/// Security ID.
	/// </summary>
	public SecurityId? SecurityId { get; set; }

	/// <summary>
	/// The additional string value.
	/// </summary>
	public string OtherValue { get; set; }

	/// <summary>
	/// Currency.
	/// </summary>
	public CurrencyTypes? Currency { get; set; }

	/// <summary>
	/// Number of orders.
	/// </summary>
	public decimal? NumberOfOrders { get; set; }

	/// <summary>
	/// Opening price.
	/// </summary>
	public decimal? OpenPrice { get; set; }

	/// <summary>
	/// Volume at open.
	/// </summary>
	public decimal? OpenSize { get; set; }

	/// <summary>
	/// Highest price.
	/// </summary>
	public decimal? HighPrice { get; set; }

	/// <summary>
	/// Volume at high.
	/// </summary>
	public decimal? HighSize { get; set; }

	/// <summary>
	/// Lowest price.
	/// </summary>
	public decimal? LowPrice { get; set; }

	/// <summary>
	/// Volume at low
	/// </summary>
	public decimal? LowSize { get; set; }

	/// <summary>
	/// Closing price.
	/// </summary>
	public decimal? ClosePrice { get; set; }

	/// <summary>
	/// Volume at close.
	/// </summary>
	public decimal? CloseSize { get; set; }

	/// <summary>
	/// Candle state.
	/// </summary>
	public CandleStates? CandleState { get; set; }

	/// <summary>
	/// Space-delimited list of conditions describing a quote.
	/// </summary>
	public string QuoteCondition { get; set; }

	/// <summary>
	/// Originator.
	/// </summary>
	public char? Originator { get; set; }

	/// <summary>
	/// Number of up trending ticks.
	/// </summary>
	public int? UpTicks { get; set; }

	/// <summary>
	/// Number of down trending ticks.
	/// </summary>
	public int? DownTicks { get; set; }

	/// <summary>
	/// Number of ticks.
	/// </summary>
	public int? TotalTicks { get; set; }

	/// <summary>
	/// Price levels.
	/// </summary>
	public CandlePriceLevel[] PriceLevels { get; set; }

	/// <summary>
	/// Yield.
	/// </summary>
	public decimal? Yield { get; set; }

	/// <summary>
	/// Buying party in a trade.
	/// </summary>
	public string Buyer { get; set; }

	/// <summary>
	/// Selling party in a trade.
	/// </summary>
	public string Seller { get; set; }
}