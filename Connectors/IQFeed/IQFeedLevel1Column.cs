#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.IQFeed.IQFeed
File: IQFeedLevel1Column.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.IQFeed
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The column describing the Level1 data flow.
	/// </summary>
	public class IQFeedLevel1Column
	{
		internal IQFeedLevel1Column(string name, Type type)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			if (type == null)
				throw new ArgumentNullException(nameof(type));

			Name = name;
			Type = type;

			Field = DefaultField;
		}

		internal IQFeedLevel1Column(string name, Type type, string format)
			: this(name, type)
		{
			if (format.IsEmpty())
				throw new ArgumentNullException(nameof(format));

			Format = format;
		}

		/// <summary>
		/// Column name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Data type.
		/// </summary>
		public Type Type { get; private set; }

		/// <summary>
		/// The data format (if <see cref="IQFeedLevel1Column.Type"/> equals to <see cref="DateTime"/> or <see cref="TimeSpan"/>).
		/// </summary>
		public string Format { get; private set; }

		internal const Level1Fields DefaultField = (Level1Fields)(-1);

		internal Level1Fields Field { get; set; }

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Name;
		}
	}
}