namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The class for representation in the form of list of news, stored in the external storage.
	/// </summary>
	public class NewsList : BaseStorageEntityList<News>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NewsList"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		public NewsList(IStorage storage)
			: base(storage)
		{
		}
	}
}