namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The class for representation in the form of list of portfolios, stored in external storage.
	/// </summary>
	public class PortfolioList : BaseStorageEntityList<Portfolio>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioList"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		public PortfolioList(IStorage storage)
			: base(storage)
		{
		}
	}
}