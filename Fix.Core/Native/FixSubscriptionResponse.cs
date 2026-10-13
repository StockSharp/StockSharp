namespace StockSharp.Fix.Native;

/// <summary>
/// Data for SubscriptionResponse FIX message.
/// </summary>
/// <param name="MdReqId">Market data request identifier.</param>
/// <param name="MdResponseId">Market data response identifier.</param>
/// <param name="Error">Error message if failed.</param>
public record struct FixSubscriptionResponse(
	FixId MdReqId,
	FixId MdResponseId,
	string Error);
