namespace StockSharp.Messages;

/// <summary>
/// Margin modes.
/// </summary>
public enum MarginModes
{
	/// <summary>
	/// Cross margin mode.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CrossKey)]
	Cross,

	/// <summary>
	/// Isolated margin mode.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IsolatedKey)]
	Isolated,
}