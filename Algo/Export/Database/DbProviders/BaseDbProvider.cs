namespace StockSharp.Algo.Export.Database.DbProviders
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Data;
	using System.Data.Common;

	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Xaml.Database;

	internal class BaseDbProvider : Disposable
	{
		public static BaseDbProvider Create(DatabaseConnectionPair connection)
		{
			if (connection.Provider is SqlServerDatabaseProvider)
				return new MSSQLDbProvider(connection);
			else
				return new SQLiteDbProvider(connection);
		}

		public BaseDbProvider(DatabaseConnectionPair connection)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");

			Database = new Database("Export", connection.ConnectionString) { Provider = connection.Provider };
		}

		protected override void DisposeManaged()
		{
			Database.Dispose();
			base.DisposeManaged();
		}

		/// <summary>
		/// Проверять уникальность данных в базе данных. Влияет на производительность.
		/// </summary>
		public bool CheckUnique { get; set; }

		public Database Database { get; private set; }

		public virtual void InsertBatch(Table table, IEnumerable<IDictionary<string, object>> parameters)
		{
			using (var connection = Database.CreateConnection())
			{
				foreach (var value in parameters)
				{
					using (var command = CreateCommand(connection, CreateInsertSqlString(table, value), value.ToDictionary(par => "@" + par.Key, par => par.Value)))
						command.ExecuteNonQuery();
				}
			}
		}

		public void CreateIfNotExists(Table table)
		{
			using (var connection = Database.CreateConnection())
			{
				using (var command = CreateCommand(connection, CreateIsTableExistsString(table), null))
				{
					var result = command.ExecuteScalar();
					if (result != null)
					{
						var value = result as string;
						if (value != null)
						{
							if (!value.IsEmpty())
								return;
						}

						if (result is int)
						{
							if ((int)result != 0)
								return;
						}

						return;
					}
				}

				using (var command = CreateCommand(connection, CreateCreateTableString(table), null))
					command.ExecuteNonQuery();	
			}
		}

		private IDbCommand CreateCommand(DbConnection connection, string sqlString, IDictionary<string, object> parameters)
		{
			var command = Database.Provider.CreateCommand(sqlString, CommandType.Text);
			command.Connection = connection;

			if (parameters != null)
			{
				foreach (var par in parameters)
				{
					var cparam = Database.Provider.Factory.CreateParameter();
					cparam.Direction = ParameterDirection.Input;
					cparam.ParameterName = par.Key;
					cparam.Value = par.Value;
					command.Parameters.Add(cparam);
				}
			}

			return command;
		}

		private static String CreateInsertSqlString(Table table, IDictionary<string, object> parameters)
		{
			var sb = new StringBuilder();
			sb.AppendLine("IF NOT EXISTS (SELECT * FROM {0} WHERE {1})".Put(table.Name, table.Columns.Where(c => c.IsPrimaryKey).Select(c => "{0} = @{0}".Put(c.Name)).Join(" AND ")));
			sb.AppendLine("BEGIN");
			sb.Append("INSERT INTO ");
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
			sb.Append("END");
			return sb.ToString();
		}

		protected virtual String CreateIsTableExistsString(Table table)
		{
			return "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}'".Put(table.Name);
		}

		private String CreateCreateTableString(Table table)
		{
			var tableName = table.Name;

			var sb = new StringBuilder();
			sb.Append("CREATE TABLE [");
			sb.Append(tableName);
			sb.Append("] (");

			foreach (var column in table.Columns)
			{
				sb.Append("[");
				sb.Append(column.Name);
				sb.Append("]");
				var type = column.DbType;
				bool isNullable = false;
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					isNullable = true;
					type = type.GetGenericArguments()[0];
				}
				if (type == typeof(String))
					isNullable = true;
				sb.Append(" ");
				sb.Append(GetDbType(type, column.ValueRestriction));
				if (!isNullable)
					sb.Append(" NOT NULL ");
				sb.Append(",");
			}

			sb.Remove(sb.Length - 1, 1);

			var primaryKeyString = CreatePrimaryKeyString(table);
			if (!primaryKeyString.IsEmpty())
				sb.AppendFormat(" {0}", primaryKeyString);
			
			sb.Append(")");
			return sb.ToString();
		}

		protected virtual String CreatePrimaryKeyString(Table table)
		{
			var keyColumns = (from column in table.Columns where column.IsPrimaryKey select column.Name).ToArray();
			if (!keyColumns.Any())
				return null;
			var sb = new StringBuilder();
			sb.Append("PRIMARY KEY(");
			foreach (var column in keyColumns)
			{
				sb.Append("[");
				sb.Append(column);
				sb.Append("],");
			}
			sb.Remove(sb.Length - 1, 1);
			sb.Append(")");
			return sb.ToString();
		}

		protected virtual String GetDbType(Type t, object restriction)
		{
			if (t == typeof(String))
			{
				var srest = restriction as StringRestriction;
				if (srest != null)
				{
					if (srest.IsFixedSize)
						return "nchar({0})".Put(srest.MaxLength);
					else if (srest.MaxLength > 0)
						return "nvarchar({0})".Put(srest.MaxLength == int.MaxValue ? "max" : srest.MaxLength.To<string>());
				}
				else
					return "ntext";
			}
			if (t == typeof(long))
				return "BIGINT";
			if (t == typeof(int))
				return "INTEGER";

			if (t == typeof(decimal))
			{
				var drest = restriction as DecimalRestriction;
				if (drest != null)
				{
					return String.Format("decimal({0},{1})", drest.Precision, drest.Scale);
				}
				return "double";
			}
			if (t == typeof(Enum))
				return "INTEGER";
			if (t == typeof(double))
				return "FLOAT";
			if (t == typeof(float))
				return "FLOAT";

			throw new NotSupportedException(t.Name + " is not supported by BaseDbProvider");
		}
	}
}