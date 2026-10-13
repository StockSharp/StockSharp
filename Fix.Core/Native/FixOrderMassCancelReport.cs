namespace StockSharp.Fix.Native;

/// <summary>
/// Data for OrderMassCancelReport FIX message.
/// </summary>
/// <param name="ClOrdId">Client order identifier of the cancel request.</param>
/// <param name="MassCancelRequestType">Mass cancel request type.</param>
/// <param name="ErrorMessage">Error message if rejected.</param>
public record struct FixOrderMassCancelReport(
	FixId ClOrdId,
	char MassCancelRequestType,
	string ErrorMessage);
