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
	using StockSharp.BusinessEntities;

	internal class MarketDepthQuoteTable : Table<TimeQuoteChange>
	{
		public MarketDepthQuoteTable(Security security)
			: base("MarketDepthQuote", CreateColumns(security))
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
			yield return new ColumnDescription("Price")
			{
				DbType = typeof(decimal),
				ValueRestriction = new DecimalRestriction { Scale = security.PriceStep == null ? 1 : security.PriceStep.Value.GetCachedDecimals() }
			};
			yield return new ColumnDescription("Volume")
			{
				DbType = typeof(decimal),
				ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep == null ? 1 : security.VolumeStep.Value.GetCachedDecimals() }
			};
			yield return new ColumnDescription("Side") { IsPrimaryKey = true, DbType = typeof(int) };
			yield return new ColumnDescription("ServerTime") { IsPrimaryKey = true, DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription("LocalTime") { DbType = typeof(DateTime) };
		}

		protected override IDictionary<string, object> ConvertToParameters(TimeQuoteChange value)
		{
			var result = new Dictionary<string, object>
			{
				{ "SecurityCode", value.SecurityId.SecurityCode },
				{ "BoardCode", value.SecurityId.BoardCode },
				{ "Price", value.Price },
				{ "Volume", value.Volume },
				{ "Side", (int)value.Side },
				{ "ServerTime", value.ServerTime },
				{ "LocalTime", value.LocalTime },
			};
			return result;
		}
	}
}