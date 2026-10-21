namespace StockSharp.Charting;

/// <summary>
/// Equity curve chart drawing styles.
/// </summary>
public enum LineChartStyles
{
	/// <summary>
	/// Chart drawing in the form of a fill area.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AreaKey)]
	Area,

	/// <summary>
	/// Chart drawing in the form of a line.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Line2Key)]
	Line,

	/// <summary>
	/// Chart drawing in the form of a dashed line.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DashedLineKey)]
	DashedLine
}