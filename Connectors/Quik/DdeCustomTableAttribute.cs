namespace StockSharp.Quik
{
	using Ecng.Serialization;

	/// <summary>
	/// Атрибут для задания имени таблицы, которую необходимо обрабатывать и конвертировать в бизнес-объект.
	/// </summary>
	public class DdeCustomTableAttribute : EntityAttribute
	{
		/// <summary>
		/// Создать атрибут.
		/// </summary>
		/// <param name="ddeTableName">Название таблицы, передаваемой через DDE.</param>
		public DdeCustomTableAttribute(string ddeTableName)
			: base(ddeTableName)
		{
		}

		/// <summary>
		/// Название таблицы, передаваемой через DDE.
		/// </summary>
		public string DdeTableName
		{
			get { return Name; }
		}
	}
}