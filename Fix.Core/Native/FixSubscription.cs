namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for Subscription FIX message.
/// </summary>
/// <param name="MdReqId">Market data request identifier.</param>
/// <param name="MdResponseId">Market data response identifier.</param>
/// <param name="MdMsg">Market data message.</param>
public record struct FixSubscription(
	string MdReqId,
	string MdResponseId,
	MarketDataMessage MdMsg);
