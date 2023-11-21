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
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Line2Key)]
		Line,

		/// <summary>
		/// Line (no gaps).
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NoGapLineKey)]
		NoGapLine,

		/// <summary>
		/// Stepped line.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StepLineKey)]
		StepLine,

		/// <summary>
		/// Band.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BandKey)]
		Band,

		/// <summary>
		/// The range with a single value.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BandOneValueKey)]
		BandOneValue,

		/// <summary>
		/// Dot.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DotStyleKey)]
		Dot,

		/// <summary>
		/// Histogram.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.HistogramKey)]
		Histogram,

		/// <summary>
		/// Bubble.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BubbleKey)]
		Bubble,

		/// <summary>
		/// Stacked bar chart.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StackedBarKey)]
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
