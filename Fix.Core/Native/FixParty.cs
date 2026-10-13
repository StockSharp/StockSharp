namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Party information from FIX message.
/// </summary>
public class FixParty
{
	/// <summary>
	/// Party identifier value.
	/// </summary>
	public string PartyId { get; set; }

	/// <summary>
	/// Party role (e.g., ExecutingFirm, ClientId, etc.).
	/// </summary>
	public PartyRole? PartyRole { get; set; }
}
