namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	/// <summary>
	/// Base result message.
	/// </summary>
	/// <typeparam name="TMessage">Message type.</typeparam>
	[DataContract]
	[Serializable]
	public abstract class BaseResultMessage<TMessage> : Message
		where TMessage : BaseResultMessage<TMessage>, new()
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseResultMessage{TMessage}"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected BaseResultMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// ID of the original message <see cref="ITransactionIdMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Error info.
		/// </summary>
		[DataMember]
		[XmlIgnore]
		public Exception Error { get; set; }

		/// <summary>
		/// Create a copy of <see cref="BaseResultMessage{TMessage}"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new TMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		protected virtual void CopyTo(TMessage destination)
		{
			base.CopyTo(destination);

			destination.OriginalTransactionId = OriginalTransactionId;
			destination.Error = Error;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",Orig={OriginalTransactionId}";

			if (Error != null)
				str += $",Error={Error.Message}";

			return str;
		}
	}
}