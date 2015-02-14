namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class Level1Table : Table<Level1ChangeMessage>
	{
		public Level1Table(Security security)
			: base("Level1", CreateColumns(security))
		{
		}

		private static Type GetDbType(Level1Fields field)
		{
			switch (field)
			{
				case Level1Fields.LastTrade:
				case Level1Fields.BestBid:
				case Level1Fields.BestAsk:
					return null;
				case Level1Fields.BidsCount:
				case Level1Fields.AsksCount:
				case Level1Fields.TradesCount:
				case Level1Fields.State:
					return typeof(int);
				case Level1Fields.LastTradeId:
					return typeof(long);
				case Level1Fields.LastTradeOrigin:
					return typeof(int?);
				case Level1Fields.BestBidTime:
				case Level1Fields.BestAskTime:
				case Level1Fields.LastTradeTime:
					return typeof(DateTimeOffset);
				default:
					return typeof(decimal);
			}
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
			yield return new ColumnDescription("LocalTime") { DbType = typeof(DateTime) };

			foreach (var field in Enumerator.GetValues<Level1Fields>())
			{
				var columnType = GetDbType(field);

				if (columnType == null)
					continue;

				var step = 0.000001m;

				switch (field)
				{
					case Level1Fields.OpenPrice:
					case Level1Fields.HighPrice:
					case Level1Fields.LowPrice:
					case Level1Fields.ClosePrice:
					case Level1Fields.MinPrice:
					case Level1Fields.MaxPrice:
					case Level1Fields.PriceStep:
					case Level1Fields.LastTradePrice:
					case Level1Fields.BestBidPrice:
					case Level1Fields.BestAskPrice:
					case Level1Fields.HighBidPrice:
					case Level1Fields.LowAskPrice:
						step = security.PriceStep;
						break;
					case Level1Fields.OpenInterest:
					case Level1Fields.BidsVolume:
					case Level1Fields.AsksVolume:
					case Level1Fields.VolumeStep:
					case Level1Fields.LastTradeVolume:
					case Level1Fields.Volume:
					case Level1Fields.BestBidVolume:
					case Level1Fields.BestAskVolume:
						step = security.VolumeStep;
						break;
					case Level1Fields.Multiplier:
						step = security.Multiplier;
						break;
				}

				yield return new ColumnDescription(field.ToString())
				{
					DbType = columnType.IsNullable() ? columnType : typeof(Nullable<>).Make(columnType),
					ValueRestriction = columnType == typeof(decimal) ? new DecimalRestriction { Scale = step.GetCachedDecimals() } : null,
				};
			}
		}

		public override IEnumerable<IDictionary<string, object>> ConvertToParameters(IEnumerable<Level1ChangeMessage> values)
		{
			return values
				.Select(m =>
				{
					var result = new Dictionary<String, object>
					{
						{ "SecurityCode", m.SecurityId.SecurityCode },
						{ "BoardCode", m.SecurityId.BoardCode },
						{ "ServerTime", m.ServerTime },
						{ "LocalTime", m.LocalTime },
					};

					foreach (var pair in m.Changes)
					{
						result.Add(pair.Key.ToString(), pair.Value.To(GetDbType(pair.Key)));
					}

					return result;
				})
				.ToArray();
		}
	}
}