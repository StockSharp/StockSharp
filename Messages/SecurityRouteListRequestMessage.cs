namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security routes list request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityRouteListRequestMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initialize <see cref="SecurityRouteListRequestMessage"/>.
		/// </summary>
		public SecurityRouteListRequestMessage()
			: base(MessageTypes.SecurityRouteListRequest)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.SecurityRoute;

		/// <summary>
		/// Create a copy of <see cref="SecurityRouteListRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new SecurityRouteListRequestMessage();
			CopyTo(clone);
			return clone;
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $",TrId={TransactionId}";

		/// <inheritdoc />
		[DataMember]
		public override DateTimeOffset? From => null;

		/// <inheritdoc />
		[DataMember]
		public override DateTimeOffset? To => DateTimeOffset.MaxValue /* prevent for online mode */;

		/// <inheritdoc />
		[DataMember]
		public override bool IsSubscribe => true;

		/// <inheritdoc />
		[DataMember]
		public override long OriginalTransactionId => 0;
	}
}