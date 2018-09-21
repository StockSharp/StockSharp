#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderCancelMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// A message containing the data for the cancellation of the order.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderCancelMessage : OrderMessage
	{
		/// <summary>
		/// ID cancellation order.
		/// </summary>
		[DataMember]
		public long? OrderId { get; set; }

		/// <summary>
		/// Cancelling order id (as a string if the electronic board does not use a numeric representation of the identifiers).
		/// </summary>
		[DataMember]
		public string OrderStringId { get; set; }

		/// <summary>
		/// Transaction ID cancellation order.
		/// </summary>
		[DataMember]
		public long OrderTransactionId { get; set; }

		/// <summary>
		/// Cancelling volume. If not specified, then it canceled the entire balance.
		/// </summary>
		[DataMember]
		public decimal? Volume { get; set; }

		/// <summary>
		/// Order side.
		/// </summary>
		[DataMember]
		public Sides? Side { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderCancelMessage"/>.
		/// </summary>
		public OrderCancelMessage()
			: base(MessageTypes.OrderCancel)
		{
		}

		/// <summary>
		/// Initialize <see cref="OrderCancelMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected OrderCancelMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="OrderCancelMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new OrderCancelMessage
			{
				OrderId = OrderId,
				OrderStringId = OrderStringId,
				TransactionId = TransactionId,
				OrderTransactionId = OrderTransactionId,
				Volume = Volume,
				OrderType = OrderType,
				PortfolioName = PortfolioName,
				SecurityId = SecurityId,
				Side = Side,
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
			return base.ToString() + $",OrderTransId={OrderTransactionId},TransId={TransactionId},OrderId={OrderId}";
		}
	}
}