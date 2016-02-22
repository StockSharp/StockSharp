#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderRegisterMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The message containing the information for the order registration.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class OrderRegisterMessage : OrderMessage
	{
		/// <summary>
		/// Order price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.OrderPriceKey)]
		[MainCategory]
		public decimal Price { get; set; }

		/// <summary>
		/// Number of contracts in an order.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.OrderVolumeKey)]
		[MainCategory]
		public decimal Volume { get; set; }

		/// <summary>
		/// Visible quantity of contracts in order.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VisibleVolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str127Key)]
		[MainCategory]
		[Nullable]
		public decimal? VisibleVolume { get; set; }

		/// <summary>
		/// Order side (buy or sell).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str129Key)]
		[MainCategory]
		public Sides Side { get; set; }

		/// <summary>
		/// Placed order comment.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str135Key)]
		[DescriptionLoc(LocalizedStrings.Str136Key)]
		[MainCategory]
		public string Comment { get; set; }

		/// <summary>
		/// Order expiry time. The default is <see langword="null" />, which mean (GTC).
		/// </summary>
		/// <remarks>
		/// If the value is equal <see langword="null" /> or <see cref="DateTimeOffset.MaxValue"/>, order will be GTC (good til cancel). Or uses exact date.
		/// </remarks>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str141Key)]
		[DescriptionLoc(LocalizedStrings.Str142Key)]
		[MainCategory]
		public DateTimeOffset? TillDate { get; set; }

		/// <summary>
		/// Order condition (e.g., stop- and algo- orders parameters).
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str154Key)]
		[DescriptionLoc(LocalizedStrings.Str155Key)]
		[CategoryLoc(LocalizedStrings.Str156Key)]
		public OrderCondition Condition { get; set; }

		/// <summary>
		/// Limit order time in force.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TimeInForceKey)]
		[DescriptionLoc(LocalizedStrings.Str232Key)]
		[MainCategory]
		[Nullable]
		public TimeInForce? TimeInForce { get; set; }

		/// <summary>
		/// Information for REPO\REPO-M orders.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str233Key)]
		[DescriptionLoc(LocalizedStrings.Str234Key)]
		[MainCategory]
		public RepoOrderInfo RepoInfo { get; set; }

		/// <summary>
		/// Information for Negotiate Deals Mode orders.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str235Key)]
		[DescriptionLoc(LocalizedStrings.Str236Key)]
		[MainCategory]
		public RpsOrderInfo RpsInfo { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderRegisterMessage"/>.
		/// </summary>
		public OrderRegisterMessage()
			: base(MessageTypes.OrderRegister)
		{
		}

		/// <summary>
		/// Initialize <see cref="OrderRegisterMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected OrderRegisterMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="OrderRegisterMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new OrderRegisterMessage(Type)
			{
				Comment = Comment,
				Condition = Condition,
				TillDate = TillDate,
				OrderType = OrderType,
				PortfolioName = PortfolioName,
				Price = Price,
				RepoInfo = RepoInfo.CloneNullable(),
				RpsInfo = RpsInfo.CloneNullable(),
				SecurityId = SecurityId,
				//SecurityType = SecurityType,
				Side = Side,
				TimeInForce = TimeInForce,
				TransactionId = TransactionId,
				VisibleVolume = VisibleVolume,
				Volume = Volume,
				Currency = Currency,
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
			return base.ToString() + ",TransId={0},Price={1},Side={2},OrdType={3},Vol={4},Sec={5}".Put(TransactionId, Price, Side, OrderType, Volume, SecurityId);
		}
	}
}