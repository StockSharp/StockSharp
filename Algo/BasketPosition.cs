#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: BasketPosition.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The basket with positions which belong to <see cref="BasketPortfolio"/>.
	/// </summary>
	public abstract class BasketPosition : Position
	{
		/// <summary>
		/// Positions from which this basket is created.
		/// </summary>
		public abstract IEnumerable<Position> InnerPositions { get; }
	}
}