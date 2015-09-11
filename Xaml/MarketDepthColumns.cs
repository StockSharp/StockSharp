namespace StockSharp.Xaml
{
	/// <summary>
	/// Columns of order book window.
	/// </summary>
	public enum MarketDepthColumns
	{
		/// <summary>
		/// The own amount to buy (+ stop amount to buy).
		/// </summary>
		OwnBuy,

		/// <summary>
		/// The amount to buy.
		/// </summary>
		Buy,

		/// <summary>
		/// Price.
		/// </summary>
		Price,

		/// <summary>
		/// The amount to sale.
		/// </summary>
		Sell,

		/// <summary>
		/// The net amount to sale (+ stop amount to sale).
		/// </summary>
		OwnSell,
	}
}