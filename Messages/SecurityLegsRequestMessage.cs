namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security legs request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityLegsRequestMessage : Message, ISubscriptionMessage
	{
		/// <summary>
		/// Initialize <see cref="SecurityLegsRequestMessage"/>.
		/// </summary>
		public SecurityLegsRequestMessage()
			: base(MessageTypes.SecurityLegsRequest)
		{
		}

		/// <inheritdoc />
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