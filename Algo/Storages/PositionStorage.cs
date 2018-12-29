namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	class PositionStorage : IPositionStorage
	{
		private readonly IEntityRegistry _entityRegistry;

		public PositionStorage(IEntityRegistry entityRegistry)
		{
			_entityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));
		}

		IEnumerable<Position> IPositionProvider.Positions => _entityRegistry.Positions;

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

		Portfolio IPortfolioProvider.GetPortfolio(string portfolioName)
		{
			return _entityRegistry.Portfolios.ReadById(portfolioName);
		}

		IEnumerable<Portfolio> IPortfolioProvider.Portfolios => _entityRegistry.Portfolios;

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

		void IPositionStorage.Save(Portfolio portfolio)
		{
			_entityRegistry.Portfolios.Save(portfolio);
		}

		void IPositionStorage.Delete(Portfolio portfolio)
		{
			_entityRegistry.Portfolios.Remove(portfolio);
		}

		void IPositionStorage.Save(Position position)
		{
			_entityRegistry.Positions.Save(position);
		}

		void IPositionStorage.Delete(Position position)
		{
			_entityRegistry.Positions.Remove(position);
		}

		Position IPositionProvider.GetPosition(Portfolio portfolio, Security security, string clientCode, string depoName)
		{
			return _entityRegistry.Positions.GetPosition(portfolio, security, clientCode, depoName);
		}
	}
}