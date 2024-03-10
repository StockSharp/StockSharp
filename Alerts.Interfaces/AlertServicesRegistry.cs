namespace StockSharp.Alerts;

using Ecng.Configuration;

/// <summary>
/// Extension class.
/// </summary>
public static class AlertServicesRegistry
{
	/// <summary>
	/// Alert notification service.
	/// </summary>
	public static IAlertNotificationService NotificationService => ConfigManager.GetService<IAlertNotificationService>();

	/// <summary>
	/// Alert notification service.
	/// </summary>
	public static IAlertNotificationService TryNotificationService => ConfigManager.TryGetService<IAlertNotificationService>();

	/// <summary>
	/// Alert processing service.
	/// </summary>
	public static IAlertProcessingService ProcessingService => ConfigManager.GetService<IAlertProcessingService>();

	/// <summary>
	/// Alert processing service.
	/// </summary>
	public static IAlertProcessingService TryProcessingService => ConfigManager.TryGetService<IAlertProcessingService>();
}