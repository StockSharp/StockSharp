namespace StockSharp.Alor.Metadata
{
	using System;

	/// <summary>
	/// Колонки системной таблицы TESYSTIME.
	/// </summary>
	public static class AlorTimeColumns
	{
		/// <summary>
		/// Идентификатор строки.
		/// </summary>
		public static readonly AlorColumn Id = new AlorColumn(AlorTableTypes.Time, "ID", typeof(int), false);

		/// <summary>
		/// Время торгового сервера.
		/// </summary>
		public static readonly AlorColumn Time = new AlorColumn(AlorTableTypes.Time, "Time", typeof(DateTime));
	}
}