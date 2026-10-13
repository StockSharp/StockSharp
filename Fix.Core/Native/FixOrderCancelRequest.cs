namespace StockSharp.Fix.Native;

/// <summary>
/// Data from OrderCancelRequest FIX message.
/// </summary>
/// <param name="ClOrdId">Client order identifier for the cancel request.</param>
/// <param name="OrigClOrdId">Original client order identifier of order to cancel.</param>
/// <param name="OrderId">Exchange order identifier.</param>
/// <param name="Account">Account identifier.</param>
/// <param name="Balance">Remaining order quantity.</param>
/// <param name="OrderQty">Original order quantity.</param>
/// <param name="SecurityType">Security type.</param>
/// <param name="CfiCode">CFI code (Classification of Financial Instruments).</param>
/// <param name="Symbol">Security symbol.</param>
/// <param name="SecurityExchange">Security exchange code.</param>
/// <param name="Text">Free text comment.</param>
/// <param name="OrdType">Order type (1=Market, 2=Limit, etc.).</param>
/// <param name="CashMargin">Cash margin indicator.</param>
/// <param name="Side">Order side (1=Buy, 2=Sell).</param>
/// <param name="Parties">Party information (ClientCode, BrokerCode).</param>
public record struct FixOrderCancelRequest(
	FixId ClOrdId,
	FixId OrigClOrdId,
	string OrderId,
	string Account,
	decimal? Balance,
	decimal? OrderQty,
	string SecurityType,
	string CfiCode,
	string Symbol,
	string SecurityExchange,
	string Text,
	char? OrdType,
	char? CashMargin,
	char? Side,
	FixParty[] Parties);
