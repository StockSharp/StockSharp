namespace StockSharp.Sterling
{
	using System;

	using SterlingLib;

	using StockSharp.Messages;
	using StockSharp.Localization;

	static class Extensions
	{
		public static OrderStates ToStockSharp(this STIOrderStatus status)
		{
			switch (status)
			{
				case STIOrderStatus.osSTIUnknown:
					throw new NotSupportedException();
				case STIOrderStatus.osSTIPendingCancel:
					return OrderStates.Active;
				case STIOrderStatus.osSTIPendingReplace:
					return OrderStates.Active;
				case STIOrderStatus.osSTIDoneForDay:
					return OrderStates.Done;
				case STIOrderStatus.osSTICalculated:
					return OrderStates.Active;
				case STIOrderStatus.osSTIFilled:
					return OrderStates.Done;
				case STIOrderStatus.osSTIStopped:
					return OrderStates.Done;
				case STIOrderStatus.osSTISuspended:
					return OrderStates.Active;
				case STIOrderStatus.osSTICanceled:
					return OrderStates.Done;
				case STIOrderStatus.osSTIExpired:
					return OrderStates.Done;
				case STIOrderStatus.osSTIPartiallyFilled:
					return OrderStates.Active;
				case STIOrderStatus.osSTIReplaced:
					return OrderStates.Done;
				case STIOrderStatus.osSTIRejected:
					return OrderStates.Failed;
				case STIOrderStatus.osSTINew:
					return OrderStates.Active;
				case STIOrderStatus.osSTIPendingNew:
					return OrderStates.Pending;
				case STIOrderStatus.osSTIAcceptedForBidding:
					return OrderStates.Active;
				case STIOrderStatus.osSTIAdjusted:
					return OrderStates.Active;
				case STIOrderStatus.osSTIStatused:
					return OrderStates.Active;
				default:
					throw new ArgumentOutOfRangeException("status");
			}
		}

		public static Sides ToSide(string side)
		{
			switch (side)
			{
				case "B":	// BUY
				case "C":	// BUY TO COVER
				case "M":	// BUY-
					return Sides.Buy;

				case "S":	// SELL
				case "T":	// SSHRT
				case "P":	// SELL+
				case "E":	// SSHRTEX
					return Sides.Buy;

				default:
					throw new ArgumentOutOfRangeException("side", side, LocalizedStrings.Str3802);
			}
		}

		public static string ToSterlingTif(this TimeInForce tif, DateTimeOffset expiryDate)
		{
			switch (tif)
			{
				case TimeInForce.PutInQueue:
				{
					if (expiryDate == DateTimeOffset.MaxValue)
						return "G";	// GTC
					else if (expiryDate == DateTime.Today)
						return "D";	// DAY
					else if (expiryDate.TimeOfDay == TimeSpan.Zero)
						return "O"; // OPG
					else
						return "N";	// NOW
				}
				case TimeInForce.MatchOrCancel:
					return "F";	// FOK
				case TimeInForce.CancelBalance:
					return "I";	// IOC
				default:
					throw new ArgumentOutOfRangeException("tif");
			}
		}

		public static STIPriceTypes ToSterlingPriceType(this OrderTypes type, SterlingOrderCondition condition)
		{
			switch (type)
			{
				case OrderTypes.Limit:
					return STIPriceTypes.ptSTILmt;
				case OrderTypes.Market:
					return STIPriceTypes.ptSTIMkt;
				case OrderTypes.Conditional:
				{
					if (condition == null)
						throw new ArgumentNullException("condition");

					switch (condition.ExtendedOrderType)
					{
						case SterlingExtendedOrderTypes.MarketOnClose:
							return STIPriceTypes.ptSTIMktClo;
						case SterlingExtendedOrderTypes.MarketOrBetter:
							return STIPriceTypes.ptSTIMktOb;
						case SterlingExtendedOrderTypes.MarketNoWait:
							return STIPriceTypes.ptSTIMktWow;
						case SterlingExtendedOrderTypes.LimitOnClose:
							return STIPriceTypes.ptSTILmtClo;
						case SterlingExtendedOrderTypes.Stop:
							return STIPriceTypes.ptSTILmtStp;
						case SterlingExtendedOrderTypes.StopLimit:
							return STIPriceTypes.ptSTILmtStpLmt;
						case SterlingExtendedOrderTypes.LimitOrBetter:
							return STIPriceTypes.ptSTILmtOb;
						case SterlingExtendedOrderTypes.LimitNoWait:
							return STIPriceTypes.ptSTILmtWow;
						case SterlingExtendedOrderTypes.Nyse:
							return STIPriceTypes.ptSTIBas;
						case SterlingExtendedOrderTypes.Close:
							return STIPriceTypes.ptSTIClo;
						case SterlingExtendedOrderTypes.Pegged:
							return STIPriceTypes.ptSTIPegged;
						case SterlingExtendedOrderTypes.ServerStop:
							return STIPriceTypes.ptSTISvrStp;
						case SterlingExtendedOrderTypes.ServerStopLimit:
							return STIPriceTypes.ptSTISvrStpLmt;
						case SterlingExtendedOrderTypes.TrailingStop:
							return STIPriceTypes.ptSTITrailStp;
						case SterlingExtendedOrderTypes.NoWait:
							return STIPriceTypes.ptSTIWow;
						case SterlingExtendedOrderTypes.Last:
							return STIPriceTypes.ptSTILast;
						case null:
							throw new ArgumentException(LocalizedStrings.Str3803, "condition");
						default:
							throw new ArgumentOutOfRangeException("condition", condition.ExtendedOrderType, LocalizedStrings.Str2500);
					}
				}
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}
	}
}