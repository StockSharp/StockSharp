namespace StockSharp.Alerts
{
	using System;

	using StockSharp.Logging;

	/// <summary>
	/// Defines an alert notification service.
	/// </summary>
	public interface IAlertNotificationService : ILogSource
	{
		/// <summary>
		/// Add alert at the output.
		/// </summary>
		/// <param name="type">Alert type.</param>
		/// <param name="caption">Signal header.</param>
		/// <param name="message">Alert text.</param>
		/// <param name="time">Creation time.</param>
		void Notify(AlertNotifications type, string caption, string message, DateTimeOffset time);
	}
}