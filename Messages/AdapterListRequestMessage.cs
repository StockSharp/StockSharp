namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Adapters list request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class AdapterListRequestMessage : Message
	{
		/// <summary>
		/// Initialize <see cref="AdapterListRequestMessage"/>.
		/// </summary>
		public AdapterListRequestMessage()
			: base(MessageTypes.AdapterListRequest)
		{
		}

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Create a copy of <see cref="AdapterListRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new AdapterListRequestMessage
			{
				TransactionId = TransactionId,
			};
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $",TrId={TransactionId}";
	}
}