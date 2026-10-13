namespace StockSharp.Fix.Native;

/// <summary>
/// Data for Logon response FIX message.
/// </summary>
/// <param name="SessionId">Trading session identifier.</param>
/// <param name="LicenseFeatureId">License feature identifier.</param>
/// <param name="ComponentTimestamp">Component timestamp for version verification.</param>
public record struct FixLogonResponse(
	string SessionId,
	string LicenseFeatureId,
	DateTime ComponentTimestamp);
