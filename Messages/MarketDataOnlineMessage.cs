namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Market data subscription goes online message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class MarketDataOnlineMessage : Message
	{
		/// <summary>
		/// ID of the original message <see cref="MarketDataMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Initialize <see cref="MarketDataOnlineMessage"/>.
		/// </summary>
		public MarketDataOnlineMessage()
			: base(MessageTypes.MarketDataOnline)
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