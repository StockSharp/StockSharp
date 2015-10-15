namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The class for the presentation in the form of stocks list, stored in the external storage.
	/// </summary>
	public class ExchangeList : BaseStorageEntityList<Exchange>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ExchangeList"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		public ExchangeList(IStorage storage)
			: base(storage)
		{
		}
	}
}
