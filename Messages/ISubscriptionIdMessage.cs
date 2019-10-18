namespace StockSharp.Messages
{
	using System.Collections.Generic;

	/// <summary>
	/// The interface describing an message with <see cref="SubscriptionId"/> property.
	/// </summary>
	public interface ISubscriptionIdMessage
	{
		/// <summary>
		/// ID of the original message <see cref="ITransactionIdMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		long OriginalTransactionId { get; set; }

		/// <summary>
		/// Subscription id.
		/// </summary>
		long SubscriptionId { get; set; }

		/// <summary>
		/// Identifiers.
		/// </summary>
		IEnumerable<long> SubscriptionIds { get; set; }
	}
}