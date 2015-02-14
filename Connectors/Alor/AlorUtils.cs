using System;

using Ecng.Common;

namespace StockSharp.Alor
{
	using StockSharp.Messages;
	using StockSharp.Localization;

	static class AlorUtils
	{
		public static OrderTypes ToOrderType(this string value)
		{
			if (value.IsEmpty())
				throw new ArgumentNullException("value");
			if (value == "M")
				return OrderTypes.Market;
			if (value == "L")
				return OrderTypes.Limit;
			throw new ArgumentException(LocalizedStrings.Str3706Params.Put(value), "value");
		}

		public static OrderDirections ToOrderDirection(this string value)
		{
			if (value.IsEmpty())
				throw new ArgumentNullException("value");

			switch (value)
			{
				case "B":
					return OrderDirections.Buy;
				case "S":
					return OrderDirections.Sell;
				default:
					throw new ArgumentException(LocalizedStrings.Str3707Params.Put(value), "value");
			}
		}

		public static TimeInForce ToOrderExecutionCondition(this string value)
		{
			if (value.IsEmpty())
				throw new ArgumentNullException("value");

			switch (value)
			{
				case " ":
					return TimeInForce.PutInQueue;
				case "N":
					return TimeInForce.MatchOrCancel;
				case "W":
					return TimeInForce.CancelBalance;
				default:
					throw new ArgumentException(LocalizedStrings.Str3706Params.Put(value), "value");
			}
		}

		public static OrderStates ToOrderState(this string value)
		{
			if (value.IsEmpty())
				throw new ArgumentNullException("value");

			switch (value)
			{
				case "O":
					return OrderStates.Active;
				case "W":
				case "E":
				case "M":
					return OrderStates.Done;
				case "R":
				case "N":
					return OrderStates.Failed;
				default:
					return OrderStates.None;
			}
		}

		public static OrderStatus ToOrderStatus(this string value)
		{
			if (value.IsEmpty())
				throw new ArgumentNullException("value");

			switch (value)
			{
				case "A":
					return OrderStatus.ReceiveByServer;
				case "N":
					return OrderStatus.CanceledByManager;
				case "C":
				case "R":
					return OrderStatus.NotDone;
				default:
					return OrderStatus.Accepted;
			}
		}

		//public static ExchangeBoard ToExchangeBoard(this string value)
		//{
		//	if (value.IsEmpty())
		//		throw new ArgumentNullException("value");

		//	switch (value)
		//	{
		//		case "RTS":
		//		case "SPBEX_FUT":
		//		case "SPBEX_OPT":
		//			return ExchangeBoard.Forts;
		//		case "MICEX":
		//		case "MICEX_FUT":
		//			return ExchangeBoard.Micex;
		//		case "TEST":
		//		case "TEST_FUT":
		//			return ExchangeBoard.Test;
		//		default:
		//			throw new ArgumentException("Тип биржи {0} не известен.".Put(value), "value");
		//	}
		//}

		public static AlorOrderConditionTypes ToOrderConditionType(this string value)
		{
			if (value.IsEmpty())
				throw new ArgumentNullException("value");

			switch (value)
			{
				case "L":
					return AlorOrderConditionTypes.StopLoss;
				case "P":
					return AlorOrderConditionTypes.TakeProfit;
				case " ":
					return AlorOrderConditionTypes.Inactive;
				default:
					throw new ArgumentException(LocalizedStrings.Str3708Params.Put(value), "value");
			}
		}

		public static string ToAlorConditionType(this AlorOrderConditionTypes value)
		{
			switch (value)
			{
				case AlorOrderConditionTypes.StopLoss:
					return "L";
				case AlorOrderConditionTypes.TakeProfit:
					return "P";
				case AlorOrderConditionTypes.Inactive:
					return " ";
				default:
					throw new ArgumentException(LocalizedStrings.Str3708Params.Put(value), "value");
			}
		}
	}
}