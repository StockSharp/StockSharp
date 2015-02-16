namespace StockSharp.Quik
{
	using Ecng.Serialization;

	class DdeCustomTypeSchemaFactory : TypeSchemaFactory
	{
		public DdeCustomTypeSchemaFactory(SearchBy searchBy, VisibleScopes scope)
			: base(searchBy, scope)
		{
		}
	}
}