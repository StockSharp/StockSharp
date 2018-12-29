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
		private readonly ISecurityStorage _securityStorage;
		private readonly IPositionStorage _positionStorage;
		private readonly bool _trackPositions;

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageEntityFactory"/>.
		/// </summary>
		/// <param name="securityStorage">Securities meta info storage.</param>
		/// <param name="positionStorage">Position storage.</param>
		/// <param name="trackPositions">Track positions.</param>
		public StorageEntityFactory(ISecurityStorage securityStorage, IPositionStorage positionStorage, bool trackPositions)
		{
			_securityStorage = securityStorage ?? throw new ArgumentNullException(nameof(securityStorage));
			_positionStorage = positionStorage ?? throw new ArgumentNullException(nameof(positionStorage));
			_trackPositions = trackPositions;
		}

		/// <inheritdoc />
		public override Security CreateSecurity(string id)
		{
			return _securityStorage.LookupById(id) ?? base.CreateSecurity(id);
		}

		/// <inheritdoc />
		public override Portfolio CreatePortfolio(string name)
		{
			return _positionStorage.GetPortfolio(name) ?? base.CreatePortfolio(name);
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