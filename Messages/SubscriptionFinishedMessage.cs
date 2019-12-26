namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Market data request finished message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class SubscriptionFinishedMessage : BaseResultMessage<SubscriptionFinishedMessage>
	{
		/// <summary>
		/// Initialize <see cref="SubscriptionFinishedMessage"/>.
		/// </summary>
		public SubscriptionFinishedMessage()
			: base(MessageTypes.SubscriptionFinished)
		{
		}
	}
}