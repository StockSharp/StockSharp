namespace StockSharp.Fix.Native;

/// <summary>
/// Data from OrderCancelReplaceRequest FIX message.
/// </summary>
/// <param name="ClOrdId">Client order identifier for the replace request.</param>
/// <param name="OrigClOrdId">Original client order identifier.</param>
/// <param name="OrderId">Exchange order identifier.</param>
/// <param name="Account">Account identifier.</param>
/// <param name="Price">New order price.</param>
/// <param name="OrderQty">New order quantity.</param>
/// <param name="SecurityType">Security type.</param>
/// <param name="CfiCode">CFI code (Classification of Financial Instruments).</param>
/// <param name="Symbol">Security symbol.</param>
/// <param name="SecurityExchange">Security exchange code.</param>
/// <param name="CashMargin">Cash margin indicator.</param>
/// <param name="Slippage">Allowed slippage.</param>
/// <param name="IsManual">Whether order modification is manual.</param>
/// <param name="MinQty">Minimum execution quantity.</param>
/// <param name="PositionEffect">Position effect (O=Open, C=Close).</param>
/// <param name="PostOnly">Post-only flag (maker only).</param>
/// <param name="SecondaryOrderId">Secondary order identifier.</param>
/// <param name="StrategyId">Strategy identifier.</param>
/// <param name="OldPrice">Previous order price.</param>
/// <param name="OldVolume">Previous order volume.</param>
/// <param name="Leverage">Leverage multiplier.</param>
/// <param name="Parties">Party information (ClientCode, BrokerCode).</param>
/// <param name="Side">Order side (1=Buy, 2=Sell). Carried so a replace can change/keep the side.</param>
/// <param name="TimeInForce">Time in force. Carried so a replace preserves the order's lifetime policy.</param>
/// <param name="ExpiryDate">Expiry time for a GTD order (maps to TillDate).</param>
/// <param name="MaxFloor">Displayed (iceberg) quantity, maps to VisibleVolume; the concealed size must survive a replace.</param>
/// <param name="Text">Free-text comment (maps to Comment).</param>
/// <param name="Condition">Order condition (stop/algo). Carried so a conditional order keeps its condition on replace.</param>
public record struct FixOrderCancelReplaceRequest(
	FixId ClOrdId,
	FixId OrigClOrdId,
	string OrderId,
	string Account,
	decimal? Price,
	decimal? OrderQty,
	string SecurityType,
	string CfiCode,
	string Symbol,
	string SecurityExchange,
	char? CashMargin,
	decimal? Slippage,
	bool? IsManual,
	decimal? MinQty,
	char? PositionEffect,
	bool? PostOnly,
	string SecondaryOrderId,
	string StrategyId,
	decimal? OldPrice,
	decimal? OldVolume,
	int? Leverage,
	FixParty[] Parties,
	char? Side,
	char? TimeInForce,
	DateTime? ExpiryDate,
	decimal? MaxFloor,
	string Text,
	OrderCondition Condition);
