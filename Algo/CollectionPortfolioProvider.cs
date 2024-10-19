namespace StockSharp.Algo;

/// <summary>
/// Collection based implementation of <see cref="IPortfolioProvider"/>.
/// </summary>
public class CollectionPortfolioProvider : IPortfolioProvider
{
	private readonly CachedSynchronizedDictionary<string, Portfolio> _inner = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the <see cref="CollectionPortfolioProvider"/>.
	/// </summary>
	public CollectionPortfolioProvider()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CollectionPortfolioProvider"/>.
	/// </summary>
	/// <param name="portfolios">The portfolios collection.</param>
	public CollectionPortfolioProvider(IEnumerable<Portfolio> portfolios)
	{
		if (portfolios is null)
			throw new ArgumentNullException(nameof(portfolios));

		foreach (var portfolio in portfolios)
			Add(portfolio);
	}

	/// <inheritdoc />
	public Portfolio LookupByPortfolioName(string name)
	{
		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		return _inner.TryGetValue(name);
	}

	/// <inheritdoc />
	public IEnumerable<Portfolio> Portfolios => _inner.CachedValues;

	/// <inheritdoc />
	public event Action<Portfolio> NewPortfolio;

	/// <inheritdoc />
	public event Action<Portfolio> PortfolioChanged;

	/// <summary>
	/// Add security.
	/// </summary>
	/// <param name="portfolio">Portfolio.</param>
	public void Add(Portfolio portfolio)
	{
		if (portfolio is null)
			throw new ArgumentNullException(nameof(portfolio));

		_inner.Add(portfolio.Name, portfolio);
		NewPortfolio?.Invoke(portfolio);
	}

	/// <summary>
	/// Remove security.
	/// </summary>
	/// <param name="portfolio">Portfolio.</param>
	/// <returns>Check result.</returns>
	public bool Remove(Portfolio portfolio)
	{
		if (portfolio is null)
			throw new ArgumentNullException(nameof(portfolio));

		if (!_inner.Remove(portfolio.Name))
			return false;

		PortfolioChanged?.Invoke(portfolio);
		return true;
	}
}