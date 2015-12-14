#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.QuikPublic
File: DdeCustomTypeSchemaFactoryAttribute.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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