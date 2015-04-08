namespace StockSharp.Studio.StrategyRunner
{
	using System.Collections.Generic;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	class StrategyConnector : Connector
	{
		private readonly StrategyEntityFactory _entityFactory;

		public ISecurityList SecurityList { get { return _entityFactory.Securities; } }

		public StrategyConnector()
		{
			EntityFactory = _entityFactory = new StrategyEntityFactory();

			TransactionAdapter = new BasketMessageAdapter(TransactionIdGenerator).ToChannel(this);
			MarketDataAdapter = new BasketMessageAdapter(TransactionIdGenerator).ToChannel(this);
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
