using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;

namespace StockSharp.Algo.Export.Database.DbProviders
{
	using Ecng.Common;
	using Ecng.Xaml.Database;

	internal class MSSQLDbProvider : BaseDbProvider
	{
		public MSSQLDbProvider(DatabaseConnectionPair connection)
			: base(connection)
		{
		}

		public override void InsertBatch(Table table, IEnumerable<IDictionary<string, object>> parameters)
		{
			if (CheckUnique)
			{
				base.InsertBatch(table, parameters);
				return;
			}

			using (var con = Database.CreateConnection())
			using (var blkcopy = new SqlBulkCopy((SqlConnection)con, SqlBulkCopyOptions.KeepIdentity, null))
			{
				blkcopy.DestinationTableName = table.Name;
				blkcopy.WriteToServer(CreateTable(table, parameters));
			}
		}

		private static DataTable CreateTable(Table table, IEnumerable<IDictionary<string, object>> parameters)
		{
			var result = new DataTable(table.Name);

			var primaryKeys = new List<DataColumn>();

			foreach (var column in table.Columns)
			{
				var dbType = column.DbType;

				if (dbType.IsGenericType && dbType.IsNullable())
					dbType = dbType.GetUnderlyingType();

				var dbCol = result.Columns.Add(column.Name, dbType);

				if (column.IsPrimaryKey)
					primaryKeys.Add(dbCol);
			}

			result.PrimaryKey = primaryKeys.ToArray();

			foreach (var param in parameters)
			{
				var row = result.NewRow();

				foreach (var pair in param)
					row[pair.Key] = pair.Value ?? DBNull.Value;

				result.Rows.Add(row);
			}

			return result;
		}

		protected override string GetDbType(Type t, object restriction)
		{
			if (t == typeof(DateTimeOffset))
				return "DATETIMEOFFSET";
			if (t == typeof(DateTime))
				return "DATETIME2";
			if (t == typeof(TimeSpan))
				return "TIME";
			if (t == typeof(Guid))
				return "GUID";
			if (t == typeof(bool))
				return "bit";

			return base.GetDbType(t, restriction);
		}
	}
}