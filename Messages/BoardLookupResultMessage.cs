namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Boards search result message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class BoardLookupResultMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BoardLookupResultMessage"/>.
		/// </summary>
		public BoardLookupResultMessage()
			: base(MessageTypes.BoardLookupResult)
		{
		}

		/// <summary>
		/// ID of the original message <see cref="BoardLookupMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Lookup error info.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Create a copy of <see cref="BoardLookupResultMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new BoardLookupResultMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected BoardLookupResultMessage CopyTo(BoardLookupResultMessage destination)
		{
			destination.OriginalTransactionId = OriginalTransactionId;
			destination.Error = Error;

			this.CopyExtensionInfo(destination);

			return destination;
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