namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Portfolio routes list request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class PortfolioRouteListRequestMessage : Message, ITransactionIdMessage
	{
		/// <summary>
		/// Initialize <see cref="PortfolioRouteListRequestMessage"/>.
		/// </summary>
		public PortfolioRouteListRequestMessage()
			: base(MessageTypes.PortfolioRouteListRequest)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Create a copy of <see cref="PortfolioRouteListRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new PortfolioRouteListRequestMessage
			{
				TransactionId = TransactionId,
			};

			CopyTo(clone);

			return clone;
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $",TrId={TransactionId}";
	}
}