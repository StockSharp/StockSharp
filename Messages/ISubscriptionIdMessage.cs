namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="SubscriptionId"/> property.
/// </summary>
public interface ISubscriptionIdMessage : IOriginalTransactionIdMessage
{
	/// <summary>
	/// Subscription id.
	/// </summary>
	long SubscriptionId { get; set; }

	/// <summary>
	/// Identifiers.
	/// </summary>
	long[] SubscriptionIds { get; set; }

	/// <summary>
	/// Data type info.
	/// </summary>
	DataType DataType { get; }
}