#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.Xaml.QuikPublic
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Collections.Generic;
using System.Linq;

namespace StockSharp.Quik.Xaml
{
	/// <summary>
	/// Вспомогательный класс для работы со списком столбцов DDE таблиц.
	/// </summary>
	static class Extensions
	{
		/// <summary>
		/// Получить список столбцов по их названиям.
		/// </summary>
		/// <param name="type">Тип таблицы.</param>
		/// <param name="columns">Названия столбцов.</param>
		/// <returns>Список столбцов.</returns>
		public static IEnumerable<DdeTableColumn> GetColumns(this Type type, IEnumerable<string> columns)
		{
			return columns.Select(column => (DdeTableColumn)type.GetProperty(column).GetValue(null, null));
		}
	}
}
