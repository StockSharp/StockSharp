namespace StockSharp.Fix.Native;

/// <summary>
/// Data from Remove FIX message.
/// </summary>
/// <param name="MdReqId">Market data request identifier.</param>
/// <param name="Id">Entity identifier to remove.</param>
/// <param name="Type">Entity type.</param>
public record struct FixRemove(
	FixId MdReqId,
	string Id,
	string Type);
