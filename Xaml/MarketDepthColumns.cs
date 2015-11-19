namespace StockSharp.Xaml
{
	/// <summary>
	/// Columns of order book window.
	/// </summary>
	public enum MarketDepthColumns
	{
		/// <summary>
		/// The own amount of bid (+ stop amount to buy).
		/// </summary>
		OwnBuy,

		/// <summary>
		/// The amount of bid.
		/// </summary>
		Buy,

		/// <summary>
		/// Price.
		/// </summary>
		Price,

		/// <summary>
		/// The amount of ask.
		/// </summary>
		Sell,

		/// <summary>
		/// The net amount of ask (+ stop amount to sale).
		/// </summary>
		OwnSell,
	}
}