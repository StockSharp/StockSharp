#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Database.DbProviders.Algo
File: BaseDbProvider.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export.Database.DbProviders
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Data;
	using System.Data.Common;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Xaml.DevExp.Database;

	abstract class BaseDbProvider : Disposable
	{
		public static BaseDbProvider Create(DatabaseConnectionPair connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			if (connection.Provider is SqlServerDatabaseProvider)
				return new MSSQLDbProvider(connection);
			else
				return new SQLiteDbProvider(connection);
		}

		protected BaseDbProvider(DatabaseConnectionPair connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			Database = new Database("Export", connection.ConnectionString) { Provider = connection.Provider };
		}

		protected override void DisposeManaged()
		{
			Database.Dispose();
			base.DisposeManaged();
		}

		/// <summary>
		/// To check uniqueness of data in the database. It effects performance.
		/// </summary>
		public bool CheckUnique { get; set; }

		public Database Database { get; }

		public abstract void InsertBatch(Table table, IEnumerable<IDictionary<string, object>> parameters);

		public void CreateIfNotExists(Table table)
		{
			using (var connection = Database.CreateConnection())
			{
				using (var command = CreateCommand(connection, CreateIsTableExistsString(table), null))
				{
					var result = command.ExecuteScalar();
					if (result != null)
					{
						if (result is string value)
						{
							if (!value.IsEmpty())
								return;
						}

						if (result is int i)
						{
							if (i != 0)
								return;
						}

						return;
					}
				}

				using (var command = CreateCommand(connection, CreateCreateTableString(table), null))
					command.ExecuteNonQuery();
			}
		}

		protected IDbCommand CreateCommand(DbConnection connection, string sqlString, IDictionary<string, object> parameters)
		{
			var command = Database.Provider.CreateCommand(sqlString, CommandType.Text);
			command.Connection = connection;

			if (parameters != null)
			{
				foreach (var par in parameters)
				{
					var dbParam = Database.Provider.Factory.CreateParameter();

					if (dbParam == null)
						throw new InvalidOperationException();

					dbParam.Direction = ParameterDirection.Input;
					dbParam.ParameterName = par.Key;
					dbParam.Value = par.Value ?? DBNull.Value;
					dbParam.IsNullable = true;

					command.Parameters.Add(dbParam);
				}
			}

			return command;
		}

		protected virtual string CreateIsTableExistsString(Table table)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));

			return $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{table.Name}'";
		}

		private string CreateCreateTableString(Table table)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));

			var tableName = table.Name;

			var sb = new StringBuilder();
			sb.Append("CREATE TABLE [");
			sb.Append(tableName);
			sb.Append("] (");

			var hasColumns = false;

			foreach (var column in table.Columns)
			{
				hasColumns = true;

				sb.Append("[");
				sb.Append(column.Name);
				sb.Append("]");

				var type = column.DbType;
				
				var isNullable = false;
				
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					isNullable = true;
					type = type.GetGenericArguments()[0];
				}
				else if (type == typeof(string))
					isNullable = true;

				sb.Append(" ");
				sb.Append(GetDbType(type, column.ValueRestriction));

				if (!isNullable)
					sb.Append(" NOT NULL");

				sb.Append(", ");
			}

			var primaryKeyString = CreatePrimaryKeyString(table, table.Columns.Where(c => c.IsPrimaryKey));
			if (!primaryKeyString.IsEmpty())
				sb.AppendFormat(" {0}", primaryKeyString);
			else if (hasColumns)
				sb.Remove(sb.Length - ", ".Length, ", ".Length);
			
			sb.Append(")");
			return sb.ToString();
		}

		protected abstract string CreatePrimaryKeyString(Table table, IEnumerable<ColumnDescription> columns);

		protected virtual string GetDbType(Type type, object restriction)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (type == typeof(string))
			{
				if (restriction is StringRestriction srest)
				{
					if (srest.IsFixedSize)
						return $"nchar({srest.MaxLength})";
					else if (srest.MaxLength > 0)
						return "nvarchar({0})".Put(srest.MaxLength == int.MaxValue ? "max" : srest.MaxLength.To<string>());
				}
				else
					return "ntext";
			}

			if (type == typeof(long))
				return "BIGINT";
			if (type == typeof(int))
				return "INTEGER";

			if (type == typeof(decimal))
			{
				return restriction is DecimalRestriction drest
					? $"decimal({drest.Precision},{drest.Scale})"
					: "decimal";
			}

			if (type == typeof(Enum))
				return "INTEGER";
			if (type == typeof(double))
				return "FLOAT";
			if (type == typeof(float))
				return "FLOAT";

			throw new NotSupportedException($"{type.Name} is not supported by {nameof(BaseDbProvider)}.");
		}
	}
}