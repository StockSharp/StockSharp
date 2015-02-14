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
