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

	using StockSharp.Messages;

	class SecurityTable : Table<SecurityMessage>
	{
		public SecurityTable()
			: base("Security", CreateColumns())
		{
		}

		private static IEnumerable<ColumnDescription> CreateColumns()
		{
			yield return new ColumnDescription(nameof(SecurityId.SecurityCode))
			{
				IsPrimaryKey = true,
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(SecurityId.BoardCode))
			{
				IsPrimaryKey = true,
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(SecurityMessage.PriceStep))
			{
				DbType = typeof(decimal?),
				ValueRestriction = new DecimalRestriction()
			};
			yield return new ColumnDescription(nameof(SecurityMessage.VolumeStep))
			{
				DbType = typeof(decimal?),
				ValueRestriction = new DecimalRestriction { Scale = 1 }
			};
			yield return new ColumnDescription(nameof(SecurityMessage.Multiplier))
			{
				DbType = typeof(decimal?),
				ValueRestriction = new DecimalRestriction { Scale = 1 }
			};
			yield return new ColumnDescription(nameof(SecurityMessage.Decimals))
			{
				DbType = typeof(int?),
			};
			yield return new ColumnDescription(nameof(SecurityMessage.SecurityType))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(SecurityMessage.OptionType))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(SecurityMessage.BinaryOptionType))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(SecurityMessage.Strike))
			{
				DbType = typeof(decimal?),
				ValueRestriction = new DecimalRestriction()
			};
			yield return new ColumnDescription(nameof(SecurityMessage.UnderlyingSecurityCode))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(SecurityMessage.UnderlyingSecurityType))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(SecurityMessage.ExpiryDate))
			{
				DbType = typeof(DateTimeOffset?),
			};
			yield return new ColumnDescription(nameof(SecurityMessage.Currency))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(3)
			};
			yield return new ColumnDescription(nameof(SecurityMessage.Name))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(SecurityMessage.ShortName))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(64)
			};
			yield return new ColumnDescription(nameof(SecurityMessage.SettlementDate))
			{
				DbType = typeof(DateTimeOffset?),
			};
			yield return new ColumnDescription(nameof(SecurityMessage.IssueSize))
			{
				DbType = typeof(decimal?),
				ValueRestriction = new DecimalRestriction()
			};
			yield return new ColumnDescription(nameof(SecurityMessage.IssueDate))
			{
				DbType = typeof(DateTimeOffset?),
			};
			yield return new ColumnDescription(nameof(SecurityMessage.CfiCode))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(6)
			};
			yield return new ColumnDescription(nameof(SecurityMessage.BasketCode))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(2)
			};
			yield return new ColumnDescription(nameof(SecurityMessage.BasketExpression))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(int.MaxValue)
			};
			yield return new ColumnDescription(nameof(SecurityId.Bloomberg))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription(nameof(SecurityId.Cusip))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription(nameof(SecurityId.IQFeed))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription(nameof(SecurityId.InteractiveBrokers))
			{
				DbType = typeof(int?),
			};
			yield return new ColumnDescription(nameof(SecurityId.Isin))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription(nameof(SecurityId.Plaza))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription(nameof(SecurityId.Ric))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
			yield return new ColumnDescription(nameof(SecurityId.Sedol))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(16)
			};
		}

		protected override IDictionary<string, object> ConvertToParameters(SecurityMessage value)
		{
			var result = new Dictionary<string, object>
			{
				{ nameof(SecurityId.SecurityCode), value.SecurityId.SecurityCode },
				{ nameof(SecurityId.BoardCode), value.SecurityId.BoardCode },
				{ nameof(SecurityMessage.PriceStep), value.PriceStep },
				{ nameof(SecurityMessage.VolumeStep), value.VolumeStep },
				{ nameof(SecurityMessage.Multiplier), value.Multiplier },
				{ nameof(SecurityMessage.Decimals), value.Decimals },
				{ nameof(SecurityMessage.SecurityType), value.SecurityType.ToString() },
				{ nameof(SecurityMessage.OptionType), value.OptionType.ToString() },
				{ nameof(SecurityMessage.BinaryOptionType), value.BinaryOptionType },
				{ nameof(SecurityMessage.Strike), value.Strike },
				{ nameof(SecurityMessage.UnderlyingSecurityCode), value.UnderlyingSecurityCode },
				{ nameof(SecurityMessage.UnderlyingSecurityType), value.UnderlyingSecurityType.ToString() },
				{ nameof(SecurityMessage.ExpiryDate), value.ExpiryDate },
				{ nameof(SecurityMessage.Currency), value.Currency.ToString() },
				{ nameof(SecurityMessage.Name), value.Name },
				{ nameof(SecurityMessage.ShortName), value.ShortName },
				{ nameof(SecurityMessage.SettlementDate), value.SettlementDate },
				{ nameof(SecurityMessage.IssueSize), value.IssueSize },
				{ nameof(SecurityMessage.IssueDate), value.IssueDate },
				{ nameof(SecurityMessage.CfiCode), value.CfiCode },
				{ nameof(SecurityId.Bloomberg), value.SecurityId.Bloomberg },
				{ nameof(SecurityId.Cusip), value.SecurityId.Cusip },
				{ nameof(SecurityId.IQFeed), value.SecurityId.IQFeed },
				{ nameof(SecurityId.InteractiveBrokers), value.SecurityId.InteractiveBrokers },
				{ nameof(SecurityId.Isin), value.SecurityId.Isin },
				{ nameof(SecurityId.Plaza), value.SecurityId.Plaza },
				{ nameof(SecurityId.Ric), value.SecurityId.Ric },
				{ nameof(SecurityId.Sedol), value.SecurityId.Sedol },
			};
			return result;
		}
	}
}