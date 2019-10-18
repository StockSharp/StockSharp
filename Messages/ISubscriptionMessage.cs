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
		/// Request historical data only.
		/// </summary>
		bool IsHistory { get; set; }
	}
}