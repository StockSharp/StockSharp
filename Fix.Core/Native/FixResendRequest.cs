namespace StockSharp.Fix.Native;

/// <summary>
/// Data from ResendRequest FIX message.
/// </summary>
/// <param name="BeginSeqNo">Beginning sequence number for resend.</param>
/// <param name="EndSeqNo">Ending sequence number for resend (0 means infinity).</param>
public record struct FixResendRequest(long? BeginSeqNo, long? EndSeqNo);
