namespace StockSharp.Alor.Metadata
{
	/// <summary>
	/// Колонки системной таблицы ORDERBOOK.
	/// </summary>
	public static class AlorQuotesColumns
	{
		/// <summary>
		/// Идентификатор строки.
		/// </summary>
		public static readonly AlorColumn Id = new AlorColumn(AlorTableTypes.Quote, "ID", typeof(int));

		/// <summary>
		/// Идентификатор режима торгов для финансового инструмента.
		/// </summary>
		public static readonly AlorColumn SecurityBoard = new AlorColumn(AlorTableTypes.Quote, "SecBoard", typeof(string), false);

		/// <summary>
		/// Идентификатор финансового инструмента.
		/// </summary>
		public static readonly AlorColumn SecurityCode = new AlorColumn(AlorTableTypes.Quote, "SecCode", typeof(string), false);

		/// <summary>
		/// Направленность котировки.
		/// </summary>
		public static readonly AlorColumn Direction = new AlorColumn(AlorTableTypes.Quote, "BuySell", typeof(string));

		/// <summary>
		/// Цена котировки.
		/// </summary>
		public static readonly AlorColumn Price = new AlorColumn(AlorTableTypes.Quote, "Price", typeof(decimal));

		/// <summary>
		/// Количество ценных бумаг, выраженное в лотах.
		/// </summary>
		public static readonly AlorColumn Volume = new AlorColumn(AlorTableTypes.Quote, "Quantity", typeof(int));
	}
}