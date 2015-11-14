namespace StockSharp.Alor.Metadata
{
	using System;

	using Ecng.Common;

	/// <summary>
	///  Описание столбца.
	/// </summary>
	public class AlorColumn : Equatable<AlorColumn>
	{
		internal AlorColumn(AlorTableTypes tableType, string name, Type dataType, bool isMandatory = true)
		{
			//TableTypeName = TableType.ToString();
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			TableType = tableType;
			IsMandatory = isMandatory;
			Name = name;
			DataType = dataType;
			//TableTypeName = TableType.ToString();
			AlorManagerColumns.AllAlorColumn.Add(this);
		}

		///// <summary>
		///// getTableTypeName
		///// </summary>
		///// <returns></returns>
		//public string TableTypeName;

		internal AlorTableTypes TableType { get; private set; }

		/// <summary>
		/// входит ли загрузку по умолчанию
		/// </summary>
		public bool IsMandatory { get; private set; }

		/// <summary>
		/// Название колонки.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Тип колонки.
		/// </summary>
		public Type DataType { get; private set; }

		/// <summary>
		/// Создать копию <see cref="AlorColumn"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override AlorColumn Clone()
		{
			return new AlorColumn(TableType, Name, DataType)
			{
				IsMandatory = IsMandatory
			};
		}

		/// <summary>
		/// Сравнить <see cref="AlorColumn" /> на эквивалентность.
		/// </summary>
		/// <param name="other">Другое значение, с которым необходимо сравнивать.</param>
		/// <returns><see langword="true"/>, если другое значение равно текущему, иначе, <see langword="false"/>.</returns>
		protected override bool OnEquals(AlorColumn other)
		{
			return TableType == other.TableType && Name == other.Name;
		}

		/// <summary>
		/// Рассчитать хеш-код колонки.
		/// </summary>
		/// <returns>Хеш-код колонки.</returns>
		public override int GetHashCode()
		{
			return TableType.GetHashCode() ^ Name.GetHashCode();
		}
	}
}