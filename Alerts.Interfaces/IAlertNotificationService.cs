namespace StockSharp.Alerts
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

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
		/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
		/// <returns><see cref="ValueTask"/>.</returns>
		ValueTask NotifyAsync(AlertNotifications type, string caption, string message, DateTimeOffset time, CancellationToken cancellationToken);
	}
}