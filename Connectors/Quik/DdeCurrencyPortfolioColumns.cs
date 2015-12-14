#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.QuikPublic
File: DdeCurrencyPortfolioColumns.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Quik
{
	/// <summary>
	/// Колонки таблицы Валюты портфелей.
	/// </summary>
	public static class DdeCurrencyPortfolioColumns
	{
		static DdeCurrencyPortfolioColumns()
		{
			FirmId = new DdeTableColumn(DdeTableTypes.CurrencyPortfolio, "Фирма", typeof(string));
			Currency = new DdeTableColumn(DdeTableTypes.CurrencyPortfolio, "Валюта", typeof(string));
			GroupCode = new DdeTableColumn(DdeTableTypes.CurrencyPortfolio, "Группа", typeof(string));
			ClientCode = new DdeTableColumn(DdeTableTypes.CurrencyPortfolio, "Код клиента", typeof(string));
			BeginPosition = new DdeTableColumn(DdeTableTypes.CurrencyPortfolio, "Входящий остаток", typeof(decimal));
			BeginLimit = new DdeTableColumn(DdeTableTypes.CurrencyPortfolio, "Входящий лимит", typeof(decimal));
			CurrentPosition = new DdeTableColumn(DdeTableTypes.CurrencyPortfolio, "Текущий остаток", typeof(decimal));
			CurrentLimit = new DdeTableColumn(DdeTableTypes.CurrencyPortfolio, "Текущий лимит", typeof(decimal));
			BlockedPosition = new DdeTableColumn(DdeTableTypes.CurrencyPortfolio, "Заблокировано", typeof(decimal));
		}

		/// <summary>
		/// Идентификатор участника торгов в торговой системе.
		/// </summary>
		public static DdeTableColumn FirmId { get; private set; }

		/// <summary>
		/// Код валюты расчетов.
		/// </summary>
		public static DdeTableColumn Currency { get; private set; }

		/// <summary>
		/// Идентификатор торговой сессии, в которой ведется лимит.
		/// </summary>
		public static DdeTableColumn GroupCode { get; private set; }

		/// <summary>
		/// Код клиента в системе QUIK, на которого установлен лимит.
		/// </summary>
		public static DdeTableColumn ClientCode { get; private set; }

		/// <summary>
		/// Сумма собственных средств клиента до совершения операций.
		/// </summary>
		public static DdeTableColumn BeginPosition { get; private set; }

		/// <summary>
		/// Разрешенная сумма заемных средств до совершения операций.
		/// </summary>
		public static DdeTableColumn BeginLimit { get; private set; }

		/// <summary>
		/// Сумма собственных средств клиента на текущий момент.
		/// </summary>
		public static DdeTableColumn CurrentPosition { get; private set; }

		/// <summary>
		/// Разрешенная сумма заемных средств на текущий момент.
		/// </summary>
		public static DdeTableColumn CurrentLimit { get; private set; }

		/// <summary>
		/// Сумма средств, заблокированных под исполнение заявок клиента.
		/// </summary>
		public static DdeTableColumn BlockedPosition { get; private set; }
	}
}
