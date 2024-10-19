namespace StockSharp.Charting;

/// <summary>
/// Orders display filter.
/// </summary>
public enum ChartOrderDisplayFilter
{
	/// <summary>All orders.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AllOrdersKey)]
	All,

	/// <summary>Orders with errors.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ErrorOrdersOnlyKey)]
	ErrorsOnly,

	/// <summary>Orders with no errors.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NoErrorOrdersOnlyKey)]
	NoErrorsOnly
}