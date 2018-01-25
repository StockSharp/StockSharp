namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.Messages;

	class IndicatorValueTable : Table<IndicatorValue>
	{
		private const int _maxInnerValue = 4;

		public IndicatorValueTable()
			: base("IndicatorValue", CreateColumns())
		{
		}

		private static IEnumerable<ColumnDescription> CreateColumns()
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
			yield return new ColumnDescription(nameof(IndicatorValue.Time)) { DbType = typeof(DateTimeOffset) };

			for (var i = 0; i < _maxInnerValue; i++)
			{
				yield return new ColumnDescription(nameof(IndicatorValue.Value) + (i + 1))
				{
					DbType = typeof(decimal?),
					ValueRestriction = new DecimalRestriction { Precision = 10, Scale = 6 }
				};	
			}
		}

		protected override IDictionary<string, object> ConvertToParameters(IndicatorValue value)
		{
			var secId = value.Security?.ToSecurityId();

			var result = new Dictionary<string, object>
			{
				{ nameof(SecurityId.SecurityCode), secId?.SecurityCode },
				{ nameof(SecurityId.BoardCode), secId?.BoardCode },
				{ nameof(IndicatorValue.Time), value.Time },
			};

			var index = 0;
			foreach (var indVal in value.ValuesAsDecimal.Take(_maxInnerValue))
			{
				result.Add(nameof(IndicatorValue.Value) + (index + 1), indVal);
				index++;
			}

			for (; index < _maxInnerValue; index++)
				result.Add(nameof(IndicatorValue.Value) + (index + 1), null);

			return result;
		}
	}
}