namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// The message containing the information for modify order.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderReplaceMessage : OrderRegisterMessage
	{
		/// <summary>
		/// Modified order id.
		/// </summary>
		[DataMember]
		public long? OldOrderId { get; set; }

		/// <summary>
		/// Modified order id (as a string if the electronic board does not use a numeric representation of the identifiers).
		/// </summary>
		[DataMember]
		public string OldOrderStringId { get; set; }

		/// <summary>
		/// Modified order transaction id.
		/// </summary>
		[DataMember]
		public long OldTransactionId { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderReplaceMessage"/>.
		/// </summary>
		public OrderReplaceMessage()
			: base(MessageTypes.OrderReplace)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="OrderReplaceMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new OrderReplaceMessage
			{
				Comment = Comment,
				Condition = Condition,
				TillDate = TillDate,
				OrderType = OrderType,
				PortfolioName = PortfolioName,
				Price = Price,
				RepoInfo = RepoInfo,
				RpsInfo = RpsInfo,
				SecurityId = SecurityId,
				Side = Side,
				TimeInForce = TimeInForce,
				TransactionId = TransactionId,
				VisibleVolume = VisibleVolume,
				Volume = Volume,
				OldOrderId = OldOrderId,
				OldOrderStringId = OldOrderStringId,
				OldTransactionId = OldTransactionId,
				UserOrderId = UserOrderId,
				ClientCode = ClientCode,
				BrokerCode = BrokerCode,
			};

			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",OldTransId={0},OldOrdId={1},NewTransId={2}".Put(OldTransactionId, OldOrderId, TransactionId);
		}
	}
}