#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Database.Algo
File: CandlesTable.cs
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

	class CandleTable : Table<CandleMessage>
	{
		public CandleTable(Security security)
			: base("Candle", CreateColumns(security))
		{
		}

		private static IEnumerable<ColumnDescription> CreateColumns(Security security)
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
			yield return new ColumnDescription("CandleType")
			{
				IsPrimaryKey = true,
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription("Argument")
			{
				IsPrimaryKey = true,
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(100)
			};
			yield return new ColumnDescription(nameof(CandleMessage.OpenTime))
			{
				IsPrimaryKey = true,
				DbType = typeof(DateTimeOffset)
			};
			yield return new ColumnDescription(nameof(CandleMessage.CloseTime))
			{
				DbType = typeof(DateTimeOffset)
			};
			yield return CreateDecimalColumn(nameof(CandleMessage.OpenPrice), security.PriceStep);
			yield return CreateDecimalColumn(nameof(CandleMessage.HighPrice), security.PriceStep);
			yield return CreateDecimalColumn(nameof(CandleMessage.LowPrice), security.PriceStep);
			yield return CreateDecimalColumn(nameof(CandleMessage.ClosePrice), security.PriceStep);
			yield return CreateDecimalColumn(nameof(CandleMessage.TotalVolume), security.VolumeStep);
			yield return new ColumnDescription(nameof(CandleMessage.OpenInterest))
			{
				DbType = typeof(decimal?),
				ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep?.GetCachedDecimals() ?? 1 }
			};
			yield return new ColumnDescription(nameof(CandleMessage.TotalTicks)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(CandleMessage.UpTicks)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(CandleMessage.DownTicks)) { DbType = typeof(int?) };
		}

		private static ColumnDescription CreateDecimalColumn(string name, decimal? step)
		{
			return new ColumnDescription(name)
			{
				DbType = typeof(decimal),
				ValueRestriction = new DecimalRestriction { Scale = (step ?? 1m).GetCachedDecimals() }
			};
		}

		protected override IDictionary<string, object> ConvertToParameters(CandleMessage value)
		{
			var result = new Dictionary<string, object>
			{
				{ nameof(SecurityId.SecurityCode), value.SecurityId.SecurityCode },
				{ nameof(SecurityId.BoardCode), value.SecurityId.BoardCode },
				{ "CandleType", value.GetType().Name.Replace(nameof(Message), string.Empty) },
				{ "Argument", value.Arg.To<string>() },
				{ nameof(CandleMessage.OpenTime), value.OpenTime },
				{ nameof(CandleMessage.CloseTime), value.CloseTime },
				{ nameof(CandleMessage.OpenPrice), value.OpenPrice },
				{ nameof(CandleMessage.HighPrice), value.HighPrice },
				{ nameof(CandleMessage.LowPrice), value.LowPrice },
				{ nameof(CandleMessage.ClosePrice), value.ClosePrice },
				{ nameof(CandleMessage.TotalVolume), value.TotalVolume },
				{ nameof(CandleMessage.OpenInterest), value.OpenInterest },
				{ nameof(CandleMessage.TotalTicks), value.TotalTicks },
				{ nameof(CandleMessage.UpTicks), value.UpTicks },
				{ nameof(CandleMessage.DownTicks), value.DownTicks },
			};
			return result;
		}
	}
}