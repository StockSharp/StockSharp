#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Database.Algo
File: MarketDepthQuoteTable.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Messages;

	class MarketDepthQuoteTable : Table<TimeQuoteChange>
	{
		public MarketDepthQuoteTable(decimal? priceStep, decimal? volumeStep)
			: base("MarketDepthQuote", CreateColumns(priceStep, volumeStep))
		{
		}

		private static IEnumerable<ColumnDescription> CreateColumns(decimal? priceStep, decimal? volumeStep)
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
			yield return new ColumnDescription(nameof(QuoteChange.Price))
			{
				DbType = typeof(decimal),
				ValueRestriction = new DecimalRestriction { Scale = priceStep?.GetCachedDecimals() ?? 1 }
			};
			yield return new ColumnDescription(nameof(QuoteChange.Volume))
			{
				DbType = typeof(decimal),
				ValueRestriction = new DecimalRestriction { Scale = volumeStep?.GetCachedDecimals() ?? 1 }
			};
			yield return new ColumnDescription(nameof(TimeQuoteChange.Side)) { IsPrimaryKey = true, DbType = typeof(int) };
			yield return new ColumnDescription(nameof(QuoteChange.OrdersCount)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(QuoteChange.Condition)) { DbType = typeof(byte) };
			yield return new ColumnDescription(nameof(TimeQuoteChange.ServerTime)) { IsPrimaryKey = true, DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription(nameof(TimeQuoteChange.LocalTime)) { DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription(nameof(QuoteChange.StartPosition)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(QuoteChange.EndPosition)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(QuoteChange.Action)) { DbType = typeof(byte?) };
		}

		protected override IDictionary<string, object> ConvertToParameters(TimeQuoteChange value)
		{
			var quote = value.Quote;

			var result = new Dictionary<string, object>
			{
				{ nameof(SecurityId.SecurityCode), value.SecurityId.SecurityCode },
				{ nameof(SecurityId.BoardCode), value.SecurityId.BoardCode },
				{ nameof(QuoteChange.Price), quote.Price },
				{ nameof(QuoteChange.Volume), quote.Volume },
				{ nameof(TimeQuoteChange.Side), (int)value.Side },
				{ nameof(QuoteChange.OrdersCount), quote.OrdersCount },
				{ nameof(QuoteChange.Condition), (byte)quote.Condition },
				{ nameof(TimeQuoteChange.ServerTime), value.ServerTime },
				{ nameof(TimeQuoteChange.LocalTime), value.LocalTime },
				{ nameof(QuoteChange.StartPosition), quote.EndPosition },
				{ nameof(QuoteChange.EndPosition), quote.EndPosition },
				{ nameof(QuoteChange.Action), (byte?)quote.Action },
			};
			return result;
		}
	}
}