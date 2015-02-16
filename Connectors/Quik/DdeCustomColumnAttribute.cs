namespace StockSharp.Quik
{
	using Ecng.Serialization;

	/// <summary>
	/// Атрибут для задания имени колонки таблицы, определяемая через <see cref="DdeCustomTableAttribute"/>.
	/// </summary>
	public class DdeCustomColumnAttribute : FieldAttribute
	{
		/// <summary>
		/// Создать атрибут.
		/// </summary>
		/// <param name="ddeColumnName">Название колонки таблицы, передаваемой через DDE.</param>
		public DdeCustomColumnAttribute(string ddeColumnName)
			: base(ddeColumnName)
		{
		}

		/// <summary>
		/// Название колонки таблицы, передаваемой через DDE.
		/// </summary>
		public string DdeColumnName
		{
			get { return Name; }
		}
	}
}