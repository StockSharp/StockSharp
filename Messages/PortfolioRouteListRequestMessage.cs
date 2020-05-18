namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Portfolio routes list request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class PortfolioRouteListRequestMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initialize <see cref="PortfolioRouteListRequestMessage"/>.
		/// </summary>
		public PortfolioRouteListRequestMessage()
			: base(MessageTypes.PortfolioRouteListRequest)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.Create(typeof(PortfolioRouteMessage), null);

		/// <summary>
		/// Create a copy of <see cref="PortfolioRouteListRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new PortfolioRouteListRequestMessage();

			CopyTo(clone);

			return clone;
		}

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