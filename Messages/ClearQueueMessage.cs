namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Clear message queue message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ClearQueueMessage : Message
	{
		/// <summary>
		/// Type of messages that should be deleted. If the value is <see langword="null" />, all messages will be deleted.
		/// </summary>
		public MessageTypes? ClearMessageType { get; set; }

		/// <summary>
		/// Security id. If the value is <see langword="null" />, all messages for the security will be deleted.
		/// </summary>
		[DataMember]
		public SecurityId? SecurityId { get; set; }

		/// <summary>
		/// An additional argument for the market data filter.
		/// </summary>
		[DataMember]
		public object Arg { get; set; }

		/// <summary>
		/// Initialize <see cref="ClearQueueMessage"/>.
		/// </summary>
		public ClearQueueMessage()
			: base(MessageTypes.ClearQueue)
		{
		}
	}
}
