#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Database.Algo
File: TransactionTable.cs
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

	class TransactionTable : Table<ExecutionMessage>
	{
		public TransactionTable(Security security)
			: base("Transaction", CreateColumns(security))
		{
		}

		private static IEnumerable<ColumnDescription> CreateColumns(Security security)
		{
			yield return new ColumnDescription("SecurityCode")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription("BoardCode")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription("ServerTime") { DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription("PortfolioName")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription("TransactionId") { DbType = typeof(int) };
			yield return new ColumnDescription("OrderId")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription("OrderPrice") { DbType = typeof(decimal), ValueRestriction = new DecimalRestriction { Scale = security.PriceStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription("OrderVolume") { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription("Balance") { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription("Side") { DbType = typeof(int) };
			yield return new ColumnDescription("OrderType") { DbType = typeof(int?) };
			yield return new ColumnDescription("OrderState") { DbType = typeof(int?) };
			yield return new ColumnDescription("TradeId")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription("TradePrice") { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.PriceStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription("TradeVolume") { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.HasOrderInfo)) { DbType = typeof(bool) };
			yield return new ColumnDescription(nameof(ExecutionMessage.HasOrderInfo)) { DbType = typeof(bool) };
		}

		protected override IDictionary<string, object> ConvertToParameters(ExecutionMessage value)
		{
			var result = new Dictionary<string, object>
			{
				{ "SecurityCode", value.SecurityId.SecurityCode },
				{ "BoardCode", value.SecurityId.BoardCode },
				{ "ServerTime", value.ServerTime },
				{ "PortfolioName", value.PortfolioName },
				{ "TransactionId", value.TransactionId },
				{ "OrderId", value.OrderId == null ? value.OrderStringId : value.OrderId.To<string>() },
				{ "OrderPrice", value.OrderPrice },
				{ "OrderVolume", value.OrderVolume },
				{ "Balance", value.Balance },
				{ "Side", (int)value.Side },
				{ "OrderType", (int?)value.OrderType },
				{ "OrderState", (int?)value.OrderState },
				{ "TradeId", value.TradeId == null ? value.TradeStringId : value.TradeId.To<string>() },
				{ "TradePrice", value.TradePrice },
				{ "TradeVolume", value.TradeVolume },
				{ nameof(value.HasOrderInfo), value.HasOrderInfo },
				{ nameof(value.HasTradeInfo), value.HasTradeInfo },
			};
			return result;
		}
	}
}