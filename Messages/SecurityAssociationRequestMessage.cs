namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security association request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityAssociationRequestMessage : Message
	{
		/// <summary>
		/// Initialize <see cref="SecurityAssociationRequestMessage"/>.
		/// </summary>
		public SecurityAssociationRequestMessage()
			: base(MessageTypes.SecurityAssociationRequest)
		{
		}

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Create a copy of <see cref="SecurityAssociationRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new SecurityAssociationRequestMessage
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