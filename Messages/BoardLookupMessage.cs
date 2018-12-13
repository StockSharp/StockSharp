namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Message boards lookup for specified criteria.
	/// </summary>
	[DataContract]
	[Serializable]
	public class BoardLookupMessage : Message
	{
		/// <summary>
		/// The filter for board search.
		/// </summary>
		[DataMember]
		public string Like { get; set; }

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BoardLookupMessage"/>.
		/// </summary>
		public BoardLookupMessage()
			: base(MessageTypes.BoardLookup)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="BoardLookupMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new BoardLookupMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected BoardLookupMessage CopyTo(BoardLookupMessage destination)
		{
			destination.TransactionId = TransactionId;
			destination.Like = Like;

			this.CopyExtensionInfo(destination);

			return destination;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Like={Like},TrId={TransactionId}";
		}
	}
}