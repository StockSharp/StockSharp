#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: ExportTypes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	/// <summary>
	/// Типы экспортов.
	/// </summary>
	public enum ExportTypes
	{
		/// <summary>
		/// В Excel.
		/// </summary>
		Excel,

		/// <summary>
		/// В xml.
		/// </summary>
		Xml,

		/// <summary>
		/// В текстовый файл.
		/// </summary>
		Txt,

		/// <summary>
		/// В базу данных.
		/// </summary>
		Sql,

		/// <summary>
		/// В формат StockSharp (bin).
		/// </summary>
		StockSharpBin,

		/// <summary>
		/// В формат StockSharp (csv).
		/// </summary>
		StockSharpCsv,
	}
}