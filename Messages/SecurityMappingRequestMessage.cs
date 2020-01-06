namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security mapping request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityMappingRequestMessage : Message, ISubscriptionMessage
	{
		/// <summary>
		/// Initialize <see cref="SecurityMappingRequestMessage"/>.
		/// </summary>
		public SecurityMappingRequestMessage()
			: base(MessageTypes.SecurityMappingRequest)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Create a copy of <see cref="SecurityMappingRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new SecurityMappingRequestMessage
			{
				TransactionId = TransactionId,
			};

			CopyTo(clone);

			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",TrId={TransactionId}";
		}

		DateTimeOffset? ISubscriptionMessage.From
		{
			get => null;
			set { }
		}

		DateTimeOffset? ISubscriptionMessage.To
		{
			// prevent for online mode
			get => DateTimeOffset.MaxValue;
			set { }
		}

		bool ISubscriptionMessage.IsSubscribe
		{
			get => true;
			set { }
		}

		long IOriginalTransactionIdMessage.OriginalTransactionId
		{
			get => 0;
			set { }
		}
	}
}