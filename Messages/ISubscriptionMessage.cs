namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// The interface describing an message with <see cref="IsSubscribe"/> property.
	/// </summary>
	public interface ISubscriptionMessage : ITransactionIdMessage, IOriginalTransactionIdMessage
	{
		/// <summary>
		/// Message contains fields with non default values.
		/// </summary>
		bool FilterEnabled { get; }

		/// <summary>
		/// Start date, from which data needs to be retrieved.
		/// </summary>
		DateTimeOffset? From { get; set; }

		/// <summary>
		/// End date, until which data needs to be retrieved.
		/// </summary>
		DateTimeOffset? To { get; set; }

		/// <summary>
		/// The message is subscription.
		/// </summary>
		bool IsSubscribe { get; set; }

		/// <summary>
		/// Skip count.
		/// </summary>
		long? Skip { get; set; }

		/// <summary>
		/// Max count.
		/// </summary>
		long? Count { get; set; }

		/// <summary>
		/// Data type info.
		/// </summary>
		DataType DataType { get; }
	}
}