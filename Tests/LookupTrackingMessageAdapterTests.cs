namespace StockSharp.Tests;

/// <summary>
/// What the lookup tracker owes whoever asked for a lookup: an answer. When the adapter underneath
/// never sends a result of its own, the tracker is the only thing standing between the caller and a
/// wait that never ends, so these tests are about when that answer arrives and when it must not.
/// </summary>
[TestClass]
public class LookupTrackingMessageAdapterTests : BaseTestClass
{
	// An adapter that answers nothing by itself: it declares that it never sends a lookup result, so
	// the tracker is the one that has to close the lookup, and it lends the tracker its timeout.
	private sealed class LookupInnerAdapter : MessageAdapter
	{
		public LookupInnerAdapter()
			: base(new IncrementalIdGenerator())
		{
			NotSupportedResultMessages = [MessageTypes.SecurityLookup];
		}

		public List<Message> InMessages { get; } = [];

		/// <summary>How long the tracker waits before closing a lookup itself.</summary>
		public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

		public override TimeSpan? LookupTimeout => Timeout;

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			InMessages.Add(message);
			return default;
		}

		public override IMessageAdapter Clone()
			=> new LookupInnerAdapter { Timeout = Timeout };
	}

	private static (LookupTrackingMessageAdapter adapter, LookupInnerAdapter inner, List<Message> output) CreateSut()
	{
		var inner = new LookupInnerAdapter();
		var adapter = new LookupTrackingMessageAdapter(inner, new LookupTrackingManagerState());

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, _) => { output.Add(m); return default; };

		return (adapter, inner, output);
	}

	// A lookup is a subscription by its nature: the flag is fixed by the message type itself.
	private static SecurityLookupMessage CreateLookup(long transactionId) => new()
	{
		TransactionId = transactionId,
	};

	private static int CountResults(List<Message> output, long transactionId)
		=> output.Count(m => IsResult(m, transactionId));

	private static bool IsResult(Message message, long transactionId)
		=> message switch
		{
			SubscriptionOnlineMessage online => online.OriginalTransactionId == transactionId,
			SubscriptionFinishedMessage finished => finished.OriginalTransactionId == transactionId,
			_ => false,
		};

	/// <summary>
	/// A lookup the adapter underneath never answers must still be closed, so that whoever asked
	/// gets a result instead of waiting for one for good.
	/// </summary>
	[TestMethod]
	public async Task Lookup_NobodyAnswers_IsClosedOnceItsTimeoutHasPassed()
	{
		var (adapter, inner, output) = CreateSut();

		await adapter.SendInMessageAsync(CreateLookup(1), CancellationToken);

		inner.InMessages.OfType<SecurityLookupMessage>().Count(m => m.TransactionId == 1)
			.AssertEqual(1, "the lookup goes on to the adapter that was supposed to answer it");

		var start = new DateTime(2026, 09, 11, 10, 00, 00, DateTimeKind.Utc);

		await inner.SendOutMessageAsync(new TimeMessage { LocalTime = start }, CancellationToken);
		CountResults(output, 1).AssertEqual(0, "no time has passed yet, so the lookup is still open");

		await inner.SendOutMessageAsync(new TimeMessage { LocalTime = start.AddSeconds(11) }, CancellationToken);

		CountResults(output, 1).AssertEqual(1, "the ten seconds are up, so the lookup is closed for the caller");
	}

	/// <summary>
	/// A lookup that is still delivering rows is not late, it is busy. Every row it delivers has to
	/// buy it the whole timeout again, or a long answer would be cut off halfway through.
	/// </summary>
	[TestMethod]
	public async Task Lookup_StillDeliveringRows_IsGivenItsTimeoutAgain()
	{
		var (adapter, inner, output) = CreateSut();

		await adapter.SendInMessageAsync(CreateLookup(1), CancellationToken);

		var start = new DateTime(2026, 09, 11, 10, 00, 00, DateTimeKind.Utc);

		await inner.SendOutMessageAsync(new TimeMessage { LocalTime = start }, CancellationToken);

		// a row of the answer, nine seconds in - one second before the lookup would have been closed
		var row = new SecurityMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			LocalTime = start.AddSeconds(9),
		};
		row.SetSubscriptionIds(subscriptionId: 1);

		await inner.SendOutMessageAsync(row, CancellationToken);

		// eighteen seconds after the lookup started, but only nine after its last row
		await inner.SendOutMessageAsync(new TimeMessage { LocalTime = start.AddSeconds(18) }, CancellationToken);

		CountResults(output, 1).AssertEqual(0, "a lookup that delivered a row nine seconds ago has not gone quiet");

		// and now it really has gone quiet for longer than the timeout
		await inner.SendOutMessageAsync(new TimeMessage { LocalTime = start.AddSeconds(30) }, CancellationToken);

		CountResults(output, 1).AssertEqual(1, "once it stops delivering, the timeout closes it as before");
	}

	/// <summary>
	/// Once the adapter underneath has answered the lookup itself, the tracker has nothing left to
	/// close: a second result later would tell the caller a finished lookup finished twice.
	/// </summary>
	[TestMethod]
	public async Task Lookup_AlreadyAnswered_IsNotClosedASecondTime()
	{
		var (adapter, inner, output) = CreateSut();

		await adapter.SendInMessageAsync(CreateLookup(1), CancellationToken);

		var start = new DateTime(2026, 09, 11, 10, 00, 00, DateTimeKind.Utc);

		await inner.SendOutMessageAsync(new TimeMessage { LocalTime = start }, CancellationToken);

		await inner.SendOutMessageAsync(new SubscriptionOnlineMessage
		{
			OriginalTransactionId = 1,
			LocalTime = start.AddSeconds(1),
		}, CancellationToken);

		CountResults(output, 1).AssertEqual(1, "the answer the adapter itself sent reaches the caller");

		await inner.SendOutMessageAsync(new TimeMessage { LocalTime = start.AddSeconds(60) }, CancellationToken);

		CountResults(output, 1).AssertEqual(1, "a lookup that was answered is not answered again when its old timeout runs out");
	}

	/// <summary>
	/// The timeout is a promise about time, not about traffic. A connection that has gone silent is
	/// exactly the case the timeout exists for, so a lookup has to be closed once its time is up even
	/// though nothing else is coming back through the adapter to notice the time passing.
	/// </summary>
	[TestMethod]
	public async Task Lookup_WithNoOutMessageTraffic_StillTimesOut()
	{
		var (adapter, inner, output) = CreateSut();

		inner.Timeout = TimeSpan.FromMilliseconds(200);

		var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		adapter.NewOutMessageAsync += (m, _) =>
		{
			if (IsResult(m, 1))
				closed.TrySetResult();

			return default;
		};

		await adapter.SendInMessageAsync(CreateLookup(1), CancellationToken);

		// nothing at all comes back through the adapter while the timeout runs out
		var completed = await Task.WhenAny(closed.Task, Task.Delay(TimeSpan.FromSeconds(2), CancellationToken));

		(completed == closed.Task).AssertTrue("a silent connection is what the timeout is for, so the lookup has to be closed without any other message arriving");
		CountResults(output, 1).AssertEqual(1);
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Dispose_CancelsPendingLookupTimeout()
	{
		var (adapter, inner, output) = CreateSut();
		inner.Timeout = TimeSpan.FromMilliseconds(200);

		await adapter.SendInMessageAsync(CreateLookup(1), CancellationToken);
		adapter.Dispose();

		await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken);

		CountResults(output, 1).AssertEqual(0);
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task LookupRowIsNotTimedOutWhileItsHandlerIsRunning()
	{
		var (adapter, inner, output) = CreateSut();
		using (adapter)
		{
			inner.Timeout = TimeSpan.FromMilliseconds(200);

			var handlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var releaseHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var lookupClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			adapter.NewOutMessageAsync += async (message, _) =>
			{
				if (message is SecurityMessage)
				{
					handlerStarted.TrySetResult();
					await releaseHandler.Task;
				}
				else if (IsResult(message, 1))
					lookupClosed.TrySetResult();
			};

			await adapter.SendInMessageAsync(CreateLookup(1), CancellationToken);

			var row = new SecurityMessage { SecurityId = Helper.CreateSecurityId() };
			row.SetSubscriptionIds(subscriptionId: 1);

			var rowTask = inner.SendOutMessageAsync(row, CancellationToken).AsTask();

			try
			{
				await handlerStarted.Task.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken);
				await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken);

				CountResults(output, 1).AssertEqual(0, "a received row keeps its lookup alive while downstream processes it");
			}
			finally
			{
				releaseHandler.TrySetResult();
				await rowTask;
			}

			var completed = await Task.WhenAny(lookupClosed.Task, Task.Delay(TimeSpan.FromSeconds(2), CancellationToken));
			(completed == lookupClosed.Task).AssertTrue("the timeout is rearmed after the row finishes processing");
			CountResults(output, 1).AssertEqual(1);
		}
	}

	[TestMethod]
	public async Task ReentrantReset_DoesNotRestoreTheOldTimeoutClock()
	{
		var (adapter, inner, output) = CreateSut();
		var reset = false;

		adapter.NewOutMessageAsync += async (message, token) =>
		{
			if (message is TimeMessage && !reset)
			{
				reset = true;
				await adapter.SendInMessageAsync(new ResetMessage(), token);
			}
		};

		var beforeReset = new DateTime(2026, 09, 11, 10, 00, 00, DateTimeKind.Utc);
		await inner.SendOutMessageAsync(new TimeMessage { LocalTime = beforeReset }, CancellationToken);

		await adapter.SendInMessageAsync(CreateLookup(2), CancellationToken);

		var firstClockAfterReset = beforeReset.AddSeconds(20);
		await inner.SendOutMessageAsync(new TimeMessage { LocalTime = firstClockAfterReset }, CancellationToken);

		CountResults(output, 2).AssertEqual(0,
			"the first clock message after reset anchors the new session and must not charge its lookup for time before it existed");

		await inner.SendOutMessageAsync(new TimeMessage { LocalTime = firstClockAfterReset.AddSeconds(11) }, CancellationToken);

		CountResults(output, 2).AssertEqual(1, "the lookup still expires after ten seconds in the new session");
	}
}
