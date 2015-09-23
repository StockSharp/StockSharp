namespace StockSharp.InteractiveBrokers
{
	using StockSharp.Messages;

	/// <summary>
	/// Data types on which candles should be based.
	/// </summary>
	public static class CandleDataTypes
	{
		/// <summary>
		/// Trades.
		/// </summary>
		public const Level1Fields Trades = Level1Fields.LastTradePrice;

		/// <summary>
		/// Spread middle.
		/// </summary>
		public const Level1Fields Midpoint = (Level1Fields)(-1);

		/// <summary>
		/// Best bid.
		/// </summary>
		public const Level1Fields Bid = Level1Fields.BestBidPrice;

		/// <summary>
		/// Best ask.
		/// </summary>
		public const Level1Fields Ask = Level1Fields.BestAskPrice;

		/// <summary>
		/// Best pair quotes.
		/// </summary>
		public const Level1Fields BidAsk = (Level1Fields)(-2);

		/// <summary>
		/// Volatility (implied).
		/// </summary>
		public const Level1Fields ImpliedVolatility = Level1Fields.ImpliedVolatility;

		/// <summary>
		/// Volatility (historic).
		/// </summary>
		public const Level1Fields HistoricalVolatility = Level1Fields.HistoricalVolatility;

		/// <summary>
		/// The best profitable offer.
		/// </summary>
		public const Level1Fields YieldAsk = (Level1Fields)(-3);

		/// <summary>
		/// The best profitable bid.
		/// </summary>
		public const Level1Fields YieldBid = (Level1Fields)(-4);

		/// <summary>
		/// Best profitable couple of quotes.
		/// </summary>
		public const Level1Fields YieldBidAsk = (Level1Fields)(-5);

		/// <summary>
		/// The last profitable trade.
		/// </summary>
		public const Level1Fields YieldLast = (Level1Fields)(-6);
	}
}