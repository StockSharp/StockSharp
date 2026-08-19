namespace StockSharp.Tests;

using StockSharp.Alerts;

/// <summary>
/// Every alert channel reaches the sink that serves it, and none reaches the others.
/// </summary>
[TestClass]
[DoNotParallelize]
public class AlertNotificationRouterTests
{
	[TestMethod]
	public async Task Sound_ReachesTheSoundService()
	{
		var sound = new CountingSound();
		var popup = new CountingPopup();
		var external = new CountingExternal();
		using var router = Create(external, sound, popup, out var log);

		await Notify(router, AlertNotifications.Sound);

		await sound.Played.Task.WaitAsync(TimeSpan.FromSeconds(5));

		popup.Count.AssertEqual(0);
		external.Count.AssertEqual(0);
		log.Warnings.AssertEqual(0);
	}

	[TestMethod]
	public async Task Telegram_ReachesTheExternalProvider()
	{
		var sound = new CountingSound();
		var popup = new CountingPopup();
		var external = new CountingExternal();
		using var router = Create(external, sound, popup, out var log);

		await Notify(router, AlertNotifications.Telegram);

		await external.Notified.Task.WaitAsync(TimeSpan.FromSeconds(5));

		sound.Count.AssertEqual(0);
		popup.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task Log_ReachesTheLogReceiver()
	{
		var sound = new CountingSound();
		var popup = new CountingPopup();
		var external = new CountingExternal();
		using var router = Create(external, sound, popup, out var log);

		await Notify(router, AlertNotifications.Log);

		await log.Written.Task.WaitAsync(TimeSpan.FromSeconds(5));

		sound.Count.AssertEqual(0);
		external.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task Popup_IsAwaitedRatherThanQueued()
	{
		var sound = new CountingSound();
		var popup = new CountingPopup { Result = true };
		var external = new CountingExternal();
		using var router = Create(external, sound, popup, out _);

		await Notify(router, AlertNotifications.Popup);

		// The popup answers whether the user clicked it, so it must be delivered by the caller's await.
		popup.Count.AssertEqual(1);
		sound.Count.AssertEqual(0);
		external.Count.AssertEqual(0);
	}

	[TestMethod]
	public void RejectsAnEmptyQueue()
		=> Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
			new AlertNotificationRouter(0, new CountingExternal(), new CountingSound(), new CountingPopup(), new CountingLog()));

	[TestMethod]
	public void RingBellIsShipped()
	{
		using var stream = AlertSounds.OpenRingBell();

		stream.AssertNotNull();
		(stream.Length > 0).AssertTrue();
	}

	private static AlertNotificationRouter Create(
		IAlertNotificationService external,
		IAlertSoundService sound,
		IDesktopPopupService popup,
		out CountingLog log)
	{
		log = new CountingLog();
		return new(10, external, sound, popup, log);
	}

	private static ValueTask Notify(IAlertNotificationService router, AlertNotifications type)
		=> router.NotifyAsync(type, null, LogLevels.Warning, "caption", "message", DateTime.UtcNow, default);

	private sealed class CountingSound : BaseLogReceiver, IAlertSoundService
	{
		public int Count;

		public TaskCompletionSource Played { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		ValueTask IAlertSoundService.PlayAsync(CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref Count);
			Played.TrySetResult();
			return default;
		}
	}

	private sealed class CountingPopup : BaseLogReceiver, IDesktopPopupService
	{
		public int Count;

		public bool Result { get; init; }

		ValueTask<bool> IDesktopPopupService.NotifyAsync(DateTime time, string caption, string message, string iconKey, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref Count);
			return new(Result);
		}
	}

	private sealed class CountingExternal : BaseLogReceiver, IAlertNotificationService
	{
		public int Count;

		public TaskCompletionSource Notified { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		ValueTask IAlertNotificationService.NotifyAsync(AlertNotifications type, long? externalId, LogLevels logLevel, string caption, string message, DateTime time, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref Count);
			Notified.TrySetResult();
			return default;
		}
	}

	private sealed class CountingLog : BaseLogReceiver
	{
		public int Warnings;

		public TaskCompletionSource Written { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		protected override void RaiseLog(LogMessage message)
		{
			if (message.Level == LogLevels.Warning)
			{
				Interlocked.Increment(ref Warnings);
				Written.TrySetResult();
			}

			base.RaiseLog(message);
		}
	}
}
