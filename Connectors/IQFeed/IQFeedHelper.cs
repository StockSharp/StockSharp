namespace StockSharp.IQFeed
{
	using System;
	using System.Collections.Generic;
	using System.Net;

	using Ecng.Common;
	using Ecng.Net;

	using StockSharp.Messages;

	static class IQFeedHelper
	{
		public static byte[] DownloadSymbols()
		{
			using (var client = new WebClientEx { Timeout = TimeSpan.FromMinutes(3), DecompressionMethods = DecompressionMethods.Deflate | DecompressionMethods.GZip })
			{
				return client.DownloadData("http://www.dtniq.com/product/mktsymbols_v2.zip");
			}
		}

		public static SecurityTypes? ToSecurityType(this string value)
		{
			switch (value)
			{
				case "EQUITY":
				case "SPOT":
				case "EQTYDEPTH":
				case "JACOBSEN": // TODO The Jacobsen
					return SecurityTypes.Stock;
				case "FUTURE":
				case "SSFUTURE":
				case "FUTDEPTH":
					return SecurityTypes.Future;
				case "FORWARD":
					return SecurityTypes.Forward;
				case "IEOPTION": // Index/Equity Option
				case "FOPTION": // Future Option
					return SecurityTypes.Option;
				case "BONDS":
				case "TREASURIES":
					return SecurityTypes.Bond;
				case "INDEX":
				case "SPREAD": // Future Spread
				case "STRATSPREAD": // Strategy Spread
				case "ICSPREAD": // Inter-Commodity Future Spread
				case "MKTSTATS": // Market Statistic
				case "MKTRPT": // Market Reports
				case "CALC": // DTN Calculated Statistic
				case "STRIP":
				case "SPRDEPTH":
					return SecurityTypes.Index;
				case "MONEY": // Money Market Fund
				case "MUTUAL": // Mutual Fund
					return SecurityTypes.Fund;
				case "FOREX": // Foreign Monetary Exchange
					return SecurityTypes.Currency;
				case "PRECMTL": // Precious Metals
				case "ARGUS": // Argus Energy
				case "RACKS": // Racks Energy
				case "RFSPOT": // Refined Fuel Spot
				case "SNL_NG": // SNL Natural Gas
				case "SNL_ELEC": // SNL Electricity
				case "NP_FLOW": // Nord Pool-N2EX Flow
				case "NP_POWER": // Nord Pool-N2EX Power Prices
				case "NP_CAPACITY": // Nord Pool-N2EX Capacity
				case "COMM3": // Commodity 3
					return SecurityTypes.Commodity;
				case "SWAPS": // Interest Rate Swap
					return SecurityTypes.Swap;
				default:
					return null;
					//throw new ArgumentException("Неизвестный тип инструмента '{0}'.".Put(value));
			}
		}

		public static void Exclude(this HashSet<Level1Fields> types, string data)
		{
			if (data.IndexOfAny(new[] { 'C', 'E', 'O' }) == -1)
			{
				types.Remove(Level1Fields.LastTrade);
				types.Remove(Level1Fields.LastTradeId);
				types.Remove(Level1Fields.LastTradePrice);
				types.Remove(Level1Fields.LastTradeTime);
				types.Remove(Level1Fields.LastTradeVolume);
			}

			if (data.IndexOf('b') == -1)
			{
				types.Remove(Level1Fields.BestBidPrice);
				types.Remove(Level1Fields.BestBidTime);
				types.Remove(Level1Fields.BestBidVolume);
			}

			if (data.IndexOf('a') == -1)
			{
				types.Remove(Level1Fields.BestAskPrice);
				types.Remove(Level1Fields.BestAskTime);
				types.Remove(Level1Fields.BestAskVolume);
			}

			if (data.IndexOf('o') == -1)
				types.Remove(Level1Fields.OpenPrice);

			if (data.IndexOf('h') == -1)
				types.Remove(Level1Fields.HighPrice);

			if (data.IndexOf('l') == -1)
				types.Remove(Level1Fields.LowPrice);

			if (data.IndexOf('c') == -1)
				types.Remove(Level1Fields.ClosePrice);

			if (data.IndexOf('s') == -1)
				types.Remove(Level1Fields.SettlementPrice);
		}

		public static long GetRequestId(this Message message)
		{
			return message.GetValue<long>("RequestId");
		}

		public static Message InitRequestId(this Message message, long requestId)
		{
			if (requestId > 0)
				message.AddValue("RequestId", requestId);

			return message;
		}

		public static object Convert(this IQFeedLevel1Column column, string value)
		{
			if (column == null)
				throw new ArgumentNullException(nameof(column));

			if (value.IsEmpty())
				return null;

			var convValue = value.To(column.Type);

			if (column.Type.IsNumeric() && convValue.To<decimal>() == 0)
				convValue = null;

			return convValue;
		}

		public static TimeSpan? ConvertToTimeSpan(this IQFeedLevel1Column column, string value)
		{
			if (column == null)
				throw new ArgumentNullException(nameof(column));

			// http://stocksharp.com/forum/yaf_postsm32150_API-4-2-2-24--InvalidCastException.aspx#post32150
			if (value.ContainsIgnoreCase("99:99:99"))
				return null;

			return value.TryToTimeSpan(column.Format);
		}

		public static DateTimeOffset? ToEst(this DateTime? estTime)
		{
			if (estTime == null)
				return null;

			return estTime.Value.ApplyTimeZone(TimeHelper.Est);
		}

		public static DateTime ToEst(this DateTimeOffset? time)
		{
			if (time == null)
				throw new ArgumentNullException(nameof(time));

			return time.Value.ToLocalTime(TimeHelper.Est);
		}
	}
}