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
		/// Number of contracts in the order.
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
		/// If the value is equal <see langword="null" />, order will be GTC (good til cancel). Or uses exact date.
		/// </remarks>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str141Key)]
		[DescriptionLoc(LocalizedStrings.Str142Key)]
		[MainCategory]
		public DateTimeOffset? TillDate { get; set; }

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
		/// Is the order of market-maker.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarketMakerKey)]
		[DescriptionLoc(LocalizedStrings.MarketMakerOrderKey, true)]
		[MainCategory]
		public bool? IsMarketMaker { get; set; }

		/// <summary>
		/// Is margin enabled.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarginKey)]
		[DescriptionLoc(LocalizedStrings.IsMarginKey)]
		[MainCategory]
		public bool? IsMargin { get; set; }

		/// <summary>
		/// Slippage in trade price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str163Key)]
		[DescriptionLoc(LocalizedStrings.Str164Key)]
		public decimal? Slippage { get; set; }

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
			var clone = new OrderRegisterMessage(Type);

			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public void CopyTo(OrderRegisterMessage destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			destination.Comment = Comment;
			destination.Condition = Condition?.Clone();
			destination.TillDate = TillDate;
			destination.OrderType = OrderType;
			destination.PortfolioName = PortfolioName;
			destination.Price = Price;
			destination.RepoInfo = RepoInfo?.Clone();
			destination.RpsInfo = RpsInfo?.Clone();
			//destination.SecurityId = SecurityId;
			//destination.SecurityType = SecurityType;
			destination.Side = Side;
			destination.TimeInForce = TimeInForce;
			destination.TransactionId = TransactionId;
			destination.VisibleVolume = VisibleVolume;
			destination.Volume = Volume;
			//destination.Currency = Currency;
			destination.UserOrderId = UserOrderId;
			destination.ClientCode = ClientCode;
			destination.BrokerCode = BrokerCode;
			destination.IsMarketMaker = IsMarketMaker;
			destination.IsMargin = IsMargin;
			destination.Slippage = Slippage;

			base.CopyTo(destination);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + $",TransId={TransactionId},Price={Price},Side={Side},OrdType={OrderType},Vol={Volume},Sec={SecurityId},Pf={PortfolioName}";
		}
	}
}