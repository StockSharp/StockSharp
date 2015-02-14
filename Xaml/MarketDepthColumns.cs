namespace StockSharp.Xaml
{
	/// <summary>
	/// Колонки окна стакана.
	/// </summary>
	public enum MarketDepthColumns
	{
		/// <summary>
		/// Собственный объем на покупку (+ стоп-объем на покупку).
		/// </summary>
		OwnBuy,

		/// <summary>
		/// Объем на покупку.
		/// </summary>
		Buy,

		/// <summary>
		/// Цена.
		/// </summary>
		Price,

		/// <summary>
		/// Объем на продажу.
		/// </summary>
		Sell,

		/// <summary>
		/// Собственный объем на продажу (+ стоп-объем на продажу).
		/// </summary>
		OwnSell,
	}
}