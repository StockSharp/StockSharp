namespace StockSharp.Algo.Storages
{
	using Ecng.Collections;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for access to the instrument storage.
	/// </summary>
	public interface IStorageSecurityList : ICollectionEx<Security>, IStorageEntityList<Security>, ISecurityStorage
	{
	}
}