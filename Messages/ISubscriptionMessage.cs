namespace StockSharp.Messages
{
	/// <summary>
	/// The interface describing an message with <see cref="IsSubscribe"/> property.
	/// </summary>
	public interface ISubscriptionMessage : ISubscriptionIdMessage, ITransactionIdMessage
	{
		/// <summary>
		/// The message is subscription.
		/// </summary>
		bool IsSubscribe { get; set; }

		/// <summary>
		/// ID of the original message <see cref="ITransactionIdMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		long OriginalTransactionId { get; set; }

		/// <summary>
		/// Request historical data only.
		/// </summary>
		bool IsHistory { get; set; }
	}
}