namespace StockSharp.Fix.Native;

/// <summary>
/// Party.
/// </summary>
public class Party
{
	/// <summary>
	/// 
	/// </summary>
	public string PartyId;

	/// <summary>
	/// 
	/// </summary>
	public PartyRole? PartyRole;
}

/// <summary>
/// Execution report.
/// </summary>
public class ExecutionReport
{
	/// <summary>
	/// <see cref="FixTags.PossDupFlag"/>.
	/// </summary>
	public bool? PossDupFlag;

	/// <summary>
	/// Describes the purpose of the execution report <see cref="Native.ExecType"/>.
	/// </summary>
	public char? ExecType;

	/// <summary>
	/// Used to identify the previous order in cancel and cancel/replace requests.
	/// </summary>
	public string OrigClOrdId;

	/// <summary>
	/// Unique identifier for Order as assigned by sell-side (broker, exchange, ECN).
	/// </summary>
	public string OrderId;

	/// <summary>
	/// Unique identifier for Order as assigned by the buy-side (institution, broker, intermediary etc.).
	/// </summary>
	public string ClOrdId;

	/// <summary>
	/// Quantity ordered. This represents the number of shares for equities or par, face or nominal value for FI instruments.
	/// </summary>
	public decimal? OrderQty;

	/// <summary>
	/// Quantity open for further execution.
	/// </summary>
	public decimal? LeavesQty;

	/// <summary>
	/// Total quantity (e.g. number of shares) filled.
	/// </summary>
	public decimal? CumQty { get; set; }

	/// <summary>
	/// Traded / Regulatory timestamp value.
	/// </summary>
	public DateTime? TrdRegTimestamp { get; set; }

	/// <summary>
	/// Traded / Regulatory timestamp type.
	/// </summary>
	public TrdRegTimestampType? TrdRegTimestampType { get; set; }

	/// <summary>
	/// Maximum quantity (e.g. number of shares) within an order to be shown on the exchange floor at any given time.
	/// </summary>
	public decimal? MaxFloor { get; set; }

	/// <summary>
	/// Required if responding to and if provided on the <see cref="FixMessages.OrderMassStatusRequest"/> message.
	/// </summary>
	public string MassStatusReqId { get; set; }

	/// <summary>
	/// Broker capacity in order execution.
	/// </summary>
	public char? LastCapacity { get; set; }

	/// <summary>
	/// Designates the capacity of the firm placing the order.
	/// </summary>
	public char? OrderCapacity { get; set; }

	/// <summary>
	/// Restrictions associated with an order. If more than one restriction is applicable to an order, this field can contain multiple instructions separated by space.
	/// </summary>
	public string OrderRestrictions { get; set; }

	/// <summary>
	/// Extended order status.
	/// </summary>
	public long? ExtendedOrderStatus { get; set; }

	/// <summary>
	/// Extended trade status.
	/// </summary>
	public int? ExtendedTradeStatus { get; set; }

	/// <summary>
	/// Identifies whether an order is a margin order or a non-margin order.
	/// </summary>
	public char? CashMargin { get; set; }

	/// <summary>
	/// Calculated average price of all fills on this order.
	/// </summary>
	public decimal? AvgPx { get; set; }

	/// <summary>
	/// Price per unit of quantity (e.g. per share).
	/// </summary>
	public decimal? Price;

	/// <summary>
	/// Ticker symbol. Common, "human understood" representation of the security.
	/// </summary>
	public string Symbol;

	/// <summary>
	/// Market used to help identify a security.
	/// </summary>
	public string SecurityExchange;

	/// <summary>
	/// Security identifier value.
	/// </summary>
	public string SecurityId;

	/// <summary>
	/// Identifies class or source of the <see cref="SecurityId"/> value. Required if <see cref="SecurityId"/> is specified.
	/// </summary>
	public string SecurityIdSource;

	/// <summary>
	/// Identifier for Trading Session.
	/// </summary>
	public string TradingSessionId;

	/// <summary>
	/// 
	/// </summary>
	public string ExDestination;

	/// <summary>
	/// 
	/// </summary>
	public DateTime SendingTime;

	/// <summary>
	/// Order type <see cref="Native.OrdType"/>.
	/// </summary>
	public char? OrdType;

	///// <summary>
	///// 
	///// </summary>
	//public decimal? StopPx;

	/// <summary>
	/// 
	/// </summary>
	public DateTime? TradeDate;

	/// <summary>
	/// 
	/// </summary>
	public DateTime? TransactTime;

	/// <summary>
	/// 
	/// </summary>
	public string Account;

	/// <summary>
	/// 
	/// </summary>
	public string ClientId;

	/// <summary>
	/// 
	/// </summary>
	public string ExecBroker;

	/// <summary>
	/// 
	/// </summary>
	public Party[] Parties;

	/// <summary>
	/// 
	/// </summary>
	public char? OrdStatus;

	/// <summary>
	/// Side of order <see cref="Native.Side"/>.
	/// </summary>
	public char? Side;

	/// <summary>
	/// Specifies how long the order remains in effect. Absence of this field is interpreted as DAY. NOTE not applicable to CIV Orders.
	/// </summary>
	public char? TimeInForce;

	/// <summary>
	/// Date of order expiration (last day the order can trade), always expressed in terms of the local market date.
	/// </summary>
	public DateTime? ExpireDate;

	/// <summary>
	/// 
	/// </summary>
	public DateTime? ExpireTime;

	/// <summary>
	/// Price of this (last) fill.
	/// </summary>
	public decimal? LastPx;

	/// <summary>
	/// Quantity (e.g. shares) bought/sold on this (last) fill.
	/// </summary>
	public decimal? LastQty;

	/// <summary>
	/// Commission.
	/// </summary>
	public decimal? Commission;

	/// <summary>
	/// Commission currency. Can be <see langword="null"/>.
	/// </summary>
	public string CommissionCurrency;

	/// <summary>
	/// Free format text string.
	/// </summary>
	public string Text;

	/// <summary>
	/// Unique identifier of execution message as assigned by sell-side (broker, exchange, ECN).
	/// </summary>
	public string ExecId;

	/// <summary>
	/// Required if responding to and if provided on the <see cref="FixMessages.OrderStatusRequest"/> message.
	/// </summary>
	public string OrdStatusReqId;

	///// <summary>
	///// 
	///// </summary>
	//public long? OriginalTransactionId;

	///// <summary>
	///// 
	///// </summary>
	//public long? TransactionId;

	/// <summary>
	/// Indicator to identify whether this fill was a result of a liquidity provider providing or liquidity taker taking the liquidity.
	/// Valid values:
	/// 1 = Added Liquidity
	/// 2 = Removed Liquidity
	/// 3 = Liquidity Routed Out
	/// </summary>
	public int? LastLiquidityInd;

	/// <summary>
	/// Minimum quantity of an order to be executed.
	/// </summary>
	public decimal? MinQty { get; set; }

	/// <summary>
	/// Yield.
	/// </summary>
	public decimal? Yield { get; set; }

	/// <summary>
	/// Stop price.
	/// </summary>
	public decimal? StopPx { get; set; }

	/// <summary>
	/// Take profit.
	/// </summary>
	public decimal? TakeProfit { get; set; }

	/// <summary>
	/// Indicates whether the resulting position after a trade should be an opening position or closing position.
	/// </summary>
	public char? PositionEffect { get; set; }

	/// <summary>
	/// Used to identify whether the order initiator is an aggressor or not in the trade.
	/// </summary>
	public bool? AggressorIndicator { get; set; }

	/// <summary>
	/// Indicates if the order was initially received manually (as opposed to electronically).
	/// </summary>
	public bool? ManualOrderIndicator { get; set; }

	/// <summary>
	/// <see cref="FixTags.OrigSendingTime"/>.
	/// </summary>
	public DateTime? OrigSendingTime { get; set; }

	/// <summary>
	/// <see cref="FixTags.MsgSeqNum"/>.
	/// </summary>
	public long MsgSeqNum { get; set; }

	/// <summary>
	/// <see cref="FixTags.SecondaryOrderID"/>.
	/// </summary>
	public string SecondaryOrderId { get; set; }

	/// <summary>
	/// <see cref="FixTags.StrategyTypeId"/>.
	/// </summary>
	public string StrategyTypeId { get; set; }

	/// <summary>
	/// <see cref="FixTags.Leverage"/>.
	/// </summary>
	public int? Leverage { get; set; }

	/// <summary>
	/// <see cref="FixTags.MarketPrice"/>.
	/// </summary>
	public decimal? MarketPrice { get; set; }

	/// <summary>
	/// <see cref="FixTags.DisplayQty"/>.
	/// </summary>
	public decimal? DisplayQty { get; set; }

	/// <summary>
	/// Extra fields.
	/// </summary>
	public IDictionary<FixTags, object> ExtraFields { get; } = new Dictionary<FixTags, object>();
}