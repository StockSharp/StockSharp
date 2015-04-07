namespace StockSharp.Studio.StrategyRunner
{
	using System.Collections.Generic;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class StrategyConnector : Connector
	{
		private readonly StrategyEntityFactory _entityFactory;

		public BasketSessionHolder BasketSessionHolder { get; private set; }

		public ISecurityList SecurityList { get { return _entityFactory.Securities; } }

		public StrategyConnector()
		{
			EntityFactory = _entityFactory = new StrategyEntityFactory();

			SessionHolder = BasketSessionHolder = new BasketSessionHolder(TransactionIdGenerator);

			TransactionAdapter = new BasketMessageAdapter(BasketSessionHolder);
			MarketDataAdapter = new BasketMessageAdapter(BasketSessionHolder);

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
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
