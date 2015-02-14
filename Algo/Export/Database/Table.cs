namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	abstract class Table
	{
		protected Table(string name, IEnumerable<ColumnDescription> columns)
		{
			Columns = columns.ToArray();
			Name = name;
		}

		public string Name
		{
			get;
			private set;
		}

		public IEnumerable<ColumnDescription> Columns
		{
			get;
			private set;
		}
	}

	abstract class Table<T> : Table
	{
		protected Table(string name, IEnumerable<ColumnDescription> columns)
			: base(name, columns)
		{
		}

		public virtual IEnumerable<IDictionary<string, object>> ConvertToParameters(IEnumerable<T> values)
		{
			return values.Select(ConvertToParameters).ToArray();
		}

		protected virtual IDictionary<string, object> ConvertToParameters(T value)
		{
			throw new NotSupportedException();
		}
	}

	class ColumnDescription
	{
		public ColumnDescription() { }

		public ColumnDescription(String name)
		{
			Name = name;
		}

		public String Name
		{
			get;
			set;
		}

		public bool IsPrimaryKey
		{
			get;
			set;
		}

		public Type DbType
		{
			get;
			set;
		}

		public object ValueRestriction
		{
			get;
			set;
		}
	}

	class StringRestriction
	{
		public StringRestriction() { }

		public StringRestriction(int maxLength)
		{
			MaxLength = maxLength;
		}

		public int MaxLength
		{
			get;
			set;
		}

		public bool IsFixedSize
		{
			get;
			set;
		}
	}

	class DecimalRestriction
	{
		public DecimalRestriction()
		{
			Precision = 15;
			Scale = 5;
		}

		public int Precision
		{
			get;
			set;
		}

		public int Scale
		{
			get;
			set;
		}
	}
}