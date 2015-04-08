namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class CandlesTable : Table<CandleMessage>
	{
		private readonly Type _candleType;
		private readonly object _arg;

		public CandlesTable(Security security, Type candleType, object arg)
			: base("Candle", CreateColumns(security))
		{
			if (candleType == null)
				throw new ArgumentNullException("candleType");

			if (arg == null)
				throw new ArgumentNullException("arg");

			_candleType = candleType;
			_arg = arg;
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
			yield return new ColumnDescription("OpenTime")
			{
				IsPrimaryKey = true,
				DbType = typeof(DateTimeOffset)
			};
			yield return new ColumnDescription("CloseTime") { DbType = typeof(DateTimeOffset) };
			yield return CreateDecimalColumn("OpenPrice", security.PriceStep);
			yield return CreateDecimalColumn("ClosePrice", security.PriceStep);
			yield return CreateDecimalColumn("HighPrice", security.PriceStep);
			yield return CreateDecimalColumn("LowPrice", security.PriceStep);
			yield return CreateDecimalColumn("TotalVolume", security.VolumeStep);
			yield return new ColumnDescription("OpenInterest")
			{
				DbType = typeof(decimal?),
				ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep == null ? 1 : security.VolumeStep.Value.GetCachedDecimals() }
			};
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
				{ "SecurityCode", value.SecurityId.SecurityCode },
				{ "BoardCode", value.SecurityId.BoardCode },
				{ "CandleType", _candleType.Name },
				{ "Argument", _arg.To<string>() },
				{ "OpenTime", value.OpenTime },
				{ "CloseTime", value.CloseTime },
				{ "OpenPrice", value.OpenPrice },
				{ "ClosePrice", value.ClosePrice },
				{ "HighPrice", value.HighPrice },
				{ "LowPrice", value.LowPrice },
				{ "TotalVolume", value.TotalVolume },
				{ "OpenInterest", value.OpenInterest },
			};
			return result;
		}
	}
}