namespace StockSharp.Messages
{
	using System.Collections.Generic;

	/// <summary>
	/// The interface describing an message with <see cref="SubscriptionId"/> property.
	/// </summary>
	public interface ISubscriptionIdMessage
	{
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