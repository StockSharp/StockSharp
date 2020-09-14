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

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// A message containing info about the order.
	/// </summary>
	[DataContract]
	[Serializable]
	public abstract class OrderMessage : SecurityMessage,
		ITransactionIdMessage, IPortfolioNameMessage, IStrategyIdMessage
	{
		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <inheritdoc />
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

		/// <inheritdoc />
		[DataMember]
		public string StrategyId { get; set; }

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
		/// Placed order comment.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str135Key)]
		[DescriptionLoc(LocalizedStrings.Str136Key)]
		[MainCategory]
		public string Comment { get; set; }

		/// <summary>
		/// Is margin enabled.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarginKey)]
		[DescriptionLoc(LocalizedStrings.IsMarginKey)]
		[MainCategory]
		public bool? IsMargin { get; set; }

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public void CopyTo(OrderMessage destination)
		{
			base.CopyTo(destination);

			destination.TransactionId = TransactionId;
			destination.PortfolioName = PortfolioName;
			destination.OrderType = OrderType;
			destination.UserOrderId = UserOrderId;
			destination.StrategyId = StrategyId;
			destination.BrokerCode = BrokerCode;
			destination.ClientCode = ClientCode;
			destination.Condition = Condition?.Clone();
			destination.Comment = Comment;
			destination.IsMargin = IsMargin;
		}

		/// <summary>
		/// Initialize <see cref="OrderMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected OrderMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",TransId={TransactionId},OrdType={OrderType},Pf={PortfolioName}(ClCode={ClientCode}),Cond={Condition},MR={IsMargin}";

			if (!Comment.IsEmpty())
				str += $",Comment={Comment}";

			if (!UserOrderId.IsEmpty())
				str += $",UID={UserOrderId}";

			if (!StrategyId.IsEmpty())
				str += $",Strategy={StrategyId}";

			if (!BrokerCode.IsEmpty())
				str += $",BrID={BrokerCode}";

			return str;
		}
	}
}