namespace StockSharp.Fix.Native;

/// <summary>
/// Data for MarketDataRequestReject FIX message.
/// </summary>
/// <param name="MdReqId">Market data request identifier.</param>
/// <param name="Error">Error exception.</param>
public record struct FixMarketDataRequestReject(
	FixId MdReqId,
	Exception Error);
