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
				return TimeHelper.GregorianStart;

			// ETrade sandbox returns 13-digit timestamp (10 digits for second and 3 digits for millisecond)
			return TimeHelper.GregorianStart.AddSeconds(
				timestamp > 2000000000L
				? timestamp / 1000d
				: timestamp);
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