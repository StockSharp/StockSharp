namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="TransactionId"/> property.
/// </summary>
public interface ITransactionIdMessage
{
	/// <summary>
	/// Request identifier.
	/// </summary>
	long TransactionId { get; set; }
}