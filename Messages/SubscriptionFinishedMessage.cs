namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Market data request finished message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class SubscriptionFinishedMessage : Message, IOriginalTransactionIdMessage
	{
		/// <inheritdoc />
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Initialize <see cref="SubscriptionFinishedMessage"/>.
		/// </summary>
		public SubscriptionFinishedMessage()
			: base(MessageTypes.SubscriptionFinished)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="SubscriptionFinishedMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new SubscriptionFinishedMessage
			{
				OriginalTransactionId = OriginalTransactionId,
			};

			CopyTo(clone);

			return clone;
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $",OrigTransId={OriginalTransactionId}";
	}
}