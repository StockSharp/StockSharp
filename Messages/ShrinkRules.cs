namespace StockSharp.Messages;

/// <summary>
/// Price rounding rules.
/// </summary>
public enum ShrinkRules
{
	/// <summary>
	/// Automatically to determine rounding to lesser or to bigger value.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AutoKey)]
	Auto,

	/// <summary>
	/// To round to lesser value.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LessKey)]
	Less,

	/// <summary>
	/// To round to bigger value.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.GreaterKey)]
	More,
}