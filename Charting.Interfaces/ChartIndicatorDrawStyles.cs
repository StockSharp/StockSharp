namespace StockSharp.Charting
{
	using System.ComponentModel.DataAnnotations;

	using StockSharp.Localization;

	/// <summary>
	/// Indicator chart drawing styles.
	/// </summary>
	public enum ChartIndicatorDrawStyles
	{
		/// <summary>
		/// Line.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1898Key)]
		Line,

		/// <summary>
		/// Line (no gaps).
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1972Key)]
		NoGapLine,

		/// <summary>
		/// Stepped line.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1973Key)]
		StepLine,

		/// <summary>
		/// Band.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1974Key)]
		Band,

		/// <summary>
		/// The range with a single value.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1974_2Key)]
		BandOneValue,

		/// <summary>
		/// Dot.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1975Key)]
		Dot,

		/// <summary>
		/// Histogram.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1976Key)]
		Histogram,

		/// <summary>
		/// Bubble.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1977Key)]
		Bubble,

		/// <summary>
		/// Stacked bar chart.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1978Key)]
		StackedBar,

		/// <summary>
		/// Dashed line.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DashedLineKey)]
		DashedLine,

		/// <summary>
		/// Area.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AreaKey)]
		Area,
	}
}
