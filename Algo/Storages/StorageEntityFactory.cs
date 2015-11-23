namespace StockSharp.Algo.Storages
{
	using System;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Storage based entity factory.
	/// </summary>
	public class StorageEntityFactory : EntityFactory
	{
		private readonly IEntityRegistry _entityRegistry;

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageEntityFactory"/>.
		/// </summary>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		public StorageEntityFactory(IEntityRegistry entityRegistry)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException(nameof(entityRegistry));

			_entityRegistry = entityRegistry;
		}

		/// <summary>
		/// To create the instrument by the identifier.
		/// </summary>
		/// <param name="id">Security ID.</param>
		/// <returns>Created instrument.</returns>
		public override Security CreateSecurity(string id)
		{
			return _entityRegistry.Securities.ReadById(id) ?? base.CreateSecurity(id);
		}

		/// <summary>
		/// To create the portfolio by the account number.
		/// </summary>
		/// <param name="name">Account number.</param>
		/// <returns>Created portfolio.</returns>
		public override Portfolio CreatePortfolio(string name)
		{
			return _entityRegistry.Portfolios.ReadById(name) ?? base.CreatePortfolio(name);
		}

		/// <summary>
		/// Create position.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		/// <param name="security">Security.</param>
		/// <returns>Created position.</returns>
		public override Position CreatePosition(Portfolio portfolio, Security security)
		{
			return _entityRegistry.Positions.ReadBySecurityAndPortfolio(security, portfolio)
			       ?? base.CreatePosition(portfolio, security);
		}
	}
}