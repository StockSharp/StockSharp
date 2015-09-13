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
				throw new ArgumentNullException("name");

			if (type == null)
				throw new ArgumentNullException("type");

			Name = name;
			Type = type;

			Field = DefaultField;
		}

		internal IQFeedLevel1Column(string name, Type type, string format)
			: this(name, type)
		{
			if (format.IsEmpty())
				throw new ArgumentNullException("format");

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