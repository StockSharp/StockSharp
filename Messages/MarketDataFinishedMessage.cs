namespace StockSharp.Messages
{
	using System.Runtime.Serialization;

	/// <summary>
	/// Market data request finished message.
	/// </summary>
	public class MarketDataFinishedMessage : Message
	{
		/// <summary>
		/// ID of the original message <see cref="MarketDataMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Contains history market data.
		/// </summary>
		[DataMember]
		public bool IsHistory { get; set; }

		/// <summary>
		/// Initialize <see cref="Message"/>.
		/// </summary>
		public MarketDataFinishedMessage()
			: base(MessageTypes.MarketDataFinished)
		{
		}
	}
}