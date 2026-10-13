namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for SecurityList response FIX message.
/// </summary>
/// <param name="SecurityReqId">Security request identifier.</param>
/// <param name="SecurityResponseId">Security response identifier.</param>
/// <param name="Securities">Security messages collection.</param>
/// <param name="LastFragment">Is last fragment flag.</param>
public record struct FixSecurityListResponse(
	FixId SecurityReqId,
	FixId SecurityResponseId,
	ICollection<SecurityMessage> Securities,
	bool LastFragment);
