namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The class for representation in the form of list of orders with errors, stored in the external storage.
	/// </summary>
	public class OrderFailList : BaseStorageEntityList<OrderFail>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrderFailList"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		public OrderFailList(IStorage storage)
			: base(storage)
		{
		}
	}
}