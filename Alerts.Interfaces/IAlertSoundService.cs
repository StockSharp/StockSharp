namespace StockSharp.Alerts;

/// <summary>
/// Plays the sound that announces an alert.
/// </summary>
public interface IAlertSoundService : ILogSource
{
	/// <summary>
	/// Play the alert sound.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask PlayAsync(CancellationToken cancellationToken);
}
