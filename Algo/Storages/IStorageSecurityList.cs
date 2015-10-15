namespace StockSharp.Algo.Storages
{
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for access to the instrument storage.
	/// </summary>
	public interface IStorageSecurityList : ISecurityList, IStorageEntityList<Security>, ISecurityStorage
	{
	}
}