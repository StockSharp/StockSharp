namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Market data request finished message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class MarketDataFinishedMessage : Message, IOriginalTransactionIdMessage
	{
		/// <inheritdoc />
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Initialize <see cref="MarketDataFinishedMessage"/>.
		/// </summary>
		public MarketDataFinishedMessage()
			: base(MessageTypes.MarketDataFinished)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="MarketDataFinishedMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new MarketDataFinishedMessage
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