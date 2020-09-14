namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Base implementation of <see cref="ISubscriptionMessage"/> interface.
	/// </summary>
	[DataContract]
	[Serializable]
	public abstract class BaseSubscriptionMessage : Message, ISubscriptionMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseSubscriptionMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		public BaseSubscriptionMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <inheritdoc />
		public virtual bool FilterEnabled => false;

		/// <inheritdoc />
		[DataMember]
		public virtual DateTimeOffset? From { get; set; }
		
		/// <inheritdoc />
		[DataMember]
		public virtual DateTimeOffset? To { get; set; }

		/// <inheritdoc />
		[DataMember]
		public virtual long? Skip { get; set; }

		/// <inheritdoc />
		[DataMember]
		public virtual long? Count { get; set; }
		
		/// <inheritdoc />
		[DataMember]
		public virtual bool IsSubscribe { get; set; }
		
		/// <inheritdoc />
		[DataMember]
		public virtual long TransactionId { get; set; }
		
		/// <inheritdoc />
		[DataMember]
		public virtual long OriginalTransactionId { get; set; }

		/// <inheritdoc />
		public abstract DataType DataType { get; }

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected BaseSubscriptionMessage CopyTo(BaseSubscriptionMessage destination)
		{
			base.CopyTo(destination);

			destination.From = From;
			destination.To = To;
			destination.Skip = Skip;
			destination.Count = Count;
			destination.IsSubscribe = IsSubscribe;
			destination.TransactionId = TransactionId;
			destination.OriginalTransactionId = OriginalTransactionId;

			return destination;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",TrId={TransactionId}";

			if (Skip != default)
				str += $",Skip={Skip}";

			if (Count != default)
				str += $",Cnt={Count}";

			if (From != default)
				str += $",From={From}";

			if (To != default)
				str += $",To={To}";

			return str;
		}
	}
}