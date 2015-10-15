namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The class for representation in the form of list of orders, stored in external storage.
	/// </summary>
	public class OrderList : BaseStorageEntityList<Order>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrderList"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		public OrderList(IStorage storage)
			: base(storage)
		{
		}
	}
}