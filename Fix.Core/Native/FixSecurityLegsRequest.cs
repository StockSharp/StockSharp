namespace StockSharp.Fix.Native;

/// <summary>
/// Data from SecurityLegsRequest FIX message.
/// </summary>
/// <param name="MdReqId">Market data request identifier.</param>
/// <param name="Symbol">Security symbol for legs lookup.</param>
public record struct FixSecurityLegsRequest(
	FixId MdReqId,
	string Symbol);
