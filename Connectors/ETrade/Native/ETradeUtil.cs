#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.Native.ETrade
File: ETradeUtil.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.ETrade.Native
{
	using System;
	
	using Ecng.Common;

	using StockSharp.Messages;

	static class ETradeUtil
	{
		public static DateTimeOffset ETradeTimestampToUTC(long timestamp)
		{
			if (timestamp <= 0)
				return TimeHelper.GregorianStart.ApplyTimeZone(TimeZoneInfo.Utc);

			// ETrade sandbox returns 13-digit timestamp (10 digits for second and 3 digits for millisecond)
			return TimeHelper.GregorianStart.AddSeconds(
				timestamp > 2000000000L
				? timestamp / 1000d
				: timestamp).ApplyTimeZone(TimeZoneInfo.Utc);
		}

		public static bool IsOrderInFinalState(Order nativeOrder)
		{
			return !(nativeOrder.orderStatus == "OPEN" || nativeOrder.orderStatus == "CANCEL_REQUESTED");
		}

		public static bool IsOrderInFinalState(BusinessEntities.Order order)
		{
			return order.State == OrderStates.Done || order.State == OrderStates.Failed;
		}

		public static Sides ETradeActionToSide(this string action)
		{
			switch (action)
			{
				case "BUY":
				case "BUY_TO_COVER":
					return Sides.Buy;
				case "SELL":
				case "SELL_SHORT":
					return Sides.Sell;
			}

			throw new ArgumentException("action");
		}

		public static OrderTypes ETradePriceTypeToOrderType(this string priceType)
		{
			switch (priceType)
			{
				case "MARKET": return OrderTypes.Market;
				case "LIMIT":  return OrderTypes.Limit;
				default:       return OrderTypes.Conditional;
			}
		}
	}
}