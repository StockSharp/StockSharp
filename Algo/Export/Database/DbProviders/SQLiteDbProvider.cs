#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Database.DbProviders.Algo
File: SQLiteDbProvider.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export.Database.DbProviders
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using Ecng.Common;
	using Ecng.Xaml.DevExp.Database;

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
				using (var connection = Database.CreateConnection())
				{
					foreach (var value in parameters)
					{
						using (var command = CreateCommand(connection, CreateInsertSqlString(table, value), value.ToDictionary(par => "@" + par.Key, par => par.Value)))
							command.ExecuteNonQuery();
					}
				}

				trans.Commit();
			}
		}

		private static string CreateInsertSqlString(Table table, IDictionary<string, object> parameters)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));

			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			var sb = new StringBuilder();

			sb.Append("INSERT OR IGNORE INTO ");
			sb.Append(table.Name);
			sb.Append(" (");
			foreach (var par in parameters)
			{
				sb.Append("[");
				sb.Append(par.Key);
				sb.Append("],");
			}
			sb.Remove(sb.Length - 1, 1);
			sb.Append(") VALUES (");
			foreach (var par in parameters)
			{
				sb.Append("@");
				sb.Append(par.Key);
				sb.Append(",");
			}
			sb.Remove(sb.Length - 1, 1);
			sb.AppendLine(")");

			return sb.ToString();
		}

		protected override string CreatePrimaryKeyString(Table table, IEnumerable<ColumnDescription> columns)
		{
			var str = columns.Select(c => $"[{c.Name}]").Join(",");

			if (str.IsEmpty())
				return null;

			return $"UNIQUE ({str})";
		}

		protected override string CreateIsTableExistsString(Table table)
		{
			return $"SELECT name FROM sqlite_master WHERE type='table' AND name='{table.Name}'";
		}

		protected override string GetDbType(Type type, object restriction)
		{
			//anothar:SQLite не поддерживает datetime2, а только datetime то есть округляет до трех знаков в миллисекундах-нам не подходит.
			if (type == typeof(DateTime))
				return "TEXT";

			if (type == typeof(DateTimeOffset))
				return "TEXT";

			if (type == typeof(TimeSpan))
				return "TEXT";

			if (type == typeof(bool))
				return "boolean";

			return base.GetDbType(type, restriction);
		}
	}
}