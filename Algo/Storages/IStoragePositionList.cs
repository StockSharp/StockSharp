#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: IStoragePositionList.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for access to the position storage.
	/// </summary>
	public interface IStoragePositionList : IStorageEntityList<Position>
	{
		/// <summary>
		/// To load the position.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="portfolio">Portfolio.</param>
		/// <returns>Position.</returns>
		Position ReadBySecurityAndPortfolio(Security security, Portfolio portfolio);
	}
}