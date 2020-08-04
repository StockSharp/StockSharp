#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Database.Algo
File: OrderLogTable.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Messages;

	class OrderLogTable : Table<ExecutionMessage>
	{
		public OrderLogTable(decimal? priceStep, decimal? volumeStep)
			: base("OrderLog", CreateColumns(priceStep, volumeStep))
		{
		}

		private static IEnumerable<ColumnDescription> CreateColumns(decimal? priceStep, decimal? volumeStep)
		{
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderId))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
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
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderPrice)) { DbType = typeof(decimal), ValueRestriction = new DecimalRestriction { Scale = priceStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderVolume)) { DbType = typeof(decimal), ValueRestriction = new DecimalRestriction { Scale = volumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.Side)) { DbType = typeof(int) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderStatus)) { DbType = typeof(long?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OrderState)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.TimeInForce)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.TradeId))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.TradePrice)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = priceStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.OpenInterest)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = volumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.SeqNum)) { DbType = typeof(long?) };
		}

		protected override IDictionary<string, object> ConvertToParameters(ExecutionMessage value)
		{
			var result = new Dictionary<string, object>
			{
				{ nameof(ExecutionMessage.OrderId), value.OrderId == null ? value.OrderStringId : value.OrderId.To<string>() },
				{ nameof(SecurityId.SecurityCode), value.SecurityId.SecurityCode },
				{ nameof(SecurityId.BoardCode), value.SecurityId.BoardCode },
				{ nameof(ExecutionMessage.ServerTime), value.ServerTime },
				{ nameof(ExecutionMessage.LocalTime), value.LocalTime },
				{ nameof(ExecutionMessage.OrderPrice), value.OrderPrice },
				{ nameof(ExecutionMessage.OrderVolume), value.OrderVolume },
				{ nameof(ExecutionMessage.Side), (int)value.Side },
				{ nameof(ExecutionMessage.OrderStatus), value.OrderStatus },
				{ nameof(ExecutionMessage.OrderState), (int?)value.OrderState },
				{ nameof(ExecutionMessage.TimeInForce), (int?)value.TimeInForce },
				{ nameof(ExecutionMessage.TradeId), value.TradeId == null ? value.TradeStringId : value.TradeId.To<string>() },
				{ nameof(ExecutionMessage.TradePrice), value.TradePrice },
				{ nameof(ExecutionMessage.OpenInterest), value.OpenInterest },
				{ nameof(ExecutionMessage.SeqNum), value.SeqNum.DefaultAsNull() },
			};
			return result;
		}
	}
}
