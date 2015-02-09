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
				DbType = typeof(decimal),
				ValueRestriction = new DecimalRestriction { Scale = security.PriceStep.GetCachedDecimals() }
			};
			//yield return new ColumnDescription("StepPrice")
			//{
			//	DbType = typeof(decimal?),
			//	ValueRestriction = new DecimalRestriction()
			//};
			yield return new ColumnDescription("VolumeStep")
			{
				DbType = typeof(decimal),
				ValueRestriction = new DecimalRestriction { Scale = security.VolumeStep.GetCachedDecimals() }
			};
			yield return new ColumnDescription("Multiplier")
			{
				DbType = typeof(decimal),
				ValueRestriction = new DecimalRestriction { Scale = security.Multiplier.GetCachedDecimals() }
			};
			//yield return new ColumnDescription("Decimals")
			//{
			//	DbType = typeof(int),
			//};
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
		}

		protected override IDictionary<string, object> ConvertToParameters(SecurityMessage value)
		{
			var result = new Dictionary<string, object>
			{
				{ "SecurityCode", value.SecurityId.SecurityCode },
				{ "BoardCode", value.SecurityId.BoardCode },
				{ "PriceStep", value.PriceStep },
				//{ "StepPrice", value.StepPrice },
				{ "VolumeStep", value.VolumeStep },
				{ "Multiplier", value.Multiplier },
				//{ "Decimals", value.Decimals },
				{ "SecurityType", value.SecurityType.ToString() },
				{ "OptionType", value.OptionType.ToString() },
				{ "Strike", value.Strike == 0 ? (decimal?)null : value.Strike },
				{ "UnderlyingSecurityCode", value.UnderlyingSecurityCode },
				{ "ExpiryDate", value.ExpiryDate },
			};
			return result;
		}
	}
}