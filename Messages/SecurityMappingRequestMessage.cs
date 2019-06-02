namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security mapping request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityMappingRequestMessage : Message
	{
		/// <summary>
		/// Initialize <see cref="SecurityMappingRequestMessage"/>.
		/// </summary>
		public SecurityMappingRequestMessage()
			: base(MessageTypes.SecurityMappingRequest)
		{
		}

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Create a copy of <see cref="SecurityMappingRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new SecurityMappingRequestMessage
			{
				TransactionId = TransactionId,
			};
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",TrId={TransactionId}";
		}
	}
}