namespace StockSharp.Algo.Storages;

class PositionStorage(IEntityRegistry entityRegistry) : IPositionStorage
{
	private readonly IEntityRegistry _entityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));

	IEnumerable<Position> IPositionProvider.Positions => _entityRegistry.Positions.Cache;

	event Action<Position> IPositionProvider.NewPosition
	{
		add => throw new NotSupportedException();
		remove => throw new NotSupportedException();
	}

	event Action<Position> IPositionProvider.PositionChanged
	{
		add => throw new NotSupportedException();
		remove => throw new NotSupportedException();
	}

	Portfolio IPortfolioProvider.LookupByPortfolioName(string portfolioName)
	{
		if (portfolioName.IsEmpty())
			throw new ArgumentNullException(nameof(portfolioName));

		return _entityRegistry.Portfolios.ReadById(portfolioName);
	}

	IEnumerable<Portfolio> IPortfolioProvider.Portfolios => _entityRegistry.Portfolios.Cache;

	SyncObject IPositionStorage.SyncRoot => _entityRegistry.Portfolios.SyncRoot;

	event Action<Portfolio> IPortfolioProvider.NewPortfolio
	{
		add => throw new NotSupportedException();
		remove => throw new NotSupportedException();
	}

	event Action<Portfolio> IPortfolioProvider.PortfolioChanged
	{
		add => throw new NotSupportedException();
		remove => throw new NotSupportedException();
	}

	void IPositionStorage.Save(Portfolio portfolio) => _entityRegistry.Portfolios.Save(portfolio);
	void IPositionStorage.Delete(Portfolio portfolio) => _entityRegistry.Portfolios.Remove(portfolio);

	void IPositionStorage.Save(Position position) => _entityRegistry.Positions.Save(position);
	void IPositionStorage.Delete(Position position) => _entityRegistry.Positions.Remove(position);

	Position IPositionProvider.GetPosition(Portfolio portfolio, Security security, string strategyId, Sides? side, string clientCode, string depoName, TPlusLimits? limit)
		=> _entityRegistry.Positions.GetPosition(portfolio, security, strategyId, side, clientCode, depoName, limit);
}