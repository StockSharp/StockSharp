namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Collection based implementation of <see cref="IPortfolioProvider"/>.
	/// </summary>
	public class CollectionPortfolioProvider : CachedSynchronizedDictionary<string, Portfolio>, IPortfolioProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionPortfolioProvider"/>.
		/// </summary>
		public CollectionPortfolioProvider()
			: this(Enumerable.Empty<Portfolio>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionPortfolioProvider"/>.
		/// </summary>
		/// <param name="portfolios">The portfolios collection.</param>
		public CollectionPortfolioProvider(IEnumerable<Portfolio> portfolios)
			: base(StringComparer.InvariantCultureIgnoreCase)
		{
			if (portfolios == null)
				throw new ArgumentNullException(nameof(portfolios));

			foreach (var portfolio in portfolios)
			{
				Add(portfolio.Name, portfolio);
			}

			//Added += p => NewPortfolio?.Invoke(p);
			//Removed += p => PortfolioChanged?.Invoke(p);

			if (portfolios is INotifyList<Portfolio> notifyList)
			{
				notifyList.Added += pf => Add(pf.Name, pf);;
				notifyList.Removed += pf => Remove(pf.Name);
				notifyList.Cleared += Clear;
			}
		}

		/// <inheritdoc />
		public Portfolio GetPortfolio(string name)
		{
			return this.TryGetValue(name);
		}

		/// <inheritdoc />
		public IEnumerable<Portfolio> Portfolios => CachedValues;

		/// <inheritdoc />
		public event Action<Portfolio> NewPortfolio;

		/// <inheritdoc />
		public event Action<Portfolio> PortfolioChanged;
	}
}