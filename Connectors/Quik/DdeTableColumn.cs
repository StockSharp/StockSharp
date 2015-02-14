namespace StockSharp.Quik
{
	using System;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Метаинформация колонки таблицы, передаваемой под DDE.
	/// </summary>
	[Ignore(FieldName = "IsDisposed")]
	[EntityFactory(typeof(UnitializedEntityFactory<DdeTableColumn>))]
	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Both)]
	[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	public class DdeTableColumn : Equatable<DdeTableColumn>
	{
		/// <summary>
		/// Создать <see cref="DdeTableColumn"/>.
		/// </summary>
		/// <param name="name">Имя колонки.</param>
		/// <param name="dataType">Тип данных в колонке.</param>
		public DdeTableColumn(string name, Type dataType)
			: this(DdeTableTypes.None, name, dataType)
		{
		}

		internal DdeTableColumn(DdeTableTypes tableType, string name, Type dataType)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			if (dataType == null)
				throw new ArgumentNullException("dataType");

			TableType = tableType;
			Name = name;
			DataType = dataType;
		}

		/// <summary>
		/// Имя колонки.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Тип данных в колонке.
		/// </summary>
		[Member]
		public Type DataType { get; private set; }

		/// <summary>
		/// Является ли колонка обязательной.
		/// </summary>
		public bool IsMandatory { get; set; }

		internal DdeTableTypes TableType { get; set; }

		///<summary>
		/// Создать копию объекта <see cref="DdeTableColumn" />.
		///</summary>
		///<returns>Копия.</returns>
		public override DdeTableColumn Clone()
		{
			return new DdeTableColumn(TableType, Name, DataType) { IsMandatory = IsMandatory };
		}

		/// <summary>
		/// Сравнить две колонки на эквивалентность.
		/// </summary>
		/// <param name="other">Другая колонка, с которой необходимо сравнивать.</param>
		/// <returns><see langword="true"/>, если другая колонка равна текущей, иначе, <see langword="false"/>.</returns>
		protected override bool OnEquals(DdeTableColumn other)
		{
			return TableType == other.TableType && Name == other.Name;
		}

		/// <summary>
		/// Рассчитать хеш-код объекта <see cref="DdeTableColumn" />.
		/// </summary>
		/// <returns>Хеш-код.</returns>
		public override int GetHashCode()
		{
			return TableType.GetHashCode() ^ Name.GetHashCode();
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return Name;
		}
	}
}