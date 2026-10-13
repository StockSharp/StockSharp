namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data from NewOrderSingle FIX message.
/// </summary>
/// <param name="ClOrdId">Client order identifier.</param>
/// <param name="Account">Account identifier.</param>
/// <param name="Price">Order price.</param>
/// <param name="OrderQty">Order quantity.</param>
/// <param name="MaxFloor">Maximum visible quantity (iceberg orders).</param>
/// <param name="SecurityType">Security type.</param>
/// <param name="CfiCode">CFI code (Classification of Financial Instruments).</param>
/// <param name="Symbol">Security symbol.</param>
/// <param name="SecurityExchange">Security exchange code.</param>
/// <param name="Side">Order side (1=Buy, 2=Sell).</param>
/// <param name="OrdType">Order type (1=Market, 2=Limit, etc.).</param>
/// <param name="TimeInForce">Time in force (0=Day, 1=GTC, 3=IOC, 4=FOK, 6=GTD).</param>
/// <param name="ExpiryDate">Order expiration date (for GTD orders).</param>
/// <param name="Condition">Order condition (stop, take-profit, etc.).</param>
/// <param name="Text">Free text comment.</param>
/// <param name="Parties">Party information (client, broker, etc.).</param>
/// <param name="OrderCapacity">Order capacity (A=Agency, P=Principal, R=Riskless).</param>
/// <param name="OrderRestrictions">Order restrictions.</param>
/// <param name="CashMargin">Cash margin indicator.</param>
/// <param name="Slippage">Allowed slippage for market orders.</param>
/// <param name="IsManual">Whether order is manual.</param>
/// <param name="MinQty">Minimum execution quantity.</param>
/// <param name="PositionEffect">Position effect (O=Open, C=Close).</param>
/// <param name="PostOnly">Post-only flag (maker only).</param>
/// <param name="SecondaryOrderId">Secondary order identifier.</param>
/// <param name="StrategyId">Strategy identifier.</param>
/// <param name="Leverage">Leverage multiplier.</param>
public record struct FixNewOrderSingle(
	FixId ClOrdId,
	string Account,
	decimal? Price,
	decimal? OrderQty,
	decimal? MaxFloor,
	string SecurityType,
	string CfiCode,
	string Symbol,
	string SecurityExchange,
	char? Side,
	char? OrdType,
	char? TimeInForce,
	DateTime? ExpiryDate,
	OrderCondition Condition,
	string Text,
	FixParty[] Parties,
	char? OrderCapacity,
	string OrderRestrictions,
	char? CashMargin,
	decimal? Slippage,
	bool? IsManual,
	decimal? MinQty,
	char? PositionEffect,
	bool? PostOnly,
	string SecondaryOrderId,
	string StrategyId,
	int? Leverage)
{
	/// <summary>
	/// Owning user's numeric id (broker extension, not a standard FIX tag). Carried over
	/// the SBE wire so the StopOrder server can scope a stop to its owner by UserId — the
	/// only safe key when a single proxy session (the OrderRouter) fronts all users.
	/// </summary>
	public long UserId { get; set; }

	/// <summary>
	/// Canonical portfolio identity — the broker's <c>BrokerPortfolio</c> PK (broker extension,
	/// not a standard FIX tag). Carried over the SBE wire so the StopOrder/Algo server persists
	/// the order's portfolio FK by id rather than the shared display name.
	/// </summary>
	public long PortfolioId { get; set; }
}
