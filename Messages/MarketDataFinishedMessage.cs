namespace StockSharp.Messages
{
	using System;
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
		[Obsolete]
		public bool IsHistory { get; set; }

		/// <summary>
		/// Initialize <see cref="Message"/>.
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
			var msg = new MarketDataFinishedMessage
			{
				LocalTime = LocalTime,
				OriginalTransactionId = OriginalTransactionId,
				//IsHistory = IsHistory,
			};

			return msg;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",OrigTransId={OriginalTransactionId}";
		}
	}
}