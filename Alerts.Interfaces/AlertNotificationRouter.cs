namespace StockSharp.Alerts;

using System.Threading.Channels;

using StockSharp.Configuration;

/// <summary>
/// Sends an alert to whichever channel it asks for, off the thread that raised it.
/// </summary>
public class AlertNotificationRouter : BaseLogReceiver, IAlertNotificationService
{
	private readonly Channel<(AlertNotifications type, long? externalId, LogLevels logLevel, string caption, string message, DateTime time)> _channel;
	private readonly CancellationTokenSource _cts = new();
	private readonly IAlertNotificationService _externalProvider;
	private readonly IAlertSoundService _sound;
	private readonly IDesktopPopupService _popup;
	private readonly ILogReceiver _log;

	/// <summary>
	/// Initializes a new instance of the <see cref="AlertNotificationRouter"/>.
	/// </summary>
	/// <param name="maxQueue">How many pending alerts are kept before new ones wait.</param>
	/// <param name="externalProvider">Carries the alerts this process cannot deliver itself, such as Telegram.</param>
	/// <param name="sound">Plays the alert sound.</param>
	/// <param name="popup">Shows the desktop popup.</param>
	/// <param name="log">Receives the alerts asking to be logged.</param>
	public AlertNotificationRouter(
		int maxQueue,
		IAlertNotificationService externalProvider,
		IAlertSoundService sound,
		IDesktopPopupService popup,
		ILogReceiver log)
	{
		if (maxQueue <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxQueue), maxQueue, LocalizedStrings.InvalidValue);

		_externalProvider = externalProvider ?? throw new ArgumentNullException(nameof(externalProvider));
		_sound = sound ?? throw new ArgumentNullException(nameof(sound));
		_popup = popup ?? throw new ArgumentNullException(nameof(popup));
		_log = log ?? throw new ArgumentNullException(nameof(log));

		_channel = Channel.CreateBounded<(AlertNotifications, long?, LogLevels, string, string, DateTime)>(maxQueue);

		Task.Run(DispatchAsync, _cts.Token);
	}

	private async Task DispatchAsync()
	{
		var reader = _channel.Reader;
		var token = _cts.Token;

		while (!token.IsCancellationRequested)
		{
			try
			{
				var (type, externalId, logLevel, caption, message, time) = await reader.ReadAsync(token);

				switch (type)
				{
					case AlertNotifications.Sound:
						await _sound.PlayAsync(token);
						break;
					case AlertNotifications.Telegram:
						await _externalProvider.NotifyAsync(type, externalId, logLevel, caption, message, time, token);
						break;
					case AlertNotifications.Log:
						// Shown to a person, so in the time zone that person set.
						_log.AddWarningLog(() => LocalizedStrings.AlertDetails
							.Put(time.ToAppTime(), caption, Environment.NewLine + message));
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
				}
			}
			catch (Exception ex)
			{
				if (!token.IsCancellationRequested)
					LogError(ex);
			}
		}
	}

	ValueTask IAlertNotificationService.NotifyAsync(AlertNotifications type, long? externalId, LogLevels logLevel, string caption, string message, DateTime time, CancellationToken cancellationToken)
	{
		// A popup answers whether the user clicked it, so it is awaited rather than queued.
		if (type == AlertNotifications.Popup)
			return new(_popup.NotifyAsync(time, caption, message, ToIconKey(logLevel), cancellationToken).AsTask());

		return _channel.Writer.WriteAsync(
			(type, externalId, logLevel, caption, message, time),
			cancellationToken);
	}

	/// <summary>
	/// The icon a popup shows for an alert of this severity. A popup that always looks the same
	/// says nothing about which alert fired.
	/// </summary>
	/// <param name="logLevel">Alert severity.</param>
	/// <returns>Icon key, or an empty string when the severity has no icon of its own.</returns>
	private static string ToIconKey(LogLevels logLevel)
		=> logLevel switch
		{
			LogLevels.Info => nameof(LogLevels.Info),
			LogLevels.Warning => nameof(LogLevels.Warning),
			LogLevels.Error => nameof(LogLevels.Error),
			LogLevels.Debug => nameof(LogLevels.Debug),
			_ => string.Empty,
		};

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_cts.Cancel();
		_channel.Writer.TryComplete();

		base.DisposeManaged();
	}
}
