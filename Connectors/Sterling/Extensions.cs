using System;
using SterlingLib;
using StockSharp.Messages;
using Ecng.Common;
using StockSharp.Localization;

namespace StockSharp.Sterling
{
	static class Extensions
	{
		public static OrderStates ToOrderStates(this STIOrderStatus status)
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

		public static Sides ToSide(this string side)
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
					return Sides.Sell;

				default:
					throw new ArgumentOutOfRangeException("side", side, LocalizedStrings.Str3802);
			}
		}

		public static string ToSterlingSide(this Sides side)
		{
			return side == Sides.Buy ? "B" : "S";
		}

		public static TimeInForce ToTif(this string tif)
		{
			switch (tif)
			{
				case "G":
				case "D":
				case "O":
				case "N":
					return TimeInForce.PutInQueue;

				case "F":
					return TimeInForce.MatchOrCancel;

				case "I":
					return TimeInForce.CancelBalance;

				default:
					throw new ArgumentOutOfRangeException(LocalizedStrings.Str1599);
			}
		}

		public static string ToSterlingTif(this TimeInForce tif, DateTimeOffset expiryDate)
		{
			switch (tif)
			{
				case TimeInForce.PutInQueue:
				{
					if (expiryDate == DateTime.MaxValue)
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

		public static OrderTypes ToPriceTypes(this STIPriceTypes priceType)
		{
			switch (priceType)
			{
				case STIPriceTypes.ptSTIMkt:
					return OrderTypes.Market;
				case STIPriceTypes.ptSTILmt:
					return OrderTypes.Limit;
				case STIPriceTypes.ptSTIMktClo:
				case STIPriceTypes.ptSTIMktOb:
				case STIPriceTypes.ptSTIMktWow:
				case STIPriceTypes.ptSTILmtClo:
				case STIPriceTypes.ptSTILmtStp:
				case STIPriceTypes.ptSTILmtStpLmt:
				case STIPriceTypes.ptSTILmtOb:
				case STIPriceTypes.ptSTIWow:
				case STIPriceTypes.ptSTILmtWow:
				case STIPriceTypes.ptSTIBas:
				case STIPriceTypes.ptSTIClo:
				case STIPriceTypes.ptSTIPegged:
				case STIPriceTypes.ptSTISvrStp:
				case STIPriceTypes.ptSTISvrStpLmt:
				case STIPriceTypes.ptSTITrailStp:
					return OrderTypes.Conditional;
				default:
					throw new ArgumentOutOfRangeException("priceType");
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

		public static SecurityTypes? ToSecurityType(this string value)
		{
			switch (value)
			{
				case "B":
				case "Non-B":
					return SecurityTypes.Stock; // ???
				case "E":
					return SecurityTypes.Stock;
				case "O":
					return SecurityTypes.Option;
				case "F":
					return SecurityTypes.Future;
				case "X":
					return SecurityTypes.Currency;
				default:
					throw new ArgumentException("{0} {1}.".Put(LocalizedStrings.Str1603, value));
			}
			//switch (value)
			//{
			//	case "EQUITY":
			//	case "SPOT":
			//	case "EQTYDEPTH":
			//		return SecurityTypes.Stock;
			//	case "FUTURE":
			//	case "SSFUTURE":
			//	case "FUTDEPTH":
			//		return SecurityTypes.Future;
			//	case "FORWARD":
			//		return SecurityTypes.Forward;
			//	case "IEOPTION": // Index/Equity Option
			//	case "FOPTION": // Future Option
			//		return SecurityTypes.Option;
			//	case "BONDS":
			//	case "TREASURIES":
			//		return SecurityTypes.Bond;
			//	case "INDEX":
			//	case "SPREAD": // Future Spread
			//	case "STRATSPREAD": // Strategy Spread
			//	case "ICSPREAD": // Inter-Commodity Future Spread
			//	case "MKTSTATS": // Market Statistic
			//	case "MKTRPT": // Market Reports
			//	case "CALC": // DTN Calculated Statistic
			//	case "STRIP":
			//	case "SPRDEPTH":
			//		return SecurityTypes.Index;
			//	case "MONEY": // Money Market Fund
			//	case "MUTUAL": // Mutual Fund
			//		return SecurityTypes.Fund;
			//	case "FOREX": // Foreign Monetary Exchange
			//		return SecurityTypes.Currency;
			//	case "PRECMTL": // Precious Metals
			//	case "ARGUS": // Argus Energy
			//	case "RACKS": // Racks Energy
			//	case "RFSPOT": // Refined Fuel Spot
			//	case "SNL_NG": // SNL Natural Gas
			//	case "SNL_ELEC": // SNL Electricity
			//	case "NP_FLOW": // Nord Pool-N2EX Flow
			//	case "NP_POWER": // Nord Pool-N2EX Power Prices
			//	case "NP_CAPACITY": // Nord Pool-N2EX Capacity
			//		return SecurityTypes.Commodity;
			//	case "SWAPS": // Interest Rate Swap
			//		return SecurityTypes.Swap;
			//	default:
			//		return null;
			//	//throw new ArgumentException("Неизвестный тип инструмента '{0}'.".Put(value));
			//}
		}

		public static DateTime StrToDateTime(this string str)
		{
			var parsedDate = new DateTime
			(
				int.Parse(str.Substring(0, 4)),
				int.Parse(str.Substring(4, 2)),
				int.Parse(str.Substring(6, 2)),
				int.Parse(str.Substring(8, 2)),
				int.Parse(str.Substring(10, 2)),
				int.Parse(str.Substring(12, 2))
			);

			return parsedDate;
		}

		public static DateTime StrToTime(this string str)
		{
			var parsedDate = new DateTime
			(
				DateTime.Now.Year,
				DateTime.Now.Month,
				DateTime.Now.Day,
				int.Parse(str.Substring(0, 2)),
				int.Parse(str.Substring(2, 2)),
				int.Parse(str.Substring(4, 2))
			);

			return parsedDate;
		}

		public static string ToBoard(this string exch)
		{
			switch (exch)
			{
				case "A":
					return "AMEX";// AMEX
				case "B":
					return "NASDAQ";// NASDAQ OMX BX
				case "C":
					return "NSE";// National Stock Exchange
				case "D":
					return "ADFN";// ADFN (FINRA)
				//case "E": // Market Independent (Generated by SIP)
				case "I":
					return "ISE"; // ISE (Alpha Quote Feed)
				case "J":
					return "EDGA";// EDGA Exchange, Inc
				case "K":
					return "EDGX";// EDGX Exchange, Inc
				case "M":
					return "CHX";// Chicago Stock Exchange (Midwest)
				case "N":
					return "NYSE";// New York Stock Exchange
				case "P":
					return "ARCA";// ARCA (NYSE ARCA)
				case "Q":
					return "NASDAQ";// NASDAQ (NASDAQ listed symbols)
				case "T":
					return "NASDAQ";// NASDAQ (Small Cap, Bulletin Board, and OTC)
				case "U": 
					return "OTC-BB";// OTC-BB
				case "W":
					return "CBSX";// CBOE Stock Exchange (CBSX)
				case "X":
					return "PHLX";// NASDAQ OMX PHLX
				case "Y":
					return "BYX";// BATS Y-Exchange BYX
				case "Z":
					return "BATS";// BATS (Alpha Quote Feed)
				//case "*": // Composite (Equities)
				//case "O": // Composite (Options)
				default:
					throw new ArgumentOutOfRangeException("exch");
			}
		}
	}
}