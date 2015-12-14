#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alor.Metadata.Alor
File: AlorPortfolioColumns.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Alor.Metadata
{
	/// <summary>
	/// Колонки системной таблицы TRDACC.
	/// </summary>
	public static class AlorPortfolioColumns
	{
		/// <summary>
		/// Идентификатор строки.
		/// </summary>
		public static readonly AlorColumn Id = new AlorColumn(AlorTableTypes.Portfolio, "ID", typeof(int), false);

		/// <summary>
		/// Номер торгового счета.
		/// </summary>
		public static readonly AlorColumn Account = new AlorColumn(AlorTableTypes.Portfolio, "Account", typeof(string));

		/// <summary>
		/// Наименование торгового счета.
		/// </summary>
		public static readonly AlorColumn Name = new AlorColumn(AlorTableTypes.Portfolio, "Name", typeof(string), false);

		/// <summary>
		/// Тип торгового счета.
		/// </summary>
		public static readonly AlorColumn Type = new AlorColumn(AlorTableTypes.Portfolio, "Type", typeof(string), false);

		/// <summary>
		/// Код банковского счета.
		/// </summary>
		public static readonly AlorColumn BankAccount = new AlorColumn(AlorTableTypes.Portfolio, "BankAccount", typeof(string), false);

		/// <summary>
		/// Код депозитарного счета.
		/// </summary>
		public static readonly AlorColumn DepoAccount = new AlorColumn(AlorTableTypes.Portfolio, "DepoAccount", typeof(string), false);

		/// <summary>
		/// Сокращенное наименование банка.
		/// </summary>
		public static readonly AlorColumn BankName = new AlorColumn(AlorTableTypes.Portfolio, "BankCode", typeof(string), false);

		/// <summary>
		/// Сокращенное наименование депозитария.
		/// </summary>
		public static readonly AlorColumn DepoName = new AlorColumn(AlorTableTypes.Portfolio, "DepoCode", typeof(string), false);
	}
}