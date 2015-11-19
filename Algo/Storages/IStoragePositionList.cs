namespace StockSharp.Algo.Storages
{
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for access to the position storage.
	/// </summary>
	public interface IStoragePositionList : IStorageEntityList<Position>
	{
		/// <summary>
		/// To load the position.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="portfolio">Portfolio.</param>
		/// <returns>Position.</returns>
		Position ReadBySecurityAndPortfolio(Security security, Portfolio portfolio);
	}
}