namespace StockSharp.SmartCom.Native
{
	using System;

	using StockSharp.Messages;

	using StockSharp.Localization;

	static class SmartComHelper
	{
		public static bool IsReject(this SmartOrderState state)
		{
			switch (state)
			{
				case SmartOrderState.ContragentReject:
				case SmartOrderState.ContragentCancel:
				case SmartOrderState.SystemReject:
				case SmartOrderState.SystemCancel:
					return true;
				default:
					return false;
			}
		}

		public static Sides? ToSide(this SmartOrderAction action)
		{
			switch (action)
			{
				case SmartOrderAction.Buy:
					return Sides.Buy;
				case SmartOrderAction.Sell:
					return Sides.Sell;
				case SmartOrderAction.Short:
					return Sides.Sell;
				case SmartOrderAction.Cover:
					return Sides.Buy;
				case 0:
					return null;
				default:
					throw new ArgumentOutOfRangeException("action", action, LocalizedStrings.Str1882);
			}
		}

		public static SmartOrderType GetSmartOrderType(this OrderRegisterMessage order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			switch (order.OrderType)
			{
				case OrderTypes.Limit:
					return SmartOrderType.Limit;
				case OrderTypes.Market:
					return SmartOrderType.Market;
				case OrderTypes.Conditional:
					return order.Price != 0 ? SmartOrderType.StopLimit : SmartOrderType.Stop;
				default:
					throw new ArgumentOutOfRangeException("order");
			}
		}

		public static OrderTypes ToOrderType(this SmartOrderType smartType)
		{
			switch (smartType)
			{
				case SmartOrderType.Market:
					return OrderTypes.Market;
				case SmartOrderType.Limit:
					return OrderTypes.Limit;
				case SmartOrderType.Stop:
				case SmartOrderType.StopLimit:
					return OrderTypes.Conditional;
				default:
					throw new ArgumentOutOfRangeException("smartType", smartType, LocalizedStrings.Str1883);
			}
		}

		//public static SmartOrderAction GetSmartAction(this SmartTrader trader, Order order)
		//{
		//	if (trader == null)
		//		throw new ArgumentNullException("trader");

		//	if (order == null)
		//		throw new ArgumentNullException("order");

		//	if (order.Security.Board.IsMicex)
		//	{
		//		var position = trader.GetPosition(order.Portfolio, order.Security);

		//		if (position == null)
		//			return order.Direction == OrderDirections.Buy ? SmartOrderAction.Buy : SmartOrderAction.Short;

		//		if (order.Direction == OrderDirections.Buy)
		//		{
		//			return position.CurrentValue >= 0 ? SmartOrderAction.Buy : SmartOrderAction.Cover;
		//		}
		//		else
		//		{
		//			return (position.CurrentValue - order.Volume) >= 0 ? SmartOrderAction.Sell : SmartOrderAction.Short;
		//		}
		//	}
		//	else
		//		return order.Direction == OrderDirections.Buy ? SmartOrderAction.Buy : SmartOrderAction.Sell;
		//}
	}
}