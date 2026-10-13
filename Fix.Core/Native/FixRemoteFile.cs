namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for RemoteFile FIX message.
/// </summary>
/// <param name="MdResponseId">Response identifier.</param>
/// <param name="File">Remote file message.</param>
public record struct FixRemoteFile(
	string MdResponseId,
	RemoteFileMessage File);
