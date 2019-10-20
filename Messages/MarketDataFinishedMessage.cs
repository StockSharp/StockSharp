namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Serialization;

	/// <summary>
	/// Market data request finished message.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
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
		//[DataMember]
		[Obsolete]
		[XmlIgnore]
		[Ignore]
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
			var clone = new MarketDataFinishedMessage
			{
				OriginalTransactionId = OriginalTransactionId,
				//IsHistory = IsHistory,
			};

			CopyTo(clone);

			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",OrigTransId={OriginalTransactionId}";
		}
	}
}