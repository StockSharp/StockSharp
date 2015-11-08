namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The portfolio provider interface.
	/// </summary>
	public interface IPortfolioProvider
	{
		/// <summary>
		/// Get all portfolios.
		/// </summary>
		IEnumerable<Portfolio> Portfolios { get; }

		/// <summary>
		/// New portfolio received.
		/// </summary>
		event Action<Portfolio> NewPortfolio;
	}
}