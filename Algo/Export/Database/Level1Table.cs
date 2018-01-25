#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Database.Algo
File: Level1Table.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
			var type = field.ToType();

			if (type == null)
				return null;

			if (type.IsEnum)
				type = type.GetEnumUnderlyingType();

			return typeof(Nullable<>).Make(type);
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
			yield return new ColumnDescription(nameof(Level1ChangeMessage.ServerTime)) { DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription(nameof(Level1ChangeMessage.LocalTime)) { DbType = typeof(DateTimeOffset) };

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
						step = security.PriceStep ?? 1;
						break;
					case Level1Fields.OpenInterest:
					case Level1Fields.BidsVolume:
					case Level1Fields.AsksVolume:
					case Level1Fields.VolumeStep:
					case Level1Fields.LastTradeVolume:
					case Level1Fields.Volume:
					case Level1Fields.BestBidVolume:
					case Level1Fields.BestAskVolume:
						step = security.VolumeStep ?? 1;
						break;
					case Level1Fields.Multiplier:
						step = security.Multiplier ?? 1;
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
					var result = new Dictionary<string, object>
					{
						{ nameof(SecurityId.SecurityCode), m.SecurityId.SecurityCode },
						{ nameof(SecurityId.BoardCode), m.SecurityId.BoardCode },
						{ nameof(Level1ChangeMessage.ServerTime), m.ServerTime },
						{ nameof(Level1ChangeMessage.LocalTime), m.LocalTime },
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