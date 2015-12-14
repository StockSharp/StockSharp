#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alor.Metadata.Alor
File: AlorQuotesColumns.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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