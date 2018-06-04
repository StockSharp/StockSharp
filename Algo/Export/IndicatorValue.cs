namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// Indicator value with time.
	/// </summary>
	public class IndicatorValue
	{
		/// <summary>
		/// Security.
		/// </summary>
		public Security Security { get; set; }

		/// <summary>
		/// Value time.
		/// </summary>
		public DateTimeOffset Time { get; set; }

		/// <summary>
		/// Converted to <see cref="decimal"/> type value.
		/// </summary>
		public decimal? ValueAsDecimal => ValuesAsDecimal.First();

		/// <summary>
		/// Converted to <see cref="decimal"/> type values.
		/// </summary>
		public IEnumerable<decimal?> ValuesAsDecimal
		{
			get
			{
				var values = new List<decimal?>(); 
				FillValues(Value, values);
				return values;
			}
		}

		private static void FillValues(IIndicatorValue value, ICollection<decimal?> values)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (value is DecimalIndicatorValue || value is CandleIndicatorValue || value is ShiftedIndicatorValue)
			{
				values.Add(value.IsEmpty ? (decimal?)null : value.GetValue<decimal>());
			}
			else if (value is ComplexIndicatorValue complexValue)
			{
				foreach (var innerIndicator in ((IComplexIndicator)value.Indicator).InnerIndicators)
				{
					var innerValue = complexValue.InnerValues.TryGetValue(innerIndicator);

					if (innerValue == null)
						values.Add(null);
					else
						FillValues(innerValue, values);
				}
			}
			else
				throw new ArgumentOutOfRangeException(nameof(value), value.GetType(), LocalizedStrings.Str1655);
		}

		/// <summary>
		/// Value.
		/// </summary>
		public IIndicatorValue Value { get; set; }
	}
}