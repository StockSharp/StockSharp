#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.AlfaDirect.Native.AlfaDirect
File: AlfaUtils.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
			return data == null ? ArrayHelper.Empty<string>() : StringHelper.Split(data);
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
			throw new ArgumentOutOfRangeException(nameof(atCode));
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