namespace StockSharp.Fix.Native;

/// <summary>
/// Data for SubscriptionOnline FIX message.
/// </summary>
/// <param name="MdReqId">Market data request identifier.</param>
public record struct FixSubscriptionOnline(FixId MdReqId);
