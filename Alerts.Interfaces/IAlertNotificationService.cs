namespace StockSharp.Alerts;

/// <summary>
/// Defines an alert notification service.
/// </summary>
public interface IAlertNotificationService : ILogSource
{
	/// <summary>
	/// Add alert at the output.
	/// </summary>
	/// <param name="type">Alert type.</param>
	/// <param name="externalId">External ID.</param>
	/// <param name="logLevel"><see cref="LogLevels"/></param>
	/// <param name="caption">Signal header.</param>
	/// <param name="message">Alert text.</param>
	/// <param name="time">Creation time.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask NotifyAsync(AlertNotifications type, long? externalId, LogLevels logLevel, string caption, string message, DateTimeOffset time, CancellationToken cancellationToken);
}

/// <summary>
/// Desktop popup notification service.
/// </summary>
public interface IDesktopPopupService : ILogSource
{
	/// <summary>
	/// Show desktop popup.
	/// </summary>
	/// <param name="time">Time.</param>
	/// <param name="caption">Signal header.</param>
	/// <param name="message">Alert text.</param>
	/// <param name="iconKey">Icon to show with notification.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>Task result is true if user has clicked the notification.</returns>
	ValueTask<bool> NotifyAsync(DateTimeOffset time, string caption, string message, string iconKey, CancellationToken cancellationToken);
}
