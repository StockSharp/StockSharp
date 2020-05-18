namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Message boards lookup for specified criteria.
	/// </summary>
	[DataContract]
	[Serializable]
	public class BoardLookupMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// The filter for board search.
		/// </summary>
		[DataMember]
		public string Like { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BoardLookupMessage"/>.
		/// </summary>
		public BoardLookupMessage()
			: base(MessageTypes.BoardLookup)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.Board;

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
			base.CopyTo(destination);

			destination.Like = Like;

			return destination;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Like={Like}";
		}

		/// <inheritdoc />
		[DataMember]
		public override DateTimeOffset? From => null;

		/// <inheritdoc />
		[DataMember]
		public override DateTimeOffset? To => DateTimeOffset.MaxValue /* prevent for online mode */;

		/// <inheritdoc />
		[DataMember]
		public override bool IsSubscribe => true;

		/// <inheritdoc />
		[DataMember]
		public override long OriginalTransactionId => 0;
	}
}