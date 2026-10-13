namespace StockSharp.Tests;

using System.Runtime.CompilerServices;

using StockSharp.Alerts;

/// <summary>
/// Every alert channel reaches the sink that serves it, and none reaches the others.
/// </summary>
[TestClass]
[DoNotParallelize]
public class AlertNotificationRouterTests : BaseTestClass
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
		=> Create(10, external, sound, popup, out log);

	private static AlertNotificationRouter Create(
		int maxQueue,
		IAlertNotificationService external,
		IAlertSoundService sound,
		IDesktopPopupService popup,
		out CountingLog log)
	{
		log = new CountingLog();
		return new(maxQueue, external, sound, popup, log);
	}

	[TestMethod]
	public async Task Popup_CarriesTheIconItsLevelDeserves()
	{
		// A popup that always looks like a warning tells the user nothing about which alert fired.
		foreach (var (level, expected) in new[]
		{
			(LogLevels.Info, "Info"),
			(LogLevels.Warning, "Warning"),
			(LogLevels.Error, "Error"),
			(LogLevels.Debug, "Debug"),
		})
		{
			var popup = new CountingPopup();
			using var router = Create(new CountingExternal(), new CountingSound(), popup, out _);

			await ((IAlertNotificationService)router).NotifyAsync(
				AlertNotifications.Popup, null, level, "caption", "message", DateTime.UtcNow, default);

			popup.LastIconKey.AssertEqual(expected);
		}
	}

	[TestMethod]
	public async Task Log_AndPopup_ShowTheTimeTheUserConfigured()
	{
		var original = AppTime.TimeZone;

		try
		{
			// A person reading an alert wants the time on their own clock, not on UTC.
			AppTime.TimeZone = TimeZoneInfo.CreateCustomTimeZone("alerts-test", TimeSpan.FromHours(5), "alerts", "alerts");

			var popup = new CountingPopup();
			using var router = Create(new CountingExternal(), new CountingSound(), popup, out var log);
			var time = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

			await ((IAlertNotificationService)router).NotifyAsync(
				AlertNotifications.Log, null, LogLevels.Warning, "caption", "message", time, default);

			await log.Written.Task.WaitAsync(TimeSpan.FromSeconds(5));

			// The message carries whatever the current culture formats, so the moment is checked
			// rather than the text: five hours east of UTC 10:00 is 15:00 the same day.
			log.LastMessage.Contains(time.ToAppTime().ToString()).AssertTrue(log.LastMessage);
			time.ToAppTime().Hour.AssertEqual(15);
		}
		finally
		{
			AppTime.TimeZone = original;
		}
	}

	[TestMethod]
	public async Task Popup_SaysNothingAboutALevelItDoesNotKnow()
	{
		var popup = new CountingPopup();
		using var router = Create(new CountingExternal(), new CountingSound(), popup, out _);

		await ((IAlertNotificationService)router).NotifyAsync(
			AlertNotifications.Popup, null, LogLevels.Verbose, "caption", "message", DateTime.UtcNow, default);

		popup.LastIconKey.IsEmpty().AssertTrue();
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ExternalProviderFailure_IsReported_AndLaterAlertsStillReachTheirChannels()
	{
		// A transport that refuses one alert has failed that alert, not the router: the
		// refusal is visible in the error log, and the next alerts are still delivered -
		// the sound one to the sound service, the next Telegram one to the same provider.
		var external = new CountingExternal();
		external.FailNext(1);

		var sound = new CountingSound();
		using var router = Create(external, sound, new CountingPopup(), out _);

		var errors = new ErrorSink();
		errors.Attach(router);

		await Notify(router, AlertNotifications.Telegram, CancellationToken);
		await errors.Raised.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		await Notify(router, AlertNotifications.Sound, CancellationToken);
		await sound.Played.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		await Notify(router, AlertNotifications.Telegram, CancellationToken);
		await WaitFor(() => external.Delivered == 1, CancellationToken);

		external.Count.AssertEqual(2, "Both Telegram alerts must have been offered to the provider");
		external.Delivered.AssertEqual(1, "The alert sent after the refusal must be delivered");
		sound.Delivered.AssertEqual(1, "A failing provider must not hold up another channel");
		errors.Count.AssertEqual(1, "A refused delivery must be reported, not swallowed");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SoundFailure_IsReported_AndLaterAlertsStillReachTheirChannels()
	{
		// The same promise for the sound device: one alert it cannot play is one alert lost,
		// so the failure is logged and the alerts queued behind it are still routed.
		var sound = new CountingSound();
		sound.FailNext(1);

		var external = new CountingExternal();
		using var router = Create(external, sound, new CountingPopup(), out _);

		var errors = new ErrorSink();
		errors.Attach(router);

		await Notify(router, AlertNotifications.Sound, CancellationToken);
		await errors.Raised.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		await Notify(router, AlertNotifications.Telegram, CancellationToken);
		await external.Notified.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		await Notify(router, AlertNotifications.Sound, CancellationToken);
		await sound.Played.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		sound.Count.AssertEqual(2, "Both sound alerts must have reached the sound service");
		sound.Delivered.AssertEqual(1, "The alert sent after the failure must be played");
		external.Delivered.AssertEqual(1, "A failing sound device must not hold up another channel");
		errors.Count.AssertEqual(1, "A sound that could not be played must be reported, not swallowed");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Dispose_DoesNotStrandACallerWaitingOnAFullQueue()
	{
		// A caller blocked because the queue is full has no other way to learn the router
		// went away, so shutting down must complete that call rather than leave it waiting -
		// and must not complete it as accepted, since nothing will ever deliver it.
		var sound = new CountingSound { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
		var router = Create(1, new CountingExternal(), sound, new CountingPopup(), out _);

		try
		{
			// A queue of one: the first alert is taken by the dispatcher and held inside
			// PlayAsync, the second fills the queue, so the third has nowhere to go.
			await Notify(router, AlertNotifications.Sound, CancellationToken);
			await sound.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
			await Notify(router, AlertNotifications.Sound, CancellationToken);

			var pending = Notify(router, AlertNotifications.Sound, CancellationToken).AsTask();

			pending.IsCompleted.AssertFalse("The third alert cannot be queued while the queue is full");

			router.Dispose();

			await Task.WhenAny(pending, Task.Delay(TimeSpan.FromSeconds(5), CancellationToken));

			pending.IsCompleted.AssertTrue("Disposing must not leave a caller waiting forever");
			pending.IsCompletedSuccessfully.AssertFalse("An alert that was never queued must not be reported as accepted");

			// observed here so the failure is not raised later as an unobserved task exception
			await ThrowsAsync<Exception>(() => pending);
		}
		finally
		{
			sound.Gate.TrySetResult();
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Notify_AfterDispose_DoesNotAcceptAnAlertItCannotDeliver()
	{
		// Once the dispatcher is stopped nothing will ever carry the alert, so accepting it
		// would tell the caller a delivery is under way that never starts.
		var external = new CountingExternal();
		var router = Create(external, new CountingSound(), new CountingPopup(), out _);

		router.Dispose();

		await ThrowsAsync<Exception>(async () => await Notify(router, AlertNotifications.Telegram, CancellationToken));

		external.Count.AssertEqual(0, "Nothing may be delivered after the router is disposed");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Dispose_WithAnAlertStillQueued_DoesNotReportItsOwnShutdownAsAnError()
	{
		// Stopping the dispatcher cancels whatever it was waiting on; that cancellation is
		// the shutdown working as asked, not a delivery that failed, so nothing is logged.
		var sound = new CountingSound { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
		var router = Create(1, new CountingExternal(), sound, new CountingPopup(), out _);

		var errors = new ErrorSink();
		errors.Attach(router);

		try
		{
			await Notify(router, AlertNotifications.Sound, CancellationToken);
			await sound.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
			await Notify(router, AlertNotifications.Sound, CancellationToken);

			router.Dispose();

			await Task.Delay(500, CancellationToken);

			errors.Count.AssertEqual(0, "Shutting down is not a delivery failure");
		}
		finally
		{
			sound.Gate.TrySetResult();
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task DispatchAfterDispose_EndsTheLoop()
	{
		// Shutting the router down stops its dispatcher for good. A loop that outlived the shutdown
		// would keep reading from a channel that is closed, report every read as a failure and spin
		// through the log at the speed of the machine - so the alert still queued must be left where
		// it is, and the shutdown itself must be reported as nothing at all.
		var sound = new CountingSound { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
		var router = Create(1, new CountingExternal(), sound, new CountingPopup(), out _);

		var errors = new ErrorSink();
		errors.Attach(router);

		try
		{
			// A queue of one: the first alert is taken by the dispatcher and held inside PlayAsync,
			// the second waits in the queue for a dispatcher that is about to go away.
			await Notify(router, AlertNotifications.Sound, CancellationToken);
			await sound.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
			await Notify(router, AlertNotifications.Sound, CancellationToken);

			router.Dispose();

			// Releasing what the dispatcher was waiting on gives a surviving loop every chance to
			// take the queued alert, so the count below is a real answer and not a matter of timing.
			sound.Gate.TrySetResult();

			await Task.Delay(1_000, CancellationToken);

			sound.Count.AssertEqual(1, "A disposed router must stop dispatching, leaving the queued alert untaken");
			errors.Count.AssertEqual(0, "A stopped dispatcher must not report the channel it closed itself as a failure");
		}
		finally
		{
			sound.Gate.TrySetResult();
		}
	}

	private static ValueTask Notify(IAlertNotificationService router, AlertNotifications type)
		=> router.NotifyAsync(type, null, LogLevels.Warning, "caption", "message", DateTime.UtcNow, default);

	private static ValueTask Notify(IAlertNotificationService router, AlertNotifications type, CancellationToken token)
		=> router.NotifyAsync(type, null, LogLevels.Warning, "caption", "message", DateTime.UtcNow, token);

	private static Task WaitFor(Func<bool> condition, CancellationToken token,
		[CallerArgumentExpression(nameof(condition))] string expectation = null)
		=> Helper.WaitUntilAsync(condition, TimeSpan.FromSeconds(5), token, expectation);

	private sealed class CountingSound : BaseLogReceiver, IAlertSoundService
	{
		private int _failuresLeft;

		// plays started, including the ones that end up refused
		public int Count;

		// plays that finished
		public int Delivered;

		public TaskCompletionSource Played { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		// signalled as soon as a play starts, so a test can tell the dispatcher is busy
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		// holds every started play until this source is completed
		public TaskCompletionSource Gate { get; init; }

		// makes the next plays throw, modelling a sound device that is unavailable
		public void FailNext(int count) => Interlocked.Exchange(ref _failuresLeft, count);

		async ValueTask IAlertSoundService.PlayAsync(CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref Count);
			Entered.TrySetResult();

			var gate = Gate;

			if (gate != null)
				await gate.Task.WaitAsync(cancellationToken);

			if (Volatile.Read(ref _failuresLeft) > 0)
			{
				Interlocked.Decrement(ref _failuresLeft);
				throw new InvalidOperationException("Sound device is unavailable.");
			}

			Interlocked.Increment(ref Delivered);
			Played.TrySetResult();
		}
	}

	private sealed class CountingPopup : BaseLogReceiver, IDesktopPopupService
	{
		public int Count;

		public bool Result { get; init; }

		public string LastIconKey { get; private set; }

		ValueTask<bool> IDesktopPopupService.NotifyAsync(DateTime time, string caption, string message, string iconKey, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref Count);
			LastIconKey = iconKey;
			return new(Result);
		}
	}

	private sealed class CountingExternal : BaseLogReceiver, IAlertNotificationService
	{
		private int _failuresLeft;

		// deliveries started, including the refused ones
		public int Count;

		// deliveries that finished
		public int Delivered;

		public TaskCompletionSource Notified { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		// makes the next deliveries throw, modelling a transport that is down
		public void FailNext(int count) => Interlocked.Exchange(ref _failuresLeft, count);

		ValueTask IAlertNotificationService.NotifyAsync(AlertNotifications type, long? externalId, LogLevels logLevel, string caption, string message, DateTime time, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref Count);

			if (Volatile.Read(ref _failuresLeft) > 0)
			{
				Interlocked.Decrement(ref _failuresLeft);
				throw new InvalidOperationException("The external provider is down.");
			}

			Interlocked.Increment(ref Delivered);
			Notified.TrySetResult();
			return default;
		}
	}

	private sealed class ErrorSink
	{
		private int _count;

		public int Count => _count;

		// signalled by the first error, so a test need not poll for one
		public TaskCompletionSource Raised { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void Attach(ILogSource source) => source.Log += OnLog;

		private void OnLog(LogMessage message)
		{
			if (message.Level != LogLevels.Error)
				return;

			Interlocked.Increment(ref _count);
			Raised.TrySetResult();
		}
	}

	private sealed class CountingLog : BaseLogReceiver
	{
		public int Warnings;

		public TaskCompletionSource Written { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public string LastMessage { get; private set; }

		protected override void RaiseLog(LogMessage message)
		{
			if (message.Level == LogLevels.Warning)
			{
				Interlocked.Increment(ref Warnings);
				LastMessage = message.Message;
				Written.TrySetResult();
			}

			base.RaiseLog(message);
		}
	}
}
