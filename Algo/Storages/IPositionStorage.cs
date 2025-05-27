namespace StockSharp.Algo.Storages;

using Key = ValueTuple<Portfolio, Security, string, Sides?, string, string, TPlusLimits?>;

/// <summary>
/// The interface for access to the position storage.
/// </summary>
public interface IPositionStorage : IPositionProvider, IPortfolioProvider
{
	/// <summary>
	/// Sync object.
	/// </summary>
	SyncObject SyncRoot { get; }

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

/// <summary>
/// In memory implementation of <see cref="IPositionStorage"/>.
/// </summary>
public class InMemoryPositionStorage : IPositionStorage
{
	private readonly IPortfolioProvider _underlying;

	private readonly CachedSynchronizedDictionary<string, Portfolio> _portfolios = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly CachedSynchronizedDictionary<Key, Position> _positions = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryPositionStorage"/>.
	/// </summary>
	public InMemoryPositionStorage()
		: this(new CollectionPortfolioProvider())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryPositionStorage"/>.
	/// </summary>
	/// <param name="underlying">Underlying provider.</param>
	public InMemoryPositionStorage(IPortfolioProvider underlying)
	{
		_underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
	}

	/// <inheritdoc />
	public IEnumerable<Position> Positions => _positions.CachedValues;

	/// <inheritdoc />
	public IEnumerable<Portfolio> Portfolios => _portfolios.CachedValues;

	/// <inheritdoc />
	public SyncObject SyncRoot => _positions.SyncRoot;

	/// <inheritdoc />
	public event Action<Position> NewPosition;

	/// <inheritdoc />
	public event Action<Position> PositionChanged;

	/// <inheritdoc />
	public event Action<Portfolio> NewPortfolio;

	/// <inheritdoc />
	public event Action<Portfolio> PortfolioChanged;

	/// <inheritdoc />
	public void Delete(Portfolio portfolio)
	{
		if (portfolio is null)
			throw new ArgumentNullException(nameof(portfolio));

		_portfolios.Remove(portfolio.Name);
	}

	/// <inheritdoc />
	public void Delete(Position position)
	{
		_positions.Remove(CreateKey(position));
	}

	/// <inheritdoc />
	public Position GetPosition(Portfolio portfolio, Security security, string strategyId, Sides? side, string clientCode = "", string depoName = "", TPlusLimits? limitType = null)
	{
		return _positions.TryGetValue(CreateKey(portfolio, security, strategyId, side, clientCode, depoName, limitType));
	}

	/// <inheritdoc />
	public Portfolio LookupByPortfolioName(string name)
	{
		return _portfolios.TryGetValue(name) ?? _underlying.LookupByPortfolioName(name);
	}

	/// <inheritdoc />
	public void Save(Portfolio portfolio)
	{
		if (portfolio is null)
			throw new ArgumentNullException(nameof(portfolio));

		var isNew = false;

		lock (_portfolios.SyncRoot)
		{
			if (!_portfolios.ContainsKey(portfolio.Name))
			{
				isNew = true;
				_portfolios.Add(portfolio.Name, portfolio);
			}
		}

		(isNew ? NewPortfolio : PortfolioChanged)?.Invoke(portfolio);
	}

	/// <inheritdoc />
	public void Save(Position position)
	{
		if (position is null)
			throw new ArgumentNullException(nameof(position));

		var key = CreateKey(position);
		var isNew = false;

		lock (_positions.SyncRoot)
		{
			if (!_positions.ContainsKey(key))
			{
				isNew = true;
				_positions.Add(key, position);
			}
		}

		(isNew ? NewPosition : PositionChanged)?.Invoke(position);
	}

	private static Key CreateKey(Position position)
	{
		if (position is null)
			throw new ArgumentNullException(nameof(position));

		return CreateKey(position.Portfolio, position.Security, position.StrategyId, position.Side, position.ClientCode, position.DepoName, position.LimitType);
	}

	private static Key CreateKey(Portfolio portfolio, Security security, string strategyId, Sides? side, string clientCode, string depoName, TPlusLimits? limitType)
	{
		if (portfolio is null)
			throw new ArgumentNullException(nameof(portfolio));

		if (security is null)
			throw new ArgumentNullException(nameof(security));

		depoName ??= string.Empty;

		clientCode ??= string.Empty;

		return (portfolio, security, strategyId?.ToLowerInvariant() ?? string.Empty, side, clientCode?.ToLowerInvariant() ?? string.Empty, depoName?.ToLowerInvariant() ?? string.Empty, limitType);
	}
}