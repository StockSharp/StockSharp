namespace StockSharp.Fix.Native;

/// <summary>
/// Data from TestRequest FIX message.
/// </summary>
/// <param name="TestReqId">Test request identifier.</param>
public record struct FixTestRequest(string TestReqId);
