namespace StockSharp.Fix.Native;

/// <summary>
/// Data for Reject FIX message.
/// </summary>
/// <param name="Error">Error message text.</param>
/// <param name="RefSeqNum">Reference sequence number of rejected message.</param>
/// <param name="RefMsgType">Reference message type of rejected message.</param>
/// <param name="RefTagId">Reference tag ID that caused rejection.</param>
/// <param name="SessionRejectReason">Session reject reason code.</param>
/// <param name="RequestId">Request identifier.</param>
public record struct FixReject(
	string Error,
	long? RefSeqNum,
	string RefMsgType,
	FixTags? RefTagId,
	int? SessionRejectReason,
	FixId RequestId);
