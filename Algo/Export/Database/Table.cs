#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Database.Algo
File: Table.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	abstract class Table
	{
		protected Table(string name, IEnumerable<ColumnDescription> columns)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			if (columns == null)
				throw new ArgumentNullException(nameof(columns));

			Name = name;
			Columns = columns.ToArray();
		}

		public string Name { get; }
		public IEnumerable<ColumnDescription> Columns { get; }
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
		public ColumnDescription(string name)
		{
			Name = name;
		}

		public string Name { get; set; }

		public bool IsPrimaryKey { get; set; }

		public Type DbType { get; set; }

		public object ValueRestriction { get; set; }
	}

	class StringRestriction
	{
		public StringRestriction(int maxLength)
		{
			MaxLength = maxLength;
		}

		public int MaxLength { get; set; }

		public bool IsFixedSize { get; set; }
	}

	class DecimalRestriction
	{
		public DecimalRestriction()
		{
			Precision = 15;
			Scale = 5;
		}

		public int Precision { get; set; }

		public int Scale { get; set; }
	}
}