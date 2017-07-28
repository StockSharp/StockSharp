namespace StockSharp.Algo.Export
{
	using System;

	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;

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
		public decimal ValueAsDecimal => Value.GetValue<decimal>();

		/// <summary>
		/// Value.
		/// </summary>
		public IIndicatorValue Value { get; set; }
	}
}