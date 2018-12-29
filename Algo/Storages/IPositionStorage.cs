namespace StockSharp.Algo.Storages
{
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for access to the position storage.
	/// </summary>
	public interface IPositionStorage : IPositionProvider, IPortfolioProvider
	{
		/// <summary>
		/// Save portfolio.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		void Save(Portfolio portfolio);

		/// <summary>
		/// Delete portfolio.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		void Delete(Portfolio portfolio);

		/// <summary>
		/// Save position.
		/// </summary>
		/// <param name="position">Position.</param>
		void Save(Position position);

		/// <summary>
		/// Delete position.
		/// </summary>
		/// <param name="position">Position.</param>
		void Delete(Position position);
	}
}