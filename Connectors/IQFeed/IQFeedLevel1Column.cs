namespace StockSharp.IQFeed
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Колонка, описывающая поток данных Level1.
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
		/// Название колонки.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Тип данных.
		/// </summary>
		public Type Type { get; private set; }

		/// <summary>
		/// Формат данных (в случае, если <see cref="Type"/> равен <see cref="DateTime"/> или <see cref="TimeSpan"/>).
		/// </summary>
		public string Format { get; private set; }

		internal const Level1Fields DefaultField = (Level1Fields)(-1);

		internal Level1Fields Field { get; set; }

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>
		/// Строковое представление.
		/// </returns>
		public override string ToString()
		{
			return Name;
		}
	}
}