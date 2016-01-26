#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.QuikPublic
File: DdeCustomTableAttribute.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Quik
{
	using Ecng.Serialization;

	/// <summary>
	/// Атрибут для задания имени таблицы, которую необходимо обрабатывать и конвертировать в бизнес-объект.
	/// </summary>
	public class DdeCustomTableAttribute : EntityAttribute
	{
		/// <summary>
		/// Создать атрибут.
		/// </summary>
		/// <param name="ddeTableName">Название таблицы, передаваемой через DDE.</param>
		public DdeCustomTableAttribute(string ddeTableName)
			: base(ddeTableName)
		{
		}

		/// <summary>
		/// Название таблицы, передаваемой через DDE.
		/// </summary>
		public string DdeTableName => Name;
	}
}