namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for SecurityLegsInfo FIX message.
/// </summary>
/// <param name="MdReqId">Request identifier.</param>
/// <param name="Legs">Dictionary of security to its legs mapping.</param>
public record struct FixSecurityLegsInfo(
	string MdReqId,
	IDictionary<SecurityId, IEnumerable<SecurityId>> Legs);
