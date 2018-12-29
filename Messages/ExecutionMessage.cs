#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: ExecutionMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The types of data that contain information in <see cref="ExecutionMessage"/>.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public enum ExecutionTypes
	{
		/// <summary>
		/// Tick trade.
		/// </summary>
		[EnumMember]
		Tick,

		/// <summary>
		/// Transaction.
		/// </summary>
		[EnumMember]
		Transaction,

		/// <summary>
		/// Obsolete.
		/// </summary>
		[EnumMember]
		[Obsolete]
		Obsolete,

		/// <summary>
		/// Order log.
		/// </summary>
		[EnumMember]
		OrderLog,
	}

	/// <summary>
	/// The message contains information about the execution.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public sealed class ExecutionMessage : Message
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Portfolio name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.PortfolioNameKey)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[DataMember]
		[MainCategory]
		[DisplayNameLoc(LocalizedStrings.ClientCodeKey)]
		[DescriptionLoc(LocalizedStrings.ClientCodeDescKey)]
		public string ClientCode { get; set; }

		/// <summary>
		/// Broker firm code.
		/// </summary>
		[DataMember]
		[MainCategory]
		[CategoryLoc(LocalizedStrings.Str2593Key)]
		[DisplayNameLoc(LocalizedStrings.BrokerKey)]
		[DescriptionLoc(LocalizedStrings.Str2619Key)]
		public string BrokerCode { get; set; }

		/// <summary>
		/// The depositary where the physical security.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.DepoKey)]
		[DescriptionLoc(LocalizedStrings.DepoNameKey)]
		[MainCategory]
		public string DepoName { get; set; }

		/// <summary>
		/// Server time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ServerTimeKey)]
		[DescriptionLoc(LocalizedStrings.ServerTimeKey, true)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Transaction ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <summary>
		/// ID of original transaction, for which this message is the answer.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OriginalTransactionKey)]
		[DescriptionLoc(LocalizedStrings.OriginalTransactionIdKey)]
		[MainCategory]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Data type, information about which is contained in the <see cref="ExecutionMessage"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DataTypeKey)]
		[DescriptionLoc(LocalizedStrings.Str110Key)]
		[MainCategory]
		[Nullable]
		public ExecutionTypes? ExecutionType { get; set; }

		/// <summary>
		/// Is the action an order cancellation.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CancelKey)]
		[DescriptionLoc(LocalizedStrings.IsActionOrderCancellationKey)]
		[MainCategory]
		public bool IsCancelled { get; set; }

		/// <summary>
		/// Order ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OrderIdKey)]
		[DescriptionLoc(LocalizedStrings.OrderIdKey, true)]
		[MainCategory]
		[Nullable]
		public long? OrderId { get; set; }

		/// <summary>
		/// Order ID (as string, if electronic board does not use numeric order ID representation).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OrderIdStringKey)]
		[DescriptionLoc(LocalizedStrings.OrderIdStringDescKey)]
		[MainCategory]
		public string OrderStringId { get; set; }

		/// <summary>
		/// Board order id. Uses in case of <see cref="ExecutionMessage.OrderId"/> and <see cref="ExecutionMessage.OrderStringId"/> is a brokerage system ids.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str117Key)]
		[DescriptionLoc(LocalizedStrings.Str118Key)]
		[MainCategory]
		public string OrderBoardId { get; set; }

		///// <summary>
		///// Derived order ID (e.g., conditional order generated a real exchange order).
		///// </summary>
		//[DataMember]
		//[DisplayNameLoc(LocalizedStrings.DerivedKey)]
		//[DescriptionLoc(LocalizedStrings.DerivedOrderIdKey)]
		//[MainCategory]
		//[Nullable]
		//public long? DerivedOrderId { get; set; }

		///// <summary>
		///// Derived order ID (e.g., conditional order generated a real exchange order).
		///// </summary>
		//[DataMember]
		//[DisplayNameLoc(LocalizedStrings.DerivedStringKey)]
		//[DescriptionLoc(LocalizedStrings.DerivedStringDescKey)]
		//[MainCategory]
		//public string DerivedOrderStringId { get; set; }

		/// <summary>
		/// Is the message contains order info.
		/// </summary>
		public bool HasOrderInfo { get; set; }

		/// <summary>
		/// Is the message contains trade info.
		/// </summary>
		public bool HasTradeInfo { get; set; }

		/// <summary>
		/// Order price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.OrderPriceKey)]
		[MainCategory]
		public decimal OrderPrice { get; set; }

		/// <summary>
		/// Number of contracts in the order.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeOrderKey)]
		[DescriptionLoc(LocalizedStrings.OrderVolumeKey)]
		[MainCategory]
		[Nullable]
		public decimal? OrderVolume { get; set; }

		/// <summary>
		/// Number of contracts in the trade.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeTradeKey)]
		[DescriptionLoc(LocalizedStrings.TradeVolumeKey)]
		[MainCategory]
		[Nullable]
		public decimal? TradeVolume { get; set; }

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
		/// Order contracts balance.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str130Key)]
		[DescriptionLoc(LocalizedStrings.Str131Key)]
		[MainCategory]
		[Nullable]
		public decimal? Balance { get; set; }

		/// <summary>
		/// Order type.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str132Key)]
		[DescriptionLoc(LocalizedStrings.Str133Key)]
		[MainCategory]
		public OrderTypes? OrderType { get; set; }

		/// <summary>
		/// System order status.
		/// </summary>
		[DataMember]
		[Browsable(false)]
		[Nullable]
		public long? OrderStatus { get; set; }

		/// <summary>
		/// Order state.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str134Key)]
		[MainCategory]
		[Nullable]
		public OrderStates? OrderState { get; set; }

		/// <summary>
		/// Placed order comment.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str135Key)]
		[DescriptionLoc(LocalizedStrings.Str136Key)]
		[MainCategory]
		public string Comment { get; set; }

		/// <summary>
		/// Message for order (created by the trading system when registered, changed or cancelled).
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str137Key)]
		[DescriptionLoc(LocalizedStrings.Str138Key)]
		[MainCategory]
		public string SystemComment { get; set; }

		/// <summary>
		/// Is a system trade.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str139Key)]
		[DescriptionLoc(LocalizedStrings.Str140Key)]
		[MainCategory]
		[Nullable]
		public bool? IsSystem { get; set; }

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
		public DateTimeOffset? ExpiryDate { get; set; }

		/// <summary>
		/// Limit order execution condition.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str143Key)]
		[DescriptionLoc(LocalizedStrings.Str144Key)]
		[MainCategory]
		[Nullable]
		public TimeInForce? TimeInForce { get; set; }

		/// <summary>
		/// Trade ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OrderIdKey)]
		[DescriptionLoc(LocalizedStrings.Str145Key)]
		[MainCategory]
		[Nullable]
		public long? TradeId { get; set; }

		/// <summary>
		/// Trade ID (as string, if electronic board does not use numeric order ID representation).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OrderIdStringKey)]
		[DescriptionLoc(LocalizedStrings.Str146Key)]
		[MainCategory]
		public string TradeStringId { get; set; }

		/// <summary>
		/// Trade price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.Str147Key)]
		[MainCategory]
		[Nullable]
		public decimal? TradePrice { get; set; }

		/// <summary>
		/// System trade status.
		/// </summary>
		[DataMember]
		[Browsable(false)]
		[Nullable]
		public int? TradeStatus { get; set; }

		/// <summary>
		/// Deal initiator (seller or buyer).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str148Key)]
		[DescriptionLoc(LocalizedStrings.Str149Key)]
		[MainCategory]
		[Nullable]
		public Sides? OriginSide { get; set; }

		/// <summary>
		/// Number of open positions (open interest).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str150Key)]
		[DescriptionLoc(LocalizedStrings.Str151Key)]
		[MainCategory]
		[Nullable]
		public decimal? OpenInterest { get; set; }

		/// <summary>
		/// Error registering/cancelling order.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str152Key)]
		[DescriptionLoc(LocalizedStrings.Str153Key, true)]
		[MainCategory]
		public Exception Error { get; set; }

		/// <summary>
		/// Order condition (e.g., stop- and algo- orders parameters).
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str154Key)]
		[DescriptionLoc(LocalizedStrings.Str155Key)]
		[CategoryLoc(LocalizedStrings.Str156Key)]
		public OrderCondition Condition { get; set; }

		///// <summary>
		///// Является ли сообщение последним в запрашиваемом пакете (только для исторических сделок).
		///// </summary>
		//[DataMember]
		//[DisplayName("Последний")]
		//[Description("Является ли сообщение последним в запрашиваемом пакете (только для исторических сделок).")]
		//[MainCategory]
		//public bool IsFinished { get; set; }

		/// <summary>
		/// Is tick uptrend or downtrend in price. Uses only <see cref="ExecutionType"/> for <see cref="ExecutionTypes.Tick"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str157Key)]
		[DescriptionLoc(LocalizedStrings.Str158Key)]
		[MainCategory]
		[Nullable]
		public bool? IsUpTick { get; set; }

		/// <summary>
		/// Commission (broker, exchange etc.). Uses when <see cref="ExecutionType"/> set to <see cref="ExecutionTypes.Transaction"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str160Key)]
		[MainCategory]
		[Nullable]
		public decimal? Commission { get; set; }

		/// <summary>
		/// Commission currency. Can be <see lnagword="null"/>.
		/// </summary>
		public string CommissionCurrency { get; set; }

		/// <summary>
		/// Network latency. Uses when <see cref="ExecutionType"/> set to <see cref="ExecutionTypes.Transaction"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str161Key)]
		[DescriptionLoc(LocalizedStrings.Str162Key)]
		[MainCategory]
		[Nullable]
		public TimeSpan? Latency { get; set; }

		/// <summary>
		/// Slippage in trade price. Uses when <see cref="ExecutionType"/> set to <see cref="ExecutionTypes.Transaction"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str163Key)]
		[DescriptionLoc(LocalizedStrings.Str164Key)]
		[MainCategory]
		[Nullable]
		public decimal? Slippage { get; set; }

		/// <summary>
		/// User order id. Uses when <see cref="ExecutionType"/> set to <see cref="ExecutionTypes.Transaction"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str165Key)]
		[DescriptionLoc(LocalizedStrings.Str166Key)]
		[MainCategory]
		public string UserOrderId { get; set; }

		/// <summary>
		/// Trading security currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str382Key)]
		[MainCategory]
		[Nullable]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// The profit, realized by trade.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PnLKey)]
		[DescriptionLoc(LocalizedStrings.PnLKey, true)]
		[MainCategory]
		[Nullable]
		public decimal? PnL { get; set; }

		/// <summary>
		/// The position, generated by order or trade.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str862Key)]
		[DescriptionLoc(LocalizedStrings.Str862Key, true)]
		[MainCategory]
		[Nullable]
		public decimal? Position { get; set; }

		/// <summary>
		/// Is the order of market-maker.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarketMakerKey)]
		[DescriptionLoc(LocalizedStrings.MarketMakerOrderKey, true)]
		public bool? IsMarketMaker { get; set; }

		/// <summary>
		/// Is margin enabled.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarginKey)]
		[DescriptionLoc(LocalizedStrings.IsMarginKey)]
		public bool? IsMargin { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ExecutionMessage"/>.
		/// </summary>
		public ExecutionMessage()
			: base(MessageTypes.Execution)
		{
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + $",T(S)={ServerTime:yyyy/MM/dd HH:mm:ss.fff},({ExecutionType}),Sec={SecurityId},Ord={OrderId}/{TransactionId}/{OriginalTransactionId},Fail={Error},Price={OrderPrice},OrdVol={OrderVolume},TrVol={TradeVolume},Bal={Balance},TId={TradeId},Pf={PortfolioName},TPrice={TradePrice},UId={UserOrderId},State={OrderState}";
		}

		/// <summary>
		/// Create a copy of <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ExecutionMessage
			{
				Balance = Balance,
				Comment = Comment,
				Condition = Condition?.Clone(),
				ClientCode = ClientCode,
				BrokerCode = BrokerCode,
				Currency = Currency,
				ServerTime = ServerTime,
				DepoName = DepoName,
				Error = Error,
				ExpiryDate = ExpiryDate,
				IsSystem = IsSystem,
				LocalTime = LocalTime,
				OpenInterest = OpenInterest,
				OrderId = OrderId,
				OrderStringId = OrderStringId,
				OrderBoardId = OrderBoardId,
				ExecutionType = ExecutionType,
				IsCancelled = IsCancelled,
				//Action = Action,
				OrderState = OrderState,
				OrderStatus = OrderStatus,
				OrderType = OrderType,
				OriginSide = OriginSide,
				PortfolioName = PortfolioName,
				OrderPrice = OrderPrice,
				SecurityId = SecurityId,
				Side = Side,
				SystemComment = SystemComment,
				TimeInForce = TimeInForce,
				TradeId = TradeId,
				TradeStringId = TradeStringId,
				TradePrice = TradePrice,
				TradeStatus = TradeStatus,
				TransactionId = TransactionId,
				OriginalTransactionId = OriginalTransactionId,
				OrderVolume = OrderVolume,
				TradeVolume = TradeVolume,
				//IsFinished = IsFinished,
				VisibleVolume = VisibleVolume,
				IsUpTick = IsUpTick,
				Commission = Commission,
				Latency = Latency,
				Slippage = Slippage,
				UserOrderId = UserOrderId,

				//DerivedOrderId = DerivedOrderId,
				//DerivedOrderStringId = DerivedOrderStringId,

				PnL = PnL,
				Position = Position,

				HasTradeInfo = HasTradeInfo,
				HasOrderInfo = HasOrderInfo,

				IsMarketMaker = IsMarketMaker,
				IsMargin = IsMargin,

				CommissionCurrency = CommissionCurrency,
			};

			this.CopyExtensionInfo(clone);

			return clone;
		}
	}
}