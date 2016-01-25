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
			yield return new ColumnDescription(nameof(SecurityId.SecurityCode))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(SecurityId.BoardCode))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.ServerTime)) { DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription(nameof(ExecutionMessage.PortfolioName))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.ClientCode))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.DepoName))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.TransactionId)) { DbType = typeof(long) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OriginalTransactionId)) { DbType = typeof(long) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderId))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderPrice)) { DbType = typeof(decimal), ValueRestriction = new DecimalRestriction { Scale = security.PriceStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderVolume)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.Balance)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.Side)) { DbType = typeof(int) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderType)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderState)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.TradeId))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.TradePrice)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.PriceStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.TradeVolume)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.HasOrderInfo)) { DbType = typeof(bool) };
			yield return new ColumnDescription(nameof(ExecutionMessage.HasOrderInfo)) { DbType = typeof(bool) };
		}

		protected override IDictionary<string, object> ConvertToParameters(ExecutionMessage value)
		{
			var result = new Dictionary<string, object>
			{
				{ nameof(SecurityId.SecurityCode), value.SecurityId.SecurityCode },
				{ nameof(SecurityId.BoardCode), value.SecurityId.BoardCode },
				{ nameof(ExecutionMessage.ServerTime), value.ServerTime },
				{ nameof(ExecutionMessage.PortfolioName), value.PortfolioName },
				{ nameof(ExecutionMessage.ClientCode), value.ClientCode },
				{ nameof(ExecutionMessage.DepoName), value.DepoName },
				{ nameof(ExecutionMessage.TransactionId), value.TransactionId },
				{ nameof(ExecutionMessage.OriginalTransactionId), value.OriginalTransactionId },
				{ nameof(ExecutionMessage.OrderId), value.OrderId == null ? value.OrderStringId : value.OrderId.To<string>() },
				{ nameof(ExecutionMessage.OrderPrice), value.OrderPrice },
				{ nameof(ExecutionMessage.OrderVolume), value.OrderVolume },
				{ nameof(ExecutionMessage.Balance), value.Balance },
				{ nameof(ExecutionMessage.Side), (int)value.Side },
				{ nameof(ExecutionMessage.OrderType), (int?)value.OrderType },
				{ nameof(ExecutionMessage.OrderState), (int?)value.OrderState },
				{ nameof(ExecutionMessage.TradeId), value.TradeId == null ? value.TradeStringId : value.TradeId.To<string>() },
				{ nameof(ExecutionMessage.TradePrice), value.TradePrice },
				{ nameof(ExecutionMessage.TradeVolume), value.TradeVolume },
				{ nameof(value.HasOrderInfo), value.HasOrderInfo },
				{ nameof(value.HasTradeInfo), value.HasTradeInfo },
			};
			return result;
		}
	}
}