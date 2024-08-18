namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="SeqNum"/> property.
/// </summary>
public interface ISeqNumMessage
{
	/// <summary>
	/// Sequence number.
	/// </summary>
	/// <remarks>Zero means no information.</remarks>
	long SeqNum { get; set; }
}