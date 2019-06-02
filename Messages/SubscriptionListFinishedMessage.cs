namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Subscriptions result message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SubscriptionListFinishedMessage : BaseResultMessage<SubscriptionListFinishedMessage>
	{
		/// <summary>
		/// Initialize <see cref="SubscriptionListFinishedMessage"/>.
		/// </summary>
		public SubscriptionListFinishedMessage()
			: base(MessageTypes.SubscriptionListFinished)
		{
		}
	}
}