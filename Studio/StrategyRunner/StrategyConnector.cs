namespace StockSharp.Studio.StrategyRunner
{
	using System.Collections.Generic;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	class StrategyConnector : Connector
	{
		private readonly StrategyEntityFactory _entityFactory;

		public ISecurityProvider SecurityProvider
		{
			get { return _entityFactory.SecurityProvider; }
		}

		public StrategyConnector()
		{
			EntityFactory = _entityFactory = new StrategyEntityFactory();
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Найденные инструменты.</returns>
		public override IEnumerable<Security> Lookup(Security criteria)
		{
			return _entityFactory.Lookup(criteria);
		}

		public Portfolio LookupPortfolio(string name)
		{
			return _entityFactory.LookupPortfolio(name);
		}
	}
}
