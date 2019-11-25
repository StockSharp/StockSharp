namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Market data subscription goes online message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class MarketDataOnlineMessage : Message, IOriginalTransactionIdMessage
	{
		/// <inheritdoc />
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
		/// Create a copy of <see cref="MarketDataOnlineMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new MarketDataOnlineMessage
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