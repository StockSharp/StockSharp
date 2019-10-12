#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// A message containing info about the order.
	/// </summary>
	[DataContract]
	[Serializable]
	public abstract class OrderMessage : SecurityMessage, ITransactionIdMessage
	{
		/// <summary>
		/// Transaction ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <summary>
		/// Portfolio name, for which an order must be placed/cancelled.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.Str229Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Order type.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str132Key)]
		[DescriptionLoc(LocalizedStrings.Str133Key)]
		[MainCategory]
		public OrderTypes? OrderType { get; set; }

		/// <summary>
		/// User's order ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str165Key)]
		[DescriptionLoc(LocalizedStrings.Str166Key)]
		[MainCategory]
		public string UserOrderId { get; set; }

		/// <summary>
		/// Broker firm code.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str2593Key)]
		[DisplayNameLoc(LocalizedStrings.BrokerKey)]
		[DescriptionLoc(LocalizedStrings.Str2619Key)]
		public string BrokerCode { get; set; }

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str2593Key)]
		[DisplayNameLoc(LocalizedStrings.ClientCodeKey)]
		[DescriptionLoc(LocalizedStrings.ClientCodeDescKey)]
		public string ClientCode { get; set; }

		/// <summary>
		/// Order condition (e.g., stop- and algo- orders parameters).
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str154Key)]
		[DescriptionLoc(LocalizedStrings.Str155Key)]
		[CategoryLoc(LocalizedStrings.Str156Key)]
		[XmlIgnore]
		public OrderCondition Condition { get; set; }

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		protected virtual void CopyTo(OrderMessage destination)
		{
			base.CopyTo(destination);

			destination.TransactionId = TransactionId;
			destination.PortfolioName = PortfolioName;
			destination.OrderType = OrderType;
			destination.UserOrderId = UserOrderId;
			destination.BrokerCode = BrokerCode;
			destination.ClientCode = ClientCode;
			destination.Condition = Condition?.Clone();
		}

		/// <summary>
		/// Initialize <see cref="OrderMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected OrderMessage(MessageTypes type)
			: base(type)
		{
		}
	}
}