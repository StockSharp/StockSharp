namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Market data request finished message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class SubscriptionFinishedMessage : BaseResultMessage<SubscriptionFinishedMessage>
	{
		/// <summary>
		/// Initialize <see cref="SubscriptionFinishedMessage"/>.
		/// </summary>
		public SubscriptionFinishedMessage()
			: base(MessageTypes.SubscriptionFinished)
		{
		}

		/// <summary>
		/// Recommended value for next <see cref="ISubscriptionMessage.From"/> (in case of partial requests).
		/// </summary>
		[DataMember]
		public DateTimeOffset? NextFrom { get; set; }

		/// <inheritdoc />
		protected override void CopyTo(SubscriptionFinishedMessage destination)
		{
			base.CopyTo(destination);

			destination.NextFrom = NextFrom;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString();

			if (NextFrom != null)
				str += $",Next={NextFrom}";

			return str;
		}
	}
}