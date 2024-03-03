namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="Body"/> property.
/// </summary>
public interface IFileMessage : IOriginalTransactionIdMessage
{
	/// <summary>
	/// File body.
	/// </summary>
	byte[] Body { get; set; }
}