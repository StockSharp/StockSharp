namespace StockSharp.Btce
{
	using System;

	using StockSharp.Messages;

	using StockSharp.Localization;

	static class Extensions
	{
		public static string ToBtce(this Sides side)
		{
			switch (side)
			{
				case Sides.Buy:
					return "buy";
				case Sides.Sell:
					return "sell";
				default:
					throw new ArgumentOutOfRangeException("side");
			}
		}

		public static Sides ToStockSharp(this string direction)
		{
			switch (direction)
			{
				case "sell":
				case "ask":
					return Sides.Sell;
				case "buy":
				case "bid":
					return Sides.Buy;
				default:
					throw new ArgumentOutOfRangeException("direction");
			}
		}

		public static QuoteChange ToStockSharp(this double[] vp, Sides side)
		{
			return new QuoteChange(side, (decimal)vp[0], (decimal)vp[1]);
		}

		public static OrderStates ToOrderState(this int status)
		{
			switch (status)
			{
				case 0:
					return OrderStates.Active;
				case 1:
					return OrderStates.Done;
				default:
					throw new ArgumentOutOfRangeException("status", status, LocalizedStrings.Str1598);
			}
		}

		//public static DateTime ToTime(this long timeStamp)
		//{
		//	return Converter.GregorianStart + TimeSpan.FromMilliseconds(timeStamp);
		//}

		public static string ToStockSharpCode(this string btceCode)
		{
			return btceCode.Replace('_', '/').ToUpperInvariant();
		}

		public static string ToBtceCode(this string stockSharpCode)
		{
			return stockSharpCode.Replace('/', '_').ToLowerInvariant();
		}
	}
}