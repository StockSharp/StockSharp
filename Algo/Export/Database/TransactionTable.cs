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
			yield return new ColumnDescription(nameof(ExecutionMessage.LocalTime)) { DbType = typeof(DateTimeOffset) };
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
			yield return new ColumnDescription(nameof(ExecutionMessage.BrokerCode))
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
			yield return new ColumnDescription(nameof(ExecutionMessage.ExpiryDate)) { DbType = typeof(DateTimeOffset?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderId))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			//yield return new ColumnDescription(nameof(ExecutionMessage.DerivedOrderId))
			//{
			//	DbType = typeof(string),
			//	ValueRestriction = new StringRestriction(32)
			//};
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderPrice)) { DbType = typeof(decimal), ValueRestriction = new DecimalRestriction { Scale = security.PriceStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderVolume)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.VisibleVolume)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.Balance)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.Side)) { DbType = typeof(int) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OriginSide)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderType)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderState)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderStatus)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.IsSystem)) { DbType = typeof(bool?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.IsUpTick)) { DbType = typeof(bool?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.IsCancelled)) { DbType = typeof(bool) };
			yield return new ColumnDescription(nameof(ExecutionMessage.TradeId))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.TradePrice)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.PriceStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.TradeVolume)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.TradeStatus)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.HasOrderInfo)) { DbType = typeof(bool) };
			yield return new ColumnDescription(nameof(ExecutionMessage.HasOrderInfo)) { DbType = typeof(bool) };
			yield return new ColumnDescription(nameof(ExecutionMessage.Comment))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(1024)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.SystemComment))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(1024)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.Commission)) { DbType = typeof(decimal?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.CommissionCurrency))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.Slippage)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.PriceStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.Latency)) { DbType = typeof(TimeSpan?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.Position)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.PnL)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.PriceStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.OpenInterest)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.Currency)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.Error))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(1024)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.IsMargin)) { DbType = typeof(bool?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.IsMarketMaker)) { DbType = typeof(bool?) };
		}

		protected override IDictionary<string, object> ConvertToParameters(ExecutionMessage value)
		{
			var result = new Dictionary<string, object>
			{
				{ nameof(SecurityId.SecurityCode), value.SecurityId.SecurityCode },
				{ nameof(SecurityId.BoardCode), value.SecurityId.BoardCode },
				{ nameof(ExecutionMessage.ServerTime), value.ServerTime },
				{ nameof(ExecutionMessage.LocalTime), value.LocalTime },
				{ nameof(ExecutionMessage.PortfolioName), value.PortfolioName },
				{ nameof(ExecutionMessage.ClientCode), value.ClientCode },
				{ nameof(ExecutionMessage.BrokerCode), value.BrokerCode },
				{ nameof(ExecutionMessage.DepoName), value.DepoName },
				{ nameof(ExecutionMessage.TransactionId), value.TransactionId },
				{ nameof(ExecutionMessage.OriginalTransactionId), value.OriginalTransactionId },
				{ nameof(ExecutionMessage.ExpiryDate), value.ExpiryDate },
				{ nameof(ExecutionMessage.OrderId), value.OrderId == null ? value.OrderStringId : value.OrderId.To<string>() },
				//{ nameof(ExecutionMessage.DerivedOrderId), value.DerivedOrderId == null ? value.DerivedOrderStringId : value.DerivedOrderId.To<string>() },
				{ nameof(ExecutionMessage.OrderPrice), value.OrderPrice },
				{ nameof(ExecutionMessage.OrderVolume), value.OrderVolume },
				{ nameof(ExecutionMessage.VisibleVolume), value.VisibleVolume },
				{ nameof(ExecutionMessage.Balance), value.Balance },
				{ nameof(ExecutionMessage.Side), (int)value.Side },
				{ nameof(ExecutionMessage.OriginSide), (int?)value.OriginSide },
				{ nameof(ExecutionMessage.OrderType), (int?)value.OrderType },
				{ nameof(ExecutionMessage.OrderState), (int?)value.OrderState },
				{ nameof(ExecutionMessage.OrderStatus), (int?)value.OrderStatus },
				{ nameof(ExecutionMessage.IsSystem), value.IsSystem },
				{ nameof(ExecutionMessage.IsUpTick), value.IsUpTick },
				{ nameof(ExecutionMessage.IsCancelled), value.IsCancelled },
				{ nameof(ExecutionMessage.TradeId), value.TradeId == null ? value.TradeStringId : value.TradeId.To<string>() },
				{ nameof(ExecutionMessage.TradePrice), value.TradePrice },
				{ nameof(ExecutionMessage.TradeVolume), value.TradeVolume },
				{ nameof(ExecutionMessage.TradeStatus), value.TradeStatus },
				{ nameof(ExecutionMessage.HasOrderInfo), value.HasOrderInfo },
				{ nameof(ExecutionMessage.HasTradeInfo), value.HasTradeInfo },
				{ nameof(ExecutionMessage.Comment), value.Comment },
				{ nameof(ExecutionMessage.SystemComment), value.SystemComment },
				{ nameof(ExecutionMessage.Commission), value.Commission },
				{ nameof(ExecutionMessage.CommissionCurrency), value.CommissionCurrency },
				{ nameof(ExecutionMessage.Slippage), value.Slippage },
				{ nameof(ExecutionMessage.Latency), value.Latency },
				{ nameof(ExecutionMessage.Position), value.Position },
				{ nameof(ExecutionMessage.PnL), value.PnL },
				{ nameof(ExecutionMessage.OpenInterest), value.OpenInterest },
				{ nameof(ExecutionMessage.Currency), (int?)value.Currency },
				{ nameof(ExecutionMessage.Error), value.Error?.Message },
				{ nameof(ExecutionMessage.IsMargin), value.IsMargin },
				{ nameof(ExecutionMessage.IsMarketMaker), value.IsMarketMaker },
			};
			return result;
		}
	}
}