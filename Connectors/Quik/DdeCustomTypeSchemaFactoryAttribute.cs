namespace StockSharp.Quik
{
	using Ecng.Serialization;

	/// <summary>
	/// Атрибут для применения особой фабрики построения DDE схемы.
	/// </summary>
	public class DdeCustomTypeSchemaFactoryAttribute : TypeSchemaFactoryAttribute
	{
		/// <summary>
		/// Создать атрибут.
		/// </summary>
		/// <param name="searchBy">Тип членов.</param>
		/// <param name="scope">Область видимости.</param>
		public DdeCustomTypeSchemaFactoryAttribute(SearchBy searchBy, VisibleScopes scope)
			: base(searchBy, scope)
		{
		}

		/// <summary>
		/// Создать фабрику.
		/// </summary>
		/// <returns>Фабрика построения DDE схемы.</returns>
		protected override SchemaFactory CreateFactory()
		{
			return new DdeCustomTypeSchemaFactory(SearchBy, Scope);
		}
	}
}