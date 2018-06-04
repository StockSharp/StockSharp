#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: StorageEntityFactory.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
			_entityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));
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