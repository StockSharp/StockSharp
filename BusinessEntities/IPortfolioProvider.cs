namespace StockSharp.BusinessEntities;

/// <summary>
/// The portfolio provider interface.
/// </summary>
public interface IPortfolioProvider
{
	/// <summary>
	/// To get the portfolio by the code name.
	/// </summary>
	/// <param name="name">Portfolio code name.</param>
	/// <returns>The got portfolio. If there is no portfolio by given criteria, <see langword="null" /> is returned.</returns>
	Portfolio LookupByPortfolioName(string name);

	/// <summary>
	/// Get all portfolios.
	/// </summary>
	IEnumerable<Portfolio> Portfolios { get; }

	/// <summary>
	/// New portfolio received.
	/// </summary>
	event Action<Portfolio> NewPortfolio;

	/// <summary>
	/// Portfolio changed.
	/// </summary>
	event Action<Portfolio> PortfolioChanged;
}