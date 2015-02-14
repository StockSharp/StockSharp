namespace StockSharp.Alor.Metadata
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	/// <summary>
	/// Менеджер  по столбцам таблиц 
	/// </summary>
	public class AlorManagerColumns
	{
		/// <summary>
		/// Список загружаемых столбцов при вызове метода startexport
		/// </summary>
		public readonly HashSet<AlorColumn> IncludeSetAlorColumns = new HashSet<AlorColumn>();

		/// <summary>
		/// последовательность всех доступных стобцов
		/// </summary>
		public static readonly HashSet<AlorColumn> AllAlorColumn =
			new HashSet<AlorColumn>();


		/// <summary>
		/// инициализация всех столбцов  и следовательно запись в AllAlorColumn
		/// </summary>
		public static void InitMetadata()
		{
			typeof(AlorTradeColumns).GetFields().ForEach(column => column.GetValue(null));
			typeof(AlorTimeColumns).GetFields().ForEach(column => column.GetValue(null));
			typeof(AlorHoldingColumns).GetFields().ForEach(column => column.GetValue(null));
			typeof(AlorPortfolioColumns).GetFields().ForEach(column => column.GetValue(null));
			typeof(AlorMoneyPositionsColumns).GetFields().ForEach(column => column.GetValue(null));
			typeof(AlorStopOrderColumns).GetFields().ForEach(column => column.GetValue(null));
			typeof(AlorSecurityColumns).GetFields().ForEach(column => column.GetValue(null));
			typeof(AlorQuotesColumns).GetFields().ForEach(column => column.GetValue(null));
			typeof(AlorOrderColumns).GetFields().ForEach(column => column.GetValue(null));
			typeof(AlorMyTradeColumns).GetFields().ForEach(column => column.GetValue(null));
		}

		internal AlorManagerColumns()
		{
			AllAlorColumn.Where(alorColumn => alorColumn.IsMandatory).ForEach(Add);
		}

		/// <summary>
		/// добавить столбец в загрузку
		/// </summary>
		/// <param name="column"></param>
		public void Add(AlorColumn column)
		{
			IncludeSetAlorColumns.Add(column);
		}

		/// <summary>
		/// удалить столбец из загрузки
		/// </summary>
		/// <param name="column"></param>
		public void Remove(AlorColumn column)
		{
			if (column.IsMandatory)
				return;
			IncludeSetAlorColumns.Remove(column);
		}

		/// <summary>
		/// Удаляет все возможное столбцы по таблице
		/// </summary>
		/// <param name="table"></param>
		public void RemoveAllBy(AlorTable table)
		{
			AllAlorColumn.Where(column => column.TableType == table.Type).ForEach(Remove);
		}

		/// <summary>
		/// Удаляет все возможное столбцы
		/// </summary>
		public void RemoveAll()
		{
			AllAlorColumn.ForEach(Remove);
		}

		/// <summary>
		/// добавляет все возможные столбцы по таблице
		/// </summary>
		/// <param name="table"></param>
		public void AddAllBy(AlorTable table)
		{
			AllAlorColumn.Where(column => column.TableType == table.Type).ForEach(Add);
		}

		/// <summary>
		/// добавляет все возможные столбцы 
		/// </summary>
		public void AddAll()
		{
			AllAlorColumn.ForEach(Add);
		}

		internal IEnumerable<AlorColumn> GetColumnsBy(AlorTableTypes table)
		{
			return IncludeSetAlorColumns.Where(alorColumn => alorColumn.TableType == table);
		}
	}
}