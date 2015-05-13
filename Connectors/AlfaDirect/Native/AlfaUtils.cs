namespace StockSharp.AlfaDirect.Native
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	static class AlfaUtils
	{
		public static string ToAlfaDirect(this Sides side)
		{
			return side == Sides.Buy ? "B" : "S";
		}

		public static string[] ToRows(this string data)
		{
			return StringHelper.Split(data);
		}

		public static string[] ToColumns(this string row)
		{
			return row.Split('|');
		}

		public static string AccountFromPortfolioName(this string portfolioName)
		{
			return portfolioName.Split('@')[0];
		}

		public static string TreatyFromAccount(this string account)
		{
			var arr = account.Split("-");
			return arr.Length == 2 ? arr[0] : null;
		}

		public static OptionTypes ATCodeToOptionType(this string atCode)
		{
			switch (atCode)
			{
				case "OCM": return OptionTypes.Call;
				case "OPM": return OptionTypes.Put;
			}
			throw new ArgumentOutOfRangeException("atCode");
		}

		public static string GetSecurityClass(this IDictionary<string, RefPair<SecurityTypes, string>> securityClassInfo, SecurityTypes? secType, string boardName)
		{
			if (secType == null)
				return null;

			if (boardName == ExchangeBoard.Forts.Code)
				return secType == SecurityTypes.Stock ? "RTS_STANDARD" : "FORTS";

			var kv = securityClassInfo.FirstOrDefault(kv2 => kv2.Value.First == secType && kv2.Value.Second == boardName);

			if (!kv.IsDefault())
				return kv.Key;

			return null;
		}
	}
}