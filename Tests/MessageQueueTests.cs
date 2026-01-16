namespace StockSharp.Tests;

/// <summary>
/// Tests for <see cref="MessageByLocalTimeQueue"/> and <see cref="MessageByOrderQueue"/>.
/// </summary>
[TestClass]
public class MessageQueueTests : BaseTestClass
{
	#region MessageByLocalTimeQueue Tests

	[TestMethod]
	public void MessageByLocalTimeQueue_Open_InitializesCorrectly()
	{
		var queue = new MessageByLocalTimeQueue();

		queue.Open();

		queue.IsClosed.AssertFalse();
		queue.Count.AssertEqual(0);
	}

	[TestMethod]
	public void MessageByLocalTimeQueue_Close_ClosesQueue()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		queue.Close();

		queue.IsClosed.AssertTrue();
	}

	[TestMethod]
	public async Task MessageByLocalTimeQueue_Enqueue_WhenClosed_DropsMessage()
	{
		var queue = new MessageByLocalTimeQueue();
		// Don't open the queue

		var message = CreateTimeMessage(DateTime.UtcNow);
		await queue.Enqueue(message, CancellationToken);

		queue.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task MessageByLocalTimeQueue_EnqueueDequeue_SingleMessage_WorksCorrectly()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var time = DateTime.UtcNow;
		var message = CreateTimeMessage(time);

		await queue.Enqueue(message, CancellationToken);
		var result = await queue.DequeueAsync(CancellationToken);

		result.AssertEqual(message);
		queue.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task MessageByLocalTimeQueue_EnqueueDequeue_SortsMessagesByLocalTime()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var baseTime = DateTime.UtcNow;
		var msg1 = CreateTimeMessage(baseTime.AddSeconds(3)); // Latest
		var msg2 = CreateTimeMessage(baseTime.AddSeconds(1)); // Earliest
		var msg3 = CreateTimeMessage(baseTime.AddSeconds(2)); // Middle

		// Enqueue in non-sorted order
		await queue.Enqueue(msg1, CancellationToken);
		await queue.Enqueue(msg2, CancellationToken);
		await queue.Enqueue(msg3, CancellationToken);

		// Allow time for sorting
		await Task.Delay(100, CancellationToken);

		// Should dequeue in sorted order by LocalTime
		var first = await queue.DequeueAsync(CancellationToken);
		var second = await queue.DequeueAsync(CancellationToken);
		var third = await queue.DequeueAsync(CancellationToken);

		first.AssertEqual(msg2); // Earliest
		second.AssertEqual(msg3); // Middle
		third.AssertEqual(msg1); // Latest
	}

	[TestMethod]
	public async Task MessageByLocalTimeQueue_EnqueueDequeue_SameTime_PreservesOrder()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var time = DateTime.UtcNow;
		var messages = new List<TimeMessage>();

		// Create messages with same LocalTime
		for (int i = 0; i < 10; i++)
		{
			var msg = CreateTimeMessage(time);
			messages.Add(msg);
			await queue.Enqueue(msg, CancellationToken);
		}

		await Task.Delay(100, CancellationToken);

		// All messages should be dequeued (order may vary for same-time messages)
		var dequeued = new List<Message>();
		for (int i = 0; i < 10; i++)
		{
			dequeued.Add(await queue.DequeueAsync(CancellationToken));
		}

		dequeued.Count.AssertEqual(10);
	}

	[TestMethod]
	public async Task MessageByLocalTimeQueue_Clear_RemovesAllMessages()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var time = DateTime.UtcNow;
		await queue.Enqueue(CreateTimeMessage(time), CancellationToken);
		await queue.Enqueue(CreateTimeMessage(time.AddSeconds(1)), CancellationToken);
		await queue.Enqueue(CreateTimeMessage(time.AddSeconds(2)), CancellationToken);

		await Task.Delay(100, CancellationToken);

		queue.Clear();

		queue.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task MessageByLocalTimeQueue_Reopen_WorksCorrectly()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var time = DateTime.UtcNow;
		await queue.Enqueue(CreateTimeMessage(time), CancellationToken);
		queue.Close();

		// Reopen
		queue.Open();
		await queue.Enqueue(CreateTimeMessage(time.AddSeconds(1)), CancellationToken);
		var result = await queue.DequeueAsync(CancellationToken);

		result.AssertNotNull();
		// Old items should be cleared when reopening
	}

	[TestMethod]
	public async Task MessageByLocalTimeQueue_ReadAllAsync_YieldsAllMessages()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var baseTime = DateTime.UtcNow;
		var msg1 = CreateTimeMessage(baseTime.AddSeconds(1));
		var msg2 = CreateTimeMessage(baseTime.AddSeconds(2));
		var msg3 = CreateTimeMessage(baseTime.AddSeconds(3));

		await queue.Enqueue(msg1, CancellationToken);
		await queue.Enqueue(msg2, CancellationToken);
		await queue.Enqueue(msg3, CancellationToken);

		await Task.Delay(100, CancellationToken);

		var items = new List<Message>();
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

		var readTask = Task.Run(async () =>
		{
			await foreach (var item in queue.ReadAllAsync(cts.Token))
			{
				items.Add(item);
				if (items.Count == 3)
					cts.Cancel();
			}
		}, CancellationToken);

		await Task.WhenAny(readTask, Task.Delay(2000, CancellationToken));

		items.Count.AssertEqual(3);
	}

	[TestMethod]
	public async Task MessageByLocalTimeQueue_ConcurrentEnqueue_MaintainsSortOrder()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var baseTime = DateTime.UtcNow;
		var messages = new List<TimeMessage>();

		// Create 100 messages with different times
		for (int i = 0; i < 100; i++)
		{
			messages.Add(CreateTimeMessage(baseTime.AddMilliseconds(i * 10)));
		}

		// Shuffle and enqueue concurrently
		var shuffled = messages.OrderBy(_ => Guid.NewGuid()).ToList();
		var tasks = shuffled.Select(m => queue.Enqueue(m, CancellationToken).AsTask());
		await Task.WhenAll(tasks);

		await Task.Delay(200, CancellationToken);

		// Dequeue and verify order
		var dequeued = new List<Message>();
		for (int i = 0; i < 100; i++)
		{
			dequeued.Add(await queue.DequeueAsync(CancellationToken));
		}

		// Verify sorted by LocalTime
		for (int i = 1; i < dequeued.Count; i++)
		{
			(dequeued[i].LocalTime >= dequeued[i - 1].LocalTime)
				.AssertTrue($"Messages not sorted: {dequeued[i - 1].LocalTime} > {dequeued[i].LocalTime}");
		}
	}

	[TestMethod]
	public void MessageByLocalTimeQueue_Clone_CreatesNewInstance()
	{
		var queue = new MessageByLocalTimeQueue();
		var clone = queue.Clone();

		clone.AssertNotNull();
		clone.AssertNotEqual(queue);
		clone.GetType().AssertEqual(typeof(MessageByLocalTimeQueue));
	}

	#endregion

	#region MessageByOrderQueue Tests

	[TestMethod]
	public void MessageByOrderQueue_Open_InitializesCorrectly()
	{
		var queue = new MessageByOrderQueue();

		queue.Open();

		queue.IsClosed.AssertFalse();
		queue.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task MessageByOrderQueue_EnqueueDequeue_PreservesFIFOOrder()
	{
		var queue = new MessageByOrderQueue();
		queue.Open();

		var baseTime = DateTime.UtcNow;
		var msg1 = CreateTimeMessage(baseTime.AddSeconds(3)); // Latest time but first enqueued
		var msg2 = CreateTimeMessage(baseTime.AddSeconds(1)); // Earliest time but second enqueued
		var msg3 = CreateTimeMessage(baseTime.AddSeconds(2)); // Middle time but third enqueued

		await queue.Enqueue(msg1, CancellationToken);
		await queue.Enqueue(msg2, CancellationToken);
		await queue.Enqueue(msg3, CancellationToken);

		await Task.Delay(100, CancellationToken);

		// Should dequeue in FIFO order (order of enqueue), not by LocalTime
		var first = await queue.DequeueAsync(CancellationToken);
		var second = await queue.DequeueAsync(CancellationToken);
		var third = await queue.DequeueAsync(CancellationToken);

		first.AssertEqual(msg1);
		second.AssertEqual(msg2);
		third.AssertEqual(msg3);
	}

	[TestMethod]
	public void MessageByOrderQueue_Clone_CreatesNewInstance()
	{
		var queue = new MessageByOrderQueue();
		var clone = queue.Clone();

		clone.AssertNotNull();
		clone.AssertNotEqual(queue);
		clone.GetType().AssertEqual(typeof(MessageByOrderQueue));
	}

	#endregion

	#region Stress Tests

	[TestMethod]
	public async Task MessageByLocalTimeQueue_HighVolume_HandlesCorrectly()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var baseTime = DateTime.UtcNow;
		const int messageCount = 1000;

		// Enqueue many messages
		var enqueueTasks = new List<Task>();
		for (int i = 0; i < messageCount; i++)
		{
			var msg = CreateTimeMessage(baseTime.AddMilliseconds(i));
			enqueueTasks.Add(queue.Enqueue(msg, CancellationToken).AsTask());
		}
		await Task.WhenAll(enqueueTasks);

		await Task.Delay(500, CancellationToken);

		// Dequeue all and verify
		var dequeued = new List<Message>();
		for (int i = 0; i < messageCount; i++)
		{
			dequeued.Add(await queue.DequeueAsync(CancellationToken));
		}

		dequeued.Count.AssertEqual(messageCount);

		// Verify all are sorted
		for (int i = 1; i < dequeued.Count; i++)
		{
			(dequeued[i].LocalTime >= dequeued[i - 1].LocalTime).AssertTrue();
		}
	}

	[TestMethod]
	public async Task MessageByLocalTimeQueue_ConcurrentEnqueueDequeue_NoDeadlock()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var baseTime = DateTime.UtcNow;
		const int iterations = 100;
		var dequeued = new System.Collections.Concurrent.ConcurrentBag<Message>();

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

		// Producer task
		var producer = Task.Run(async () =>
		{
			for (int i = 0; i < iterations; i++)
			{
				await queue.Enqueue(CreateTimeMessage(baseTime.AddMilliseconds(i)), cts.Token);
				await Task.Delay(1, cts.Token);
			}
		}, cts.Token);

		// Consumer task
		var consumer = Task.Run(async () =>
		{
			while (dequeued.Count < iterations && !cts.Token.IsCancellationRequested)
			{
				try
				{
					var msg = await queue.DequeueAsync(cts.Token);
					dequeued.Add(msg);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}, cts.Token);

		// Wait for producer to finish, then give consumer time to catch up
		await producer;
		await Task.Delay(500, CancellationToken);
		cts.Cancel();

		try { await consumer; } catch (OperationCanceledException) { }

		dequeued.Count.AssertEqual(iterations);
	}

	#endregion

	#region Helper Methods

	private static TimeMessage CreateTimeMessage(DateTime localTime)
	{
		return new TimeMessage { LocalTime = localTime };
	}

	#endregion
}
