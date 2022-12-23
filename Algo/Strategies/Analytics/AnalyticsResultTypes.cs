namespace StockSharp.Algo.Strategies.Analytics;

using System.ComponentModel.DataAnnotations;

using StockSharp.Localization;

/// <summary>
/// Types of result.
/// </summary>
public enum AnalyticsResultTypes
{
	/// <summary>
	/// Table.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str3280Key)]
	Grid,

	/// <summary>
	/// Bubble chart.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1977Key)]
	Bubble,

	/// <summary>
	/// Histogram.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1976Key)]
	Histogram,

	/// <summary>
	/// Heatmap.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.HeatmapKey)]
	Heatmap,
}