#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderStatusMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// A message requesting current registered orders and trades.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderStatusMessage : OrderCancelMessage
	{
		/// <summary>
		/// Start date, from which data needs to be retrieved.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str343Key)]
		[DescriptionLoc(LocalizedStrings.Str344Key)]
		[MainCategory]
		public DateTimeOffset? From { get; set; }

		/// <summary>
		/// End date, until which data needs to be retrieved.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str345Key)]
		[DescriptionLoc(LocalizedStrings.Str346Key)]
		[MainCategory]
		public DateTimeOffset? To { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderStatusMessage"/>.
		/// </summary>
		public OrderStatusMessage()
			: base(MessageTypes.OrderStatus)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="OrderStatusMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new OrderStatusMessage
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
				From = From,
				To = To
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
			return base.ToString() + $",TransId={TransactionId}";
		}
	}
}