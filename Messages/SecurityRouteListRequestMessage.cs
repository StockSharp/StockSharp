namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security routes list request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityRouteListRequestMessage : Message
	{
		/// <summary>
		/// Initialize <see cref="SecurityRouteListRequestMessage"/>.
		/// </summary>
		public SecurityRouteListRequestMessage()
			: base(MessageTypes.SecurityRouteListRequest)
		{
		}

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Create a copy of <see cref="SecurityRouteListRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new SecurityRouteListRequestMessage
			{
				TransactionId = TransactionId,
			};
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $",TrId={TransactionId}";
	}
}