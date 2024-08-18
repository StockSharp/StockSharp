namespace StockSharp.Charting;

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
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TimeNoBreaksKey)]
	CategoryDateTime,

	/// <summary>
	/// Number.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NumericKey)]
	Numeric
}