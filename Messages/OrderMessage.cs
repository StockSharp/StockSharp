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
	using System.ComponentModel.DataAnnotations;
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
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TransactionKey,
			Description = LocalizedStrings.TransactionIdKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public long TransactionId { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PortfolioKey,
			Description = LocalizedStrings.OrderPortfolioNameKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Order type.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.OrderTypeKey,
			Description = LocalizedStrings.OrderTypeDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public OrderTypes? OrderType { get; set; }

		/// <summary>
		/// User's order ID.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.UserIdKey,
			Description = LocalizedStrings.UserOrderIdKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string UserOrderId { get; set; }

		/// <inheritdoc />
		[DataMember]
		public string StrategyId { get; set; }

		/// <summary>
		/// Broker firm code.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BrokerKey,
			Description = LocalizedStrings.BrokerCodeKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string BrokerCode { get; set; }

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ClientCodeKey,
			Description = LocalizedStrings.ClientCodeDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string ClientCode { get; set; }

		/// <summary>
		/// Order condition (e.g., stop- and algo- orders parameters).
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ConditionKey,
			Description = LocalizedStrings.OrderConditionDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		[XmlIgnore]
		public OrderCondition Condition { get; set; }

		/// <summary>
		/// Placed order comment.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CommentKey,
			Description = LocalizedStrings.OrderCommentKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string Comment { get; set; }

		/// <summary>
		/// Margin mode.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.MarginKey,
			Description = LocalizedStrings.MarginModeKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public MarginModes? MarginMode { get; set; }

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
			destination.MarginMode = MarginMode;
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
			var str = base.ToString() + $",TransId={TransactionId},OrdType={OrderType},Pf={PortfolioName}(ClCode={ClientCode}),Cond={Condition},MR={MarginMode}";

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