using System;
using System.Collections.Generic;
using System.Linq;

namespace StockSharp.Algo.Export.Database.DbProviders
{
	using Ecng.Common;
	using Ecng.Xaml.Database;

	internal class SQLiteDbProvider : BaseDbProvider
	{
		public SQLiteDbProvider(DatabaseConnectionPair connection)
			: base(connection)
		{
		}

		public override void InsertBatch(Table table, IEnumerable<IDictionary<string, object>> parameters)
		{
			using (var trans = Database.BeginBatch())
			{
				base.InsertBatch(table, parameters);
				trans.Commit();
			}
		}

		protected override string CreatePrimaryKeyString(Table table)
		{
			var keyColumns = (from column in table.Columns where column.IsPrimaryKey select column).ToArray();
			//SQLite не поддреживает больше одного primary key - по идее нужно делать триггер...
			if (!keyColumns.Any() || keyColumns.Count() > 1)
				return null;
			return base.CreatePrimaryKeyString(table);
		}

		protected override string CreateIsTableExistsString(Table table)
		{
			return "SELECT name FROM sqlite_master WHERE type='table' AND name='{0}'".Put(table.Name);
		}

		protected override string GetDbType(Type t, object restriction)
		{
			//anothar:SQLite не поддерживает datetime2, а только datetime то есть округляет до трех знаков в миллисекундах-нам не подходит.
			if (t == typeof(DateTime))
				return "TEXT";
			if (t == typeof(bool))
				return "boolean";

			return base.GetDbType(t, restriction);
		}
	}
}