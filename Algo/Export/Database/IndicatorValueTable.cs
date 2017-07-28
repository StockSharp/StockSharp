namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

	class IndicatorValueTable : Table<IndicatorValue>
	{
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
			yield return new ColumnDescription(nameof(IndicatorValue.Value)) { DbType = typeof(decimal), ValueRestriction = new DecimalRestriction { Precision = 10, Scale = 6 } };
		}

		protected override IDictionary<string, object> ConvertToParameters(IndicatorValue value)
		{
			var secId = value.Security?.ToSecurityId();

			var result = new Dictionary<string, object>
			{
				{ nameof(SecurityId.SecurityCode), secId?.SecurityCode },
				{ nameof(SecurityId.BoardCode), secId?.BoardCode },
				{ nameof(IndicatorValue.Time), value.Time },
				{ nameof(IndicatorValue.Value), value.ValueAsDecimal },
			};
			return result;
		}
	}
}