#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rithmic.Rithmic
File: RithmicUtils.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Rithmic
{
	using System;
	using System.Text;

	using com.omnesys.rapi;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	internal static class RithmicUtils
	{
		public static Action<T> WithError<T>(this Action<T> handler, Action<Exception> errorHandler)
		{
			if (errorHandler == null)
				throw new ArgumentNullException(nameof(errorHandler));

			return v =>
			{
				try
				{
					handler.SafeInvoke(v);
				}
				catch (Exception ex)
				{
					errorHandler(ex);
				}
			};
		}

		public static Action<T> WithDump<T>(this Action<T> handler, ILogReceiver receiver)
		{
			if (receiver == null)
				throw new ArgumentNullException(nameof(receiver));

			return v =>
			{
				receiver.AddLog(LogLevels.Debug, () => v.DumpableToString());
				handler.SafeInvoke(v);
			};
		}

		public static string DumpableToString(this object dumpable)
		{
			if (dumpable == null)
				throw new ArgumentNullException(nameof(dumpable));

			dynamic d = dumpable;
			var sb = new StringBuilder();
			d.Dump(sb);
			return sb.ToString();
		}

		public static DateTimeOffset ToTime(int seconds, int microseconds = 0)
		{
			return TimeHelper.GregorianStart
				.AddSeconds(seconds)
				.AddTicks(microseconds * TimeHelper.TicksPerMicrosecond)
				.ApplyTimeZone(TimeZoneInfo.Utc);
		}

		public static DateTimeOffset? ToDateTime(string date, string time)
		{
			if (date.IsEmpty())
				return null;

			var dateTime = date.ToDateTimeOffset("yyyyMMdd");

			if (!time.IsEmpty())
				dateTime = ToTime(time.To<int>());

			return dateTime;
		}

		public static SecurityTypes? ToSecurityType(string instrumentType)
		{
			if (instrumentType.IsEmpty())
				return null;

			switch (instrumentType.ToLowerInvariant())
			{
				case "future":
					return SecurityTypes.Future;
				case "future option":
					return SecurityTypes.Option;
				case "spread":
					return SecurityTypes.Index;
				default:
					throw new ArgumentOutOfRangeException(nameof(instrumentType), LocalizedStrings.Str2140Params.Put(instrumentType));
			}
		}

		public static OptionTypes? ToOptionType(string putCallIndicator)
		{
			if (putCallIndicator.IsEmpty())
				return null;

			switch (putCallIndicator.ToLowerInvariant())
			{
				case "call":
					return OptionTypes.Call;
				case "put":
					return OptionTypes.Put;
				default:
					throw new ArgumentOutOfRangeException(nameof(putCallIndicator), LocalizedStrings.Str1606Params.Put(putCallIndicator));
			}
		}

		public static Sides? ToOriginSide(string aggressorSide)
		{
			switch (aggressorSide)
			{
				case "B":
					return Sides.Buy;
				case "S":
					return Sides.Sell;
				default:
					return null;
			}
		}

		public static string ToRithmic(this Sides side)
		{
			return side == Sides.Buy
				? Constants.BUY_SELL_TYPE_BUY
				: Constants.BUY_SELL_TYPE_SELL;
		}

		public static string ToRithmic(this TimeInForce? tif, DateTimeOffset? expiryDate)
		{
			switch (tif)
			{
				case TimeInForce.PutInQueue:
				case null:
				{
					if (expiryDate == null)
						return Constants.ORDER_DURATION_GTC;
					else// if (expiryDate == DateTime.Today)
						return Constants.ORDER_DURATION_DAY;
				}
				case TimeInForce.MatchOrCancel:
					return Constants.ORDER_DURATION_FOK;
				case TimeInForce.CancelBalance:
					return Constants.ORDER_DURATION_IOC;
				default:
					throw new ArgumentOutOfRangeException(nameof(tif));
			}
		}

		public static OrderTypes ToOrderType(string orderType)
		{
			if (orderType == Constants.ORDER_TYPE_LIMIT)
				return OrderTypes.Limit;
			else if (orderType == Constants.ORDER_TYPE_LMT_IF_TOUCHED)
				return OrderTypes.Limit;
			else if (orderType == Constants.ORDER_TYPE_MARKET)
				return OrderTypes.Market;
			else if (orderType == Constants.ORDER_TYPE_MKT_IF_TOUCHED)
				return OrderTypes.Market;
			else if (orderType == Constants.ORDER_TYPE_STOP_LIMIT)
				return OrderTypes.Conditional;
			else if (orderType == Constants.ORDER_TYPE_STOP_MARKET)
				return OrderTypes.Conditional;
			else if (orderType == Constants.ORDER_TYPE_EXTERNAL)
				return OrderTypes.Execute;
			else
				throw new ArgumentOutOfRangeException(nameof(orderType), orderType, LocalizedStrings.Str3499);
		}

		public static Sides ToSide(string buySellType)
		{
			if (buySellType == Constants.BUY_SELL_TYPE_BUY)
				return Sides.Buy;
			else if (buySellType == Constants.BUY_SELL_TYPE_SELL)
				return Sides.Sell;
			else if (buySellType == Constants.BUY_SELL_TYPE_SELL_SHORT)
				return Sides.Sell;
			else if (buySellType == Constants.BUY_SELL_TYPE_SELL_SHORT_EXEMPT)
				return Sides.Sell;
			else
				throw new ArgumentOutOfRangeException(nameof(buySellType), buySellType, LocalizedStrings.Str3500);
		}

		public static TimeInForce ToTif(string orderDuration)
		{
			if (orderDuration == Constants.ORDER_DURATION_DAY)
				return TimeInForce.PutInQueue;
			else if (orderDuration == Constants.ORDER_DURATION_FOK)
				return TimeInForce.MatchOrCancel;
			else if (orderDuration == Constants.ORDER_DURATION_GTC)
				return TimeInForce.PutInQueue;
			else if (orderDuration == Constants.ORDER_DURATION_IOC)
				return TimeInForce.CancelBalance;
			else
				throw new ArgumentOutOfRangeException(nameof(orderDuration), orderDuration, LocalizedStrings.Str3501);
		}

		public static int ToSsboe(this DateTimeOffset time)
		{
			var value = (int)(time.UtcDateTime - TimeHelper.GregorianStart).TotalSeconds;

			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(time), time, LocalizedStrings.Str3502);

			return value;
		}

		public static PortfolioStates? ToPortfolioState(this string state)
		{
			switch (state)
			{
				case "active":
					return PortfolioStates.Active;
				case "inactive":
					return PortfolioStates.Blocked;
				default:
					return null;
			}
		}

		public static BaseChangeMessage<TChange> TryAdd<TChange>(this BaseChangeMessage<TChange> message, TChange change, Ignorable<double> value)
		{
			switch (value.State)
			{
				case Ignorable<double>.ValueState.Ignore:
					break;
				case Ignorable<double>.ValueState.Clear:
					message.Add(change, 0m);
					break;
				case Ignorable<double>.ValueState.Use:
					message.TryAdd(change, value.Value.ToDecimal());
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return message;
		}
	}
}