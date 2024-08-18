namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="OriginalTransactionId"/> property.
/// </summary>
public interface IOriginalTransactionIdMessage
{
	/// <summary>
	/// ID of the original message <see cref="ITransactionIdMessage.TransactionId"/> for which this message is a response.
	/// </summary>
	long OriginalTransactionId { get; set; }
}