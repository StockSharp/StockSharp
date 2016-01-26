#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Database.Algo
File: SecuritiesTable.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class SecuritiesTable : Table<SecurityMessage>
	{
		public SecuritiesTable(Security security)
			: base("Security", CreateColumns(security))
		{
		}

		private static IEnumerable<ColumnDescription> CreateColumns(Security security)
		{
			yield return new ColumnDescription("SecurityCode")
			{
				IsPrimaryKey = true,
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription("BoardCode")
			{
				IsPrimaryKey = true,
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription("PriceStep")
			{
				DbType = typeof(decimal?),
				ValueRestriction = new DecimalRestriction { Scale = security.PriceStep?.GetCachedDecimals() ?? 1 }
			};
			yield return new ColumnDescription("VolumeStep")
			{
				DbType = typeof(decimal?),
				ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 }
			};
			yield return new ColumnDescription("Multiplier")
			{
				DbType = typeof(decimal?),
				ValueRestriction = new DecimalRestriction { Scale = security.Multiplier?.GetCachedDecimals() ?? 1 }
			};
			yield return new ColumnDescription("Decimals")
			{
				DbType = typeof(int?),
			};
			yield return new ColumnDescription("SecurityType")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription("OptionType")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription("BinaryOptionType")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription("Strike")
			{
				DbType = typeof(decimal?),
				ValueRestriction = new DecimalRestriction()
			};
			yield return new ColumnDescription("UnderlyingSecurityCode")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription("ExpiryDate")
			{
				DbType = typeof(DateTimeOffset?),
			};
			yield return new ColumnDescription("Currency")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(3)
			};
			yield return new ColumnDescription("Name")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription("ShortName")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(64)
			};
			yield return new ColumnDescription("SettlementDate")
			{
				DbType = typeof(DateTimeOffset?),
			};
			yield return new ColumnDescription("Bloomberg")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription("CUSIP")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription("IQFeed")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription("InteractiveBrokers")
			{
				DbType = typeof(int?),
			};
			yield return new ColumnDescription("ISIN")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription("Plaza")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription("Ric")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription("SEDOL")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
		}

		protected override IDictionary<string, object> ConvertToParameters(SecurityMessage value)
		{
			var result = new Dictionary<string, object>
			{
				{ "SecurityCode", value.SecurityId.SecurityCode },
				{ "BoardCode", value.SecurityId.BoardCode },
				{ "PriceStep", value.PriceStep },
				{ "VolumeStep", value.VolumeStep },
				{ "Multiplier", value.Multiplier },
				{ "Decimals", value.Decimals },
				{ "SecurityType", value.SecurityType.ToString() },
				{ "OptionType", value.OptionType.ToString() },
				{ "BinaryOptionType", value.BinaryOptionType },
				{ "Strike", value.Strike },
				{ "UnderlyingSecurityCode", value.UnderlyingSecurityCode },
				{ "ExpiryDate", value.ExpiryDate },
				{ "Currency", value.Currency.ToString() },
				{ "Name", value.Name },
				{ "ShortName", value.ShortName },
				{ "SettlementDate", value.SettlementDate },
				{ "Bloomberg", value.SecurityId.Bloomberg },
				{ "CUSIP", value.SecurityId.Cusip },
				{ "IQFeed", value.SecurityId.IQFeed },
				{ "InteractiveBrokers", value.SecurityId.InteractiveBrokers },
				{ "ISIN", value.SecurityId.Isin },
				{ "Plaza", value.SecurityId.Plaza },
				{ "RIC", value.SecurityId.Ric },
				{ "SEDOL", value.SecurityId.Sedol },
			};
			return result;
		}
	}
}