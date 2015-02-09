using System;
using System.Collections.Generic;
using Ecng.Common;
using Ecng.Reflection;

using StockSharp.BusinessEntities;

using TEClientLib;

namespace StockSharp.Alor.Metadata
{
using StockSharp.Localization;	internal enum AlorTableTypes
	{
		Security,
		Order,
		StopOrder,
		Trade,
		MyTrade,
		Position,
		Money,
		Portfolio,
		Quote,
		Time,
	}

	/// <summary>
	/// Информация о системной таблице Alor-Trade.
	/// </summary>
	public class AlorTable
	{
		internal AlorTable(AlorTableTypes type, string name, Action<Exception> processDataError)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			if (processDataError == null)
				throw new ArgumentNullException("processDataError");

			Type = type;
			Name = name;
			ProcessDataError = processDataError;
		}

		internal AlorTableTypes Type { get; private set; }

		/// <summary>
		/// Название таблицы.
		/// </summary>
		public string Name { get; private set; }

		internal Action<Exception> ProcessDataError { get; private set; }

		internal string[] AllFieldNames
		{
			get { return MetaTable.AllFieldNames.Split(','); }
		}

		internal SlotTable MetaTable { get; set; }
		private List<AlorColumn> _columns = new List<AlorColumn>();

		internal List<AlorColumn> Columns
		{
			set
			{
				var allFieldNames = MetaTable.AllFieldNames.Split(',');
				if (value == null)
					throw new ArgumentNullException("value");
				MetaTable.FieldNames = "";
				foreach (var alorColumn in value)
				{
					if (allFieldNames.IndexOf(alorColumn.Name) == -1)
						throw new ArgumentException(LocalizedStrings.Str3700Params.Put(alorColumn.Name, Name));
					if (MetaTable.FieldNames != "")
						MetaTable.FieldNames += ",";
					MetaTable.FieldNames += "" + alorColumn.Name;
				}

				_columns = value;
			}

			get { return _columns; }
		}

		internal object GetObject(object[] values, AlorColumn column)
		{
			if (values == null)
				throw new ArgumentNullException("values");
			if (column == null)
				throw new ArgumentNullException("column");

			int index = Columns.FindIndex(item => item.Name == column.Name);
			if (index == -1)
				throw new ArgumentException(LocalizedStrings.Str3700Params.Put(column.Name, Name), "column");

			return values[index];
		}

		internal object GetValue(object[] values, AlorColumn column)
		{
			var value = GetObject(values, column);

			try
			{
				if (value is string && (string)value == string.Empty)
				{
					if (column.DataType == typeof(string))
						return value;
					value = column.DataType.CreateInstance();
				}

				if (value == null)
					value = column.DataType.CreateInstance();

				return value.To(column.DataType);
			}

			catch (Exception ex)
			{

				ProcessDataError(ex);
				return column.DataType.CreateInstance();
			}
		}

		internal T GetValue<T>(object[] values, AlorColumn column)
		{

			return (T)GetValue(values, column);
		}

		internal bool IsCorrectType(object[] values, AlorColumn column)
		{
			var value = GetObject(values, column);
			return value.GetType() == column.DataType;
		}

		internal void FillNonMandatoryInfo(IExtendableEntity entity, object[] values)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			if (values == null)
				throw new ArgumentNullException("values");

			if (entity.ExtensionInfo == null)
				entity.ExtensionInfo = new Dictionary<object, object>();

			foreach (var column in Columns)
			{
				if (!column.IsMandatory)
					entity.ExtensionInfo[column] = GetObject(values, column);
			}
		}
	}
}