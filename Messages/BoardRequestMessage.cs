namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Board request message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).
	/// </summary>
	[DataContract]
	[Serializable]
	public class BoardRequestMessage : Message
	{
		/// <summary>
		/// Board code.
		/// </summary>
		[DataMember]
		public string BoardCode { get; set; }

		/// <summary>
		/// The message is subscription.
		/// </summary>
		[DataMember]
		public bool IsSubscribe { get; set; }

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// ID of the original message <see cref="TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Subscribe or unsubscribe error info. To be set if the answer.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BoardRequestMessage"/>.
		/// </summary>
		public BoardRequestMessage()
			: base(MessageTypes.BoardRequest)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="BoardRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new BoardRequestMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected BoardRequestMessage CopyTo(BoardRequestMessage destination)
		{
			destination.BoardCode = BoardCode;
			destination.IsSubscribe = IsSubscribe;
			destination.TransactionId = TransactionId;
			destination.OriginalTransactionId = OriginalTransactionId;
			destination.Error = Error;

			this.CopyExtensionInfo(destination);

			return destination;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Code={BoardCode},IsSubscribe={IsSubscribe},TrId={TransactionId},Origin={OriginalTransactionId}";
		}
	}
}