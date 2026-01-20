namespace StockSharp.Messages;

/// <summary>
/// The interface describing a message with <see cref="SecurityIds"/> property.
/// </summary>
public interface ISecurityIdsMessage
{
	/// <summary>
	/// Security identifiers for filtering.
	/// </summary>
	SecurityId[] SecurityIds { get; set; }
}
