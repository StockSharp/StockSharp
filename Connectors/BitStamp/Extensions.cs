#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.BitStamp
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp
{
	using StockSharp.Messages;

	static class Extensions
	{
		public static QuoteChange ToStockSharp(this double[] vp, Sides side)
		{
			return new QuoteChange(side, (decimal)vp[0], (decimal)vp[1]);
		}

		public static Sides ToStockSharp(this int type)
		{
			return type == 0 ? Sides.Buy : Sides.Sell;
		}
	}
}