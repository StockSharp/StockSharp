namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="SecurityId"/> property.
/// </summary>
public interface ISecurityIdMessage
{
	/// <summary>
	/// Security ID.
	/// </summary>
	SecurityId SecurityId { get; set; }
}

/// <summary>
/// The interface describing an message with <see cref="SecurityId"/> property.
/// </summary>
public interface INullableSecurityIdMessage
{
	/// <summary>
	/// Security ID.
	/// </summary>
	SecurityId? SecurityId { get; set; }
}