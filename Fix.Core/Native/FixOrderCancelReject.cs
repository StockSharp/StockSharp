namespace StockSharp.Fix.Native;

/// <summary>
/// Data for OrderCancelReject FIX message.
/// </summary>
/// <param name="ClOrdId">Client order identifier of the cancel request.</param>
/// <param name="OrderId">Exchange order identifier.</param>
/// <param name="ErrorText">Rejection error text.</param>
/// <param name="TransactTime">Transaction time.</param>
/// <param name="CxlRejResponseTo">Which request this rejects: a cancel or a cancel/replace. Carried
/// so a rejected replace is reported as a replace reject, not always as a cancel reject.</param>
public record struct FixOrderCancelReject(
	FixId ClOrdId,
	string OrderId,
	string ErrorText,
	DateTime TransactTime,
	char CxlRejResponseTo);
