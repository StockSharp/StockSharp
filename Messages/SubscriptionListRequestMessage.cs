namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Subscriptions list request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SubscriptionListRequestMessage : Message, ITransactionIdMessage
	{
		/// <summary>
		/// Initialize <see cref="SubscriptionListRequestMessage"/>.
		/// </summary>
		public SubscriptionListRequestMessage()
			: base(MessageTypes.SubscriptionListRequest)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Create a copy of <see cref="SubscriptionListRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new SubscriptionListRequestMessage
			{
				TransactionId = TransactionId,
			};
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $",TrId={TransactionId}";
	}
}