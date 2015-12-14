#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: MarketDepthColumns.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	/// <summary>
	/// Columns of order book window.
	/// </summary>
	public enum MarketDepthColumns
	{
		/// <summary>
		/// The own amount of bid (+ stop amount to buy).
		/// </summary>
		OwnBuy,

		/// <summary>
		/// The amount of bid.
		/// </summary>
		Buy,

		/// <summary>
		/// Price.
		/// </summary>
		Price,

		/// <summary>
		/// The amount of ask.
		/// </summary>
		Sell,

		/// <summary>
		/// The net amount of ask (+ stop amount to sale).
		/// </summary>
		OwnSell,
	}
}