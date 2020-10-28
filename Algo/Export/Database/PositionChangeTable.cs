namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Messages;

	class PositionChangeTable : Table<PositionChangeMessage>
	{
		public PositionChangeTable(decimal? priceStep, decimal? volumeStep)
			: base("PositionChange", CreateColumns(priceStep, volumeStep))
		{
		}

		private static Type GetDbType(PositionChangeTypes field)
		{
			var type = field.ToType();

			if (type == null)
				return null;

			if (type.IsEnum)
				type = type.GetEnumUnderlyingType();

			return type.IsClass ? type : typeof(Nullable<>).Make(type);
		}

		private static IEnumerable<ColumnDescription> CreateColumns(decimal? priceStep, decimal? volumeStep)
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
			yield return new ColumnDescription(nameof(PositionChangeMessage.PortfolioName))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(PositionChangeMessage.ClientCode))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(PositionChangeMessage.DepoName))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(PositionChangeMessage.LimitType))
			{
				DbType = typeof(int?),
			};
			yield return new ColumnDescription(nameof(PositionChangeMessage.StrategyId))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32)
			};
			yield return new ColumnDescription(nameof(PositionChangeMessage.Side))
			{
				DbType = typeof(int?),
			};
			yield return new ColumnDescription(nameof(Level1ChangeMessage.ServerTime)) { DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription(nameof(Level1ChangeMessage.LocalTime)) { DbType = typeof(DateTimeOffset) };

			foreach (var type in Enumerator.GetValues<PositionChangeTypes>().Where(t => !t.IsObsolete()))
			{
				var columnType = GetDbType(type);

				if (columnType == null)
					continue;

				var step = 0.000001m;

				switch (type)
				{
					case PositionChangeTypes.State:
					case PositionChangeTypes.Currency:
						break;
					//default:
					//	step = security.Multiplier ?? 1;
					//	break;
				}

				yield return new ColumnDescription(type.ToString())
				{
					DbType = columnType.IsNullable() || columnType.IsClass ? columnType : typeof(Nullable<>).Make(columnType),
					ValueRestriction = columnType == typeof(decimal) ? new DecimalRestriction { Scale = step.GetCachedDecimals() } : null,
				};
			}
		}

		public override IEnumerable<IDictionary<string, object>> ConvertToParameters(IEnumerable<PositionChangeMessage> values)
		{
			return values
				.Select(m =>
				{
					var result = new Dictionary<string, object>
					{
						{ nameof(SecurityId.SecurityCode), m.SecurityId.SecurityCode },
						{ nameof(SecurityId.BoardCode), m.SecurityId.BoardCode },
						{ nameof(PositionChangeMessage.PortfolioName), m.PortfolioName },
						{ nameof(PositionChangeMessage.ClientCode), m.ClientCode },
						{ nameof(PositionChangeMessage.DepoName), m.DepoName },
						{ nameof(PositionChangeMessage.LimitType), (int?)m.LimitType },
						{ nameof(PositionChangeMessage.StrategyId), m.StrategyId },
						{ nameof(PositionChangeMessage.Side), (int?)m.Side },
						{ nameof(PositionChangeMessage.ServerTime), m.ServerTime },
						{ nameof(PositionChangeMessage.LocalTime), m.LocalTime },
					};

					foreach (var pair in m.Changes)
					{
						if (!pair.Key.IsObsolete())
							result.Add(pair.Key.ToString(), pair.Value.To(GetDbType(pair.Key)));
					}

					return result;
				})
				.ToArray();
		}
	}
}