namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// Subscription response message.
	/// </summary>
	public class SubscriptionResponseMessage : BaseResultMessage<SubscriptionResponseMessage>
	{
		/// <summary>
		/// Not supported error.
		/// </summary>
		public static NotSupportedException NotSupported = new NotSupportedException();

		/// <summary>
		/// Initialize <see cref="SubscriptionResponseMessage"/>.
		/// </summary>
		public SubscriptionResponseMessage()
			: base(MessageTypes.SubscriptionResponse)
		{
		}
	}
}