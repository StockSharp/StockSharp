#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderReplaceMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

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
				OldOrderId = OldOrderId,
				OldOrderStringId = OldOrderStringId,
				OldTransactionId = OldTransactionId,
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
			return base.ToString() + $",OldTransId={OldTransactionId},OldOrdId={OldOrderId},NewTransId={TransactionId}";
		}
	}
}