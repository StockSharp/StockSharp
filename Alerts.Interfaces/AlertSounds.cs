namespace StockSharp.Alerts;

using System.IO;
using System.Reflection;

/// <summary>
/// The sounds shipped for alert notifications.
/// </summary>
public static class AlertSounds
{
	private const string _ringBellName = "StockSharp.Alerts.ringbell.wav";

	/// <summary>
	/// Opens the default alert sound. The caller owns the returned stream.
	/// </summary>
	/// <returns>Wave audio.</returns>
	public static Stream OpenRingBell()
		=> typeof(AlertSounds).Assembly.GetManifestResourceStream(_ringBellName)
			?? throw new InvalidOperationException($"Sound '{_ringBellName}' was not embedded.");
}
