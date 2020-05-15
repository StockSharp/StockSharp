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

	/// <summary>
	/// Table.
	/// </summary>
	public abstract class Table
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Table"/>.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="columns">Columns.</param>
		protected Table(string name, IEnumerable<ColumnDescription> columns)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			if (columns == null)
				throw new ArgumentNullException(nameof(columns));

			Name = name;
			Columns = columns.ToArray();
		}

		/// <summary>
		/// Name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Columns.
		/// </summary>
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
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Column.
	/// </summary>
	public class ColumnDescription
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ColumnDescription"/>.
		/// </summary>
		/// <param name="name">Name.</param>
		public ColumnDescription(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Identifier.
		/// </summary>
		public bool IsPrimaryKey { get; set; }

		/// <summary>
		/// Type.
		/// </summary>
		public Type DbType { get; set; }

		/// <summary>
		/// Restriction.
		/// </summary>
		public object ValueRestriction { get; set; }
	}

	/// <summary>
	/// <see cref="string"/> restriction.
	/// </summary>
	public class StringRestriction
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StringRestriction"/>.
		/// </summary>
		/// <param name="maxLength"></param>
		public StringRestriction(int maxLength)
		{
			MaxLength = maxLength;
		}

		/// <summary>
		/// Max length.
		/// </summary>
		public int MaxLength { get; set; }

		/// <summary>
		/// Fixed size.
		/// </summary>
		public bool IsFixedSize { get; set; }
	}

	/// <summary>
	/// <see cref="decimal"/> restriction.
	/// </summary>
	public class DecimalRestriction
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DecimalRestriction"/>.
		/// </summary>
		public DecimalRestriction()
		{
			Precision = 15;
			Scale = 5;
		}

		/// <summary>
		/// Precision.
		/// </summary>
		public int Precision { get; set; }

		/// <summary>
		/// Scale.
		/// </summary>
		public int Scale { get; set; }
	}
}