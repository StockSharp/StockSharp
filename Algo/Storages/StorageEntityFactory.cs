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
		private readonly ISecurityProvider _securityProvider;
		private readonly IPositionProvider _positionStorage;
		private readonly IPortfolioProvider _portfolioProvider;
		private readonly bool _trackPositions;

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageEntityFactory"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="positionStorage">Position storage.</param>
		/// <param name="trackPositions">Track positions.</param>
		public StorageEntityFactory(ISecurityProvider securityProvider, IPositionProvider positionStorage, bool trackPositions)
			: this(securityProvider, positionStorage)
		{
			_positionStorage = positionStorage ?? throw new ArgumentNullException(nameof(positionStorage));
			_trackPositions = trackPositions;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageEntityFactory"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		public StorageEntityFactory(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider)
		{
			_securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
			_portfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
		}

		/// <inheritdoc />
		public override Security CreateSecurity(string id)
		{
			return _securityProvider.LookupById(id) ?? base.CreateSecurity(id);
		}

		/// <inheritdoc />
		public override Portfolio CreatePortfolio(string name)
		{
			return _portfolioProvider.LookupByPortfolioName(name) ?? base.CreatePortfolio(name);
		}

		/// <inheritdoc />
		public override Position CreatePosition(Portfolio portfolio, Security security)
		{
			if (_trackPositions)
			{
				var position = _positionStorage.GetPosition(portfolio, security);

				if (position != null)
					return position;
			}
			
			return base.CreatePosition(portfolio, security);
		}
	}
}