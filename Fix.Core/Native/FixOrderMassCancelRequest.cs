namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data from OrderMassCancelRequest FIX message.
/// </summary>
/// <param name="ClOrdId">Client order identifier for the cancel request.</param>
/// <param name="Account">Account identifier filter.</param>
/// <param name="SecurityType">Security type filter.</param>
/// <param name="SecurityTypes">Multiple security types filter.</param>
/// <param name="CfiCode">CFI code filter.</param>
/// <param name="Symbol">Security symbol filter.</param>
/// <param name="SecurityExchange">Security exchange filter.</param>
/// <param name="Side">Side filter (1=Buy, 2=Sell).</param>
/// <param name="MassCancelRequestType">Mass cancel request type.</param>
/// <param name="Mode">Order group cancel mode.</param>
/// <param name="Parties">Party information (client code, broker code, etc.).</param>
public record struct FixOrderMassCancelRequest(
	FixId ClOrdId,
	string Account,
	string SecurityType,
	string[] SecurityTypes,
	string CfiCode,
	string Symbol,
	string SecurityExchange,
	char? Side,
	char? MassCancelRequestType,
	OrderGroupCancelModes Mode,
	FixParty[] Parties);
