namespace StockSharp.Fix.Native;

/// <summary>
/// Data for SecurityList error response FIX message.
/// </summary>
/// <param name="SecurityReqId">Security request identifier.</param>
/// <param name="SecurityResponseId">Security response identifier.</param>
/// <param name="Error">Error exception.</param>
public record struct FixSecurityListResponseError(
	FixId SecurityReqId,
	FixId SecurityResponseId,
	Exception Error);
