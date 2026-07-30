namespace StockSharp.Fix.Native;

/// <summary>
/// Data from SequenceReset FIX message.
/// </summary>
/// <param name="GapFill">Indicates whether the message is a gap fill.</param>
/// <param name="NewSeqNo">New sequence number.</param>
public record struct FixSequenceReset(bool? GapFill, long? NewSeqNo);
