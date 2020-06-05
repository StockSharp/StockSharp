#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.BitStamp
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;

	static class Extensions
	{
		public static Sides ToSide(this int type)
		{
			return type == 0 ? Sides.Buy : Sides.Sell;
		}

		public static string ToCurrency(this SecurityId securityId)
		{
			return securityId.SecurityCode?.Remove("/").ToLowerInvariant();
		}

		public static readonly string BitStampBoard = "BSTMP";

		public static bool IsAssociated(this SecurityId secId)
		{
			return secId.BoardCode.CompareIgnoreCase(BitStampBoard);
		}

		public static SecurityId ToStockSharp(this string currency, bool format = true)
		{
			if (format)
			{
				if (currency.Length > 3 && !currency.Contains("/"))
					currency = currency.Insert(3, "/");

				currency = currency.ToUpperInvariant();
			}

			return new SecurityId
			{
				SecurityCode = currency,
				BoardCode = BitStampBoard,
			};
		}

		public static DateTimeOffset ToDto(this string value, string format = "yyyy-MM-dd HH:mm:ss")
		{
			return value.ToDateTime(format).ApplyUtc();
		}
	}
}