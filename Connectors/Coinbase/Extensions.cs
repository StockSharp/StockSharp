namespace StockSharp.Coinbase
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	static class Extensions
	{
		public static string ToNative(this Sides side)
		{
			switch (side)
			{
				case Sides.Buy:
					return "buy";
				case Sides.Sell:
					return "sell";
				default:
					throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue);
			}
		}

		public static Sides ToSide(this string side)
		{
			switch (side)
			{
				case "buy":
					return Sides.Buy;
				case "sell":
					return Sides.Sell;
				default:
					throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue);
			}
		}

		public static string ToNative(this OrderTypes? type)
		{
			switch (type)
			{
				case null:
					return null;
				case OrderTypes.Limit:
					return "limit";
				case OrderTypes.Market:
					return "market";
				case OrderTypes.Conditional:
					return "stop";
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
			}
		}

		public static OrderTypes ToOrderType(this string type)
		{
			switch (type)
			{
				case "limit":
					return OrderTypes.Limit;
				case "market":
					return OrderTypes.Market;
				case "stop":
					return OrderTypes.Conditional;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
			}
		}

		public static OrderStates ToOrderState(this string type)
		{
			switch (type)
			{
				case "pending":
				case "received":
					return OrderStates.Pending;
				case "open":
				case "active":
					return OrderStates.Active;
				case "filled":
				case "done":
				case "canceled":
				case "settled":
					return OrderStates.Done;
				case "rejected":
					return OrderStates.Failed;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
			}
		}

		public static string ToNative(this TimeInForce? tif, DateTimeOffset? tillDate)
		{
			switch (tif)
			{
				case null:
					return null;
				case TimeInForce.PutInQueue:
					return tillDate == null ? "GTC" : "GTT";
				case TimeInForce.CancelBalance:
					return "IOC";
				case TimeInForce.MatchOrCancel:
					return "FOK";
				default:
					throw new ArgumentOutOfRangeException(nameof(tif), tif, LocalizedStrings.InvalidValue);
			}
		}

		public static TimeInForce? ToTimeInForce(this string tif)
		{
			switch (tif)
			{
				case null:
					return null;
				case "GTC":
				case "GTT":
					return TimeInForce.PutInQueue;
				case "IOC":
					return TimeInForce.CancelBalance;
				case "FOK":
					return TimeInForce.MatchOrCancel;
				default:
					throw new ArgumentOutOfRangeException(nameof(tif), tif, LocalizedStrings.InvalidValue);
			}
		}

		public static decimal GetBalance(this Native.Model.Order order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			return (order.Size - order.FilledSize ?? 0).ToDecimal().Value;
		}

		public static string ToCurrency(this SecurityId securityId)
		{
			return securityId.SecurityCode.Replace("/", "-").ToUpperInvariant();
		}

		public static SecurityId ToStockSharp(this string currency)
		{
			return new SecurityId
			{
				SecurityCode = currency.Replace("-", "/").ToUpperInvariant(),
				BoardCode = BoardCodes.Coinbase,
			};
		}
	}
}