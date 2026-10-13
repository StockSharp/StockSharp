namespace StockSharp.Fix.Native;

/// <summary>
/// Data from BoardLookup FIX message.
/// </summary>
/// <param name="MdReqId">Request ID (TransactionId).</param>
/// <param name="Like">Filter pattern for board names.</param>
/// <param name="DisableArchive">Whether to exclude archived boards.</param>
public record struct FixBoardLookup(
	FixId MdReqId,
	string Like,
	bool DisableArchive);
