namespace StockSharp.Fix.Native;

/// <summary>
/// Data for Heartbeat FIX message.
/// </summary>
/// <param name="TestReqId">Test request identifier.</param>
public record struct FixHeartbeat(string TestReqId);
