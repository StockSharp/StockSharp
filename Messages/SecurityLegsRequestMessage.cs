namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security legs request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityLegsRequestMessage : Message
	{
		/// <summary>
		/// Initialize <see cref="SecurityLegsRequestMessage"/>.
		/// </summary>
		public SecurityLegsRequestMessage()
			: base(MessageTypes.SecurityLegsRequest)
		{
		}

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// The filter for securities search.
		/// </summary>
		[DataMember]
		public string Like { get; set; }

		/// <summary>
		/// Create a copy of <see cref="SecurityLegsRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new SecurityLegsRequestMessage
			{
				TransactionId = TransactionId,
				Like = Like,
			};
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Like={Like},TrId={TransactionId}";
		}
	}
}