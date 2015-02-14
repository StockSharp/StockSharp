namespace StockSharp.Algo.Storages
{
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Интерфейс для доступа к хранилищу инструментов.
	/// </summary>
	public interface IStorageSecurityList : ISecurityList, IStorageEntityList<Security>, ISecurityStorage
	{
	}
}