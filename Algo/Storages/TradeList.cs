namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The class for representation of tick trades, stored in external storage, in the form of list.
	/// </summary>
	public class TradeList : BaseStorageEntityList<Trade>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TradeList"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		public TradeList(IStorage storage)
			: base(storage)
		{
		}
	}
}