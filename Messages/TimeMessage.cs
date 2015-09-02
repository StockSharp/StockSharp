namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// The message contains information about the current time.
	/// </summary>
	[DataContract]
	[Serializable]
	public sealed class TimeMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TimeMessage"/>.
		/// </summary>
		public TimeMessage()
			: base(MessageTypes.Time)
		{
		}

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public string TransactionId { get; set; }

		/// <summary>
		/// ID of the original message <see cref="TimeMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public string OriginalTransactionId { get; set; }

		/// <summary>
		/// Server time.
		/// </summary>
		[DataMember]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",ID={0},Response={1}".Put(TransactionId, OriginalTransactionId);
		}

		/// <summary>
		/// Create a copy of <see cref="TimeMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new TimeMessage
			{
				TransactionId = TransactionId,
				OriginalTransactionId = OriginalTransactionId,
				LocalTime = LocalTime,
				ServerTime = ServerTime,
			};
		}
	}
}