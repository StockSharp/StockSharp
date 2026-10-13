namespace StockSharp.Charting;

/// <summary>
/// The part of the chart whose settings can be captured and put back in memory, without saving and loading them.
/// </summary>
public interface IChartSnapshotPart
{
	/// <summary>
	/// Capture the settings as they are now.
	/// </summary>
	/// <returns>The captured settings. Later changes of the part do not reach them.</returns>
	object CaptureSettings();

	/// <summary>
	/// Put back the settings captured by <see cref="CaptureSettings"/>.
	/// </summary>
	/// <param name="settings">The captured settings.</param>
	void RestoreSettings(object settings);
}
