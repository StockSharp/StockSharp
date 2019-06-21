namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security route response message.
	/// </summary>
	[Serializable]
	[DataContract]
	public abstract class BaseRouteMessage<TMessage> : BaseResultMessage<TMessage>
		where TMessage : BaseRouteMessage<TMessage>, new()
	{
		/// <summary>
		/// Initialize <see cref="BaseRouteMessage{TMessage}"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected BaseRouteMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Adapter identifier.
		/// </summary>
		[DataMember]
		public Guid AdapterId { get; set; }

		/// <inheritdoc />
		protected override void CopyTo(TMessage destination)
		{
			base.CopyTo(destination);

			destination.AdapterId = AdapterId;
		}
	}
}