#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: IPortfolioProvider.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The portfolio provider interface.
	/// </summary>
	public interface IPortfolioProvider
	{
		/// <summary>
		/// Get all portfolios.
		/// </summary>
		IEnumerable<Portfolio> Portfolios { get; }

		/// <summary>
		/// New portfolio received.
		/// </summary>
		event Action<Portfolio> NewPortfolio;

		/// <summary>
		/// Portfolio changed.
		/// </summary>
		event Action<Portfolio> PortfolioChanged;
	}
}