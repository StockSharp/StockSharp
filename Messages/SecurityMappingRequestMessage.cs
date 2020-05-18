namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security mapping request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityMappingRequestMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initialize <see cref="SecurityMappingRequestMessage"/>.
		/// </summary>
		public SecurityMappingRequestMessage()
			: base(MessageTypes.SecurityMappingRequest)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.Create(typeof(SecurityMappingMessage), null);

		/// <summary>
		/// Create a copy of <see cref="SecurityMappingRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new SecurityMappingRequestMessage();
			CopyTo(clone);
			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",TrId={TransactionId}";
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