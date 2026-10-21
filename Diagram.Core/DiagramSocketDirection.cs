namespace StockSharp.Diagram;

/// <summary>
/// Directions of connections.
/// </summary>
public enum DiagramSocketDirection
{
	/// <summary>
	/// Incoming.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.InputKey)]
	In,

	/// <summary>
	/// Outgoing.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OutputKey)]
	Out,
}