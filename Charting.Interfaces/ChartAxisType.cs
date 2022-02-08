namespace StockSharp.Charting
{
	using System.ComponentModel.DataAnnotations;

	using StockSharp.Localization;

	/// <summary>
	/// Chart axes types.
	/// </summary>
	public enum ChartAxisType
	{
		/// <summary>
		/// Time.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TimeKey)]
		DateTime,

		/// <summary>
		/// Time without breaks.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1915Key)]
		CategoryDateTime,

		/// <summary>
		/// Number.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1916Key)]
		Numeric
	}
}