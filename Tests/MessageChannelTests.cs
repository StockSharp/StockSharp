namespace StockSharp.Tests;

/// <summary>
/// Common tests for all <see cref="IMessageChannel"/> implementations.
/// Uses DataRow to test both InMemoryMessageChannel and AsyncMessageChannel.
/// </summary>
[TestClass]
public class MessageChannelTests : BaseTestClass
{
	public enum ChannelType
	{
		InMemory,
		Async,
	}

	private static IMessageChannel CreateChannel(ChannelType type, Action<Exception> errorHandler = null)
	{
		errorHandler ??= _ => { };

		return type switch
		{
			ChannelType.InMemory => new InMemoryMessageChannel(
				new MessageByOrderQueue(),
				"TestChannel",
				errorHandler),
			ChannelType.Async => new AsyncMessageChannel(
				new PassThroughMessageAdapter(new IncrementalIdGenerator()) { MaxParallelMessages = 1 }),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}

	#region Basic State Operations

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public void Open_ChangesStateToStarted(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);

		channel.Open();

		channel.State.AssertEqual(ChannelStates.Started);
	}

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public void Close_ChangesStateToStopped(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);
		channel.Open();

		channel.Close();

		channel.State.AssertEqual(ChannelStates.Stopped);
	}

	/// <summary>
	/// Note: AsyncMessageChannel doesn't have Disabled property, so this test only runs for InMemory.
	/// </summary>
	[TestMethod]
	public void InMemoryChannel_Disabled_PreventsOpening()
	{
		var channel = new InMemoryMessageChannel(new MessageByOrderQueue(), "TestChannel", _ => { });
		channel.Disabled = true;

		channel.Open();

		channel.State.AssertEqual(ChannelStates.Stopped);
		channel.Dispose();
	}

	#endregion

	#region Send Message Operations

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public async Task SendInMessageAsync_WhenClosed_DropsMessage(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);
		var processed = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += (msg, ct) =>
		{
			processed.TrySetResult(true);
			return default;
		};

		// Don't open the channel
		await channel.SendInMessageAsync(CreateTimeMessage(DateTime.UtcNow), CancellationToken);

		await Task.Delay(100, CancellationToken);
		processed.Task.IsCompleted.AssertFalse();
	}

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public async Task SendInMessageAsync_WhenOpen_ProcessesMessage(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);
		var processed = AsyncHelper.CreateTaskCompletionSource<Message>();

		channel.NewOutMessageAsync += (msg, ct) =>
		{
			// For AsyncMessageChannel, first message is ConnectMessage
			if (msg is ConnectMessage)
				processed.TrySetResult(msg);
			// For InMemoryMessageChannel, it's TimeMessage
			else if (msg is TimeMessage)
				processed.TrySetResult(msg);
			return default;
		};

		channel.Open();

		// AsyncMessageChannel requires ConnectMessage first
		if (channelType == ChannelType.Async)
		{
			var message = new ConnectMessage();
			await channel.SendInMessageAsync(message, CancellationToken);
			var result = await processed.Task.WithCancellation(CancellationToken);
			result.AssertEqual(message);
		}
		else
		{
			var message = CreateTimeMessage(DateTime.UtcNow);
			await channel.SendInMessageAsync(message, CancellationToken);
			var result = await processed.Task.WithCancellation(CancellationToken);
			result.AssertEqual(message);
		}
	}

	#endregion

	#region Suspend/Resume

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public async Task Suspend_PausesProcessing(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);
		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var messageProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += (msg, ct) =>
		{
			if (msg is ConnectMessage)
				connected.TrySetResult(true);
			else if (msg is TimeMessage)
				messageProcessed.TrySetResult(true);
			return default;
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		channel.Suspend();
		channel.State.AssertEqual(ChannelStates.Suspended);

		await channel.SendInMessageAsync(CreateTimeMessage(DateTime.UtcNow), CancellationToken);
		await Task.Delay(300, CancellationToken);
		messageProcessed.Task.IsCompleted.AssertFalse();

		channel.Resume();
		await messageProcessed.Task.WithCancellation(CancellationToken);
	}

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public void Suspend_ChangesStateToSuspended(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);
		channel.Open();

		channel.Suspend();

		channel.State.AssertEqual(ChannelStates.Suspended);
	}

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public void Resume_ChangesStateToStarted(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);
		channel.Open();
		channel.Suspend();

		channel.Resume();

		channel.State.AssertEqual(ChannelStates.Started);
	}

	#endregion

	#region Clear

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public async Task Clear_RemovesPendingMessages(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);
		var processedCount = 0;
		var gate = AsyncHelper.CreateTaskCompletionSource<bool>();
		var firstNonConnectProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();
		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += async (msg, ct) =>
		{
			if (msg is ConnectMessage)
			{
				connected.TrySetResult(true);
				return;
			}

			if (Interlocked.Increment(ref processedCount) == 1)
			{
				firstNonConnectProcessed.TrySetResult(true);
				await gate.Task;
			}
		};

		channel.Open();

		// AsyncMessageChannel requires ConnectMessage first
		if (channelType == ChannelType.Async)
		{
			await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
			await connected.Task.WithCancellation(CancellationToken);
		}

		// Send multiple messages
		var baseTime = DateTime.UtcNow;
		for (int i = 0; i < 10; i++)
		{
			await channel.SendInMessageAsync(CreateTimeMessage(baseTime.AddSeconds(i)), CancellationToken);
		}

		// Wait for first to start processing
		await firstNonConnectProcessed.Task.WithCancellation(CancellationToken);

		// Clear pending messages
		channel.Clear();

		// Release first message
		gate.TrySetResult(true);

		await Task.Delay(200, CancellationToken);

		// Only first message should have been processed (rest cleared)
		// Note: The exact count may vary slightly due to timing, but should be much less than 10
		(processedCount < 5).AssertTrue($"Expected less than 5 processed, got {processedCount}");
	}

	#endregion

	#region Error Handling

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public async Task ErrorInHandler_DoesNotStopChannel(ChannelType channelType)
	{
		var errorCaught = AsyncHelper.CreateTaskCompletionSource<Exception>();
		var secondMessageProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();
		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var messageCount = 0;

		IMessageChannel channel;
		if (channelType == ChannelType.InMemory)
		{
			channel = new InMemoryMessageChannel(
				new MessageByOrderQueue(),
				"TestChannel",
				ex => errorCaught.TrySetResult(ex));
		}
		else
		{
			var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
			{
				MaxParallelMessages = 1,
				FaultDelay = TimeSpan.Zero,
			};
			channel = new AsyncMessageChannel(adapter);
		}

		using var _ = (IDisposable)channel;

		channel.NewOutMessageAsync += (msg, ct) =>
		{
			if (msg is ConnectMessage)
			{
				connected.TrySetResult(true);
				return default;
			}

			var count = Interlocked.Increment(ref messageCount);
			if (count == 1)
				throw new InvalidOperationException("Test error");
			if (count == 2)
				secondMessageProcessed.TrySetResult(true);
			return default;
		};

		channel.Open();

		// AsyncMessageChannel requires ConnectMessage first
		if (channelType == ChannelType.Async)
		{
			await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
			await connected.Task.WithCancellation(CancellationToken);
		}

		await channel.SendInMessageAsync(CreateTimeMessage(DateTime.UtcNow), CancellationToken);
		await channel.SendInMessageAsync(CreateTimeMessage(DateTime.UtcNow.AddSeconds(1)), CancellationToken);

		// Second message should still be processed after error in first
		await secondMessageProcessed.Task.WithCancellation(CancellationToken);

		channel.State.AssertEqual(ChannelStates.Started);
	}

	#endregion

	#region Clone

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public void Clone_CreatesIndependentChannel(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);
		channel.Open();

		using var clone = channel.Clone();

		clone.AssertNotNull();
		clone.AssertNotEqual(channel);
		clone.State.AssertEqual(ChannelStates.Stopped); // Clone starts closed
	}

	#endregion

	#region Concurrent Operations

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public async Task ConcurrentSend_AllMessagesProcessed(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);
		var processedMessages = new System.Collections.Concurrent.ConcurrentBag<Message>();
		var allProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();
		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		const int messageCount = 50;

		channel.NewOutMessageAsync += (msg, ct) =>
		{
			if (msg is ConnectMessage)
			{
				connected.TrySetResult(true);
				return default;
			}

			processedMessages.Add(msg);
			if (processedMessages.Count == messageCount)
				allProcessed.TrySetResult(true);
			return default;
		};

		channel.Open();

		// AsyncMessageChannel requires ConnectMessage first
		if (channelType == ChannelType.Async)
		{
			await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
			await connected.Task.WithCancellation(CancellationToken);
		}

		// Send messages concurrently
		var baseTime = DateTime.UtcNow;
		var tasks = Enumerable.Range(0, messageCount)
			.Select(i => channel.SendInMessageAsync(CreateTimeMessage(baseTime.AddMilliseconds(i)), CancellationToken).AsTask());
		await Task.WhenAll(tasks);

		await allProcessed.Task.WithCancellation(CancellationToken);

		processedMessages.Count.AssertEqual(messageCount);
	}

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public async Task RapidOpenClose_NoDeadlock(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);

		for (int i = 0; i < 10; i++)
		{
			channel.Open();
			await channel.SendInMessageAsync(CreateTimeMessage(DateTime.UtcNow), CancellationToken);
			channel.Close();
		}

		// If we get here without hanging, test passes
	}

	#endregion

	#region Reopen

	[TestMethod]
	[DataRow(ChannelType.InMemory)]
	[DataRow(ChannelType.Async)]
	public async Task Reopen_WorksAfterClose(ChannelType channelType)
	{
		using var channel = CreateChannel(channelType);

		var connectCount = 0;
		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += (message, token) =>
		{
			if (message is ConnectMessage)
			{
				Interlocked.Increment(ref connectCount);
				connected.TrySetResult(true);
			}
			return default;
		};

		// First open
		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);
		connectCount.AssertEqual(1);

		// Close
		channel.Close();
		channel.State.AssertEqual(ChannelStates.Stopped);

		// Reopen
		connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);
		connectCount.AssertEqual(2);
	}

	#endregion

	#region Helper Methods

	private static TimeMessage CreateTimeMessage(DateTime localTime)
	{
		return new TimeMessage { LocalTime = localTime };
	}

	private static ExecutionMessage CreateExecutionMessage(DateTime localTime)
	{
		return new ExecutionMessage
		{
			LocalTime = localTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100,
			TradeVolume = 1
		};
	}

	private static OrderRegisterMessage CreateOrderMessage(DateTime localTime)
	{
		return new OrderRegisterMessage
		{
			LocalTime = localTime,
			TransactionId = localTime.Ticks,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
			Price = 100,
			Volume = 1
		};
	}

	private static IMessageChannel CreateInMemoryChannel(IMessageQueue queue)
	{
		return new InMemoryMessageChannel(queue, "TestChannel", _ => { });
	}

	#endregion

	#region InMemoryChannel Queue Ordering

	[TestMethod]
	public async Task InMemoryChannel_MessagesByLocalTimeQueue_ProcessedInTimeOrder()
	{
		using var channel = CreateInMemoryChannel(new MessageByLocalTimeQueue());
		var processedMessages = new List<Message>();
		var allProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += (msg, ct) =>
		{
			lock (processedMessages)
			{
				processedMessages.Add(msg);
				if (processedMessages.Count == 3)
					allProcessed.TrySetResult(true);
			}
			return default;
		};

		channel.Open();

		var baseTime = DateTime.UtcNow;
		var msg1 = CreateTimeMessage(baseTime.AddSeconds(3)); // Latest
		var msg2 = CreateTimeMessage(baseTime.AddSeconds(1)); // Earliest
		var msg3 = CreateTimeMessage(baseTime.AddSeconds(2)); // Middle

		// Send in non-sorted order
		await channel.SendInMessageAsync(msg1, CancellationToken);
		await channel.SendInMessageAsync(msg2, CancellationToken);
		await channel.SendInMessageAsync(msg3, CancellationToken);

		await allProcessed.Task.WithCancellation(CancellationToken);

		// Should be processed in LocalTime order
		processedMessages[0].AssertEqual(msg2); // Earliest
		processedMessages[1].AssertEqual(msg3); // Middle
		processedMessages[2].AssertEqual(msg1); // Latest
	}

	[TestMethod]
	public async Task InMemoryChannel_MessagesByOrderQueue_ProcessedInFIFOOrder()
	{
		using var channel = CreateInMemoryChannel(new MessageByOrderQueue());
		var processedMessages = new List<Message>();
		var allProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += (msg, ct) =>
		{
			lock (processedMessages)
			{
				processedMessages.Add(msg);
				if (processedMessages.Count == 3)
					allProcessed.TrySetResult(true);
			}
			return default;
		};

		channel.Open();

		var baseTime = DateTime.UtcNow;
		var msg1 = CreateTimeMessage(baseTime.AddSeconds(3)); // Latest time, first sent
		var msg2 = CreateTimeMessage(baseTime.AddSeconds(1)); // Earliest time, second sent
		var msg3 = CreateTimeMessage(baseTime.AddSeconds(2)); // Middle time, third sent

		await channel.SendInMessageAsync(msg1, CancellationToken);
		await channel.SendInMessageAsync(msg2, CancellationToken);
		await channel.SendInMessageAsync(msg3, CancellationToken);

		await allProcessed.Task.WithCancellation(CancellationToken);

		// Should be processed in FIFO order (send order)
		processedMessages[0].AssertEqual(msg1);
		processedMessages[1].AssertEqual(msg2);
		processedMessages[2].AssertEqual(msg3);
	}

	[TestMethod]
	public async Task InMemoryChannel_MessageByLocalTimeQueue_OutOfOrderTimes_SortsCorrectly()
	{
		using var channel = CreateInMemoryChannel(new MessageByLocalTimeQueue());
		var processedMessages = new List<Message>();
		var allProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();
		const int messageCount = 50;

		channel.NewOutMessageAsync += (msg, ct) =>
		{
			lock (processedMessages)
			{
				processedMessages.Add(msg);
				if (processedMessages.Count == messageCount)
					allProcessed.TrySetResult(true);
			}
			return default;
		};

		channel.Open();

		var baseTime = DateTime.UtcNow;
		var messages = new List<TimeMessage>();

		// Create messages with shuffled times
		for (int i = 0; i < messageCount; i++)
		{
			messages.Add(CreateTimeMessage(baseTime.AddMilliseconds(i * 100)));
		}

		// Shuffle and send
		var shuffled = messages.OrderBy(_ => Guid.NewGuid()).ToList();
		foreach (var msg in shuffled)
		{
			await channel.SendInMessageAsync(msg, CancellationToken);
		}

		await allProcessed.Task.WithCancellation(CancellationToken);

		// Verify messages are processed in LocalTime order
		for (int i = 1; i < processedMessages.Count; i++)
		{
			(processedMessages[i].LocalTime >= processedMessages[i - 1].LocalTime)
				.AssertTrue($"Time order violation at index {i}: {processedMessages[i - 1].LocalTime} > {processedMessages[i].LocalTime}");
		}
	}

	[TestMethod]
	public async Task InMemoryChannel_MessageByLocalTimeQueue_FutureTimeMessage_ProcessedAfterCurrent()
	{
		using var channel = CreateInMemoryChannel(new MessageByLocalTimeQueue());
		var processedMessages = new List<Message>();
		var allProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += (msg, ct) =>
		{
			lock (processedMessages)
			{
				processedMessages.Add(msg);
				if (processedMessages.Count == 3)
					allProcessed.TrySetResult(true);
			}
			return default;
		};

		channel.Open();

		var now = DateTime.UtcNow;
		var pastMsg = CreateTimeMessage(now.AddSeconds(-10)); // Past
		var currentMsg = CreateTimeMessage(now); // Current
		var futureMsg = CreateTimeMessage(now.AddSeconds(10)); // Future

		// Send in mixed order
		await channel.SendInMessageAsync(futureMsg, CancellationToken);
		await channel.SendInMessageAsync(pastMsg, CancellationToken);
		await channel.SendInMessageAsync(currentMsg, CancellationToken);

		await allProcessed.Task.WithCancellation(CancellationToken);

		// Should be in time order: past, current, future
		processedMessages[0].AssertEqual(pastMsg);
		processedMessages[1].AssertEqual(currentMsg);
		processedMessages[2].AssertEqual(futureMsg);
	}

	/// <summary>
	/// Simulates a backtest scenario where historical data messages are sent
	/// and commands (like OrderRegister) are interleaved.
	/// This is a key test for the time ordering in backtesting.
	/// </summary>
	[TestMethod]
	public async Task InMemoryChannel_BacktestSimulation_CommandsAndDataMixedTiming()
	{
		using var channel = CreateInMemoryChannel(new MessageByLocalTimeQueue());
		var processedMessages = new List<(Message Message, DateTime ProcessedAt)>();
		var allProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();
		const int expectedCount = 6;

		channel.NewOutMessageAsync += (msg, ct) =>
		{
			lock (processedMessages)
			{
				processedMessages.Add((msg, DateTime.UtcNow));
				if (processedMessages.Count == expectedCount)
					allProcessed.TrySetResult(true);
			}
			return default;
		};

		channel.Open();

		// Simulate backtest: historical data at t=10, t=20, t=30 seconds
		// And commands at "current" times (which may be different)
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		// Historical market data messages
		var tick1 = CreateExecutionMessage(baseTime.AddSeconds(10));
		var tick2 = CreateExecutionMessage(baseTime.AddSeconds(20));
		var tick3 = CreateExecutionMessage(baseTime.AddSeconds(30));

		// Command messages - in real backtest, these get LocalTime from when they're created
		// which might be "ahead" of the historical data being processed
		var order1 = CreateOrderMessage(baseTime.AddSeconds(15)); // Should be between tick1 and tick2
		var order2 = CreateOrderMessage(baseTime.AddSeconds(25)); // Should be between tick2 and tick3
		var order3 = CreateOrderMessage(baseTime.AddSeconds(35)); // Should be after tick3

		// Send in random order (simulating async processing)
		await channel.SendInMessageAsync(order2, CancellationToken);
		await channel.SendInMessageAsync(tick3, CancellationToken);
		await channel.SendInMessageAsync(tick1, CancellationToken);
		await channel.SendInMessageAsync(order1, CancellationToken);
		await channel.SendInMessageAsync(tick2, CancellationToken);
		await channel.SendInMessageAsync(order3, CancellationToken);

		await allProcessed.Task.WithCancellation(CancellationToken);

		// Verify strict time ordering
		var orderedMessages = processedMessages.OrderBy(x => x.Message.LocalTime).ToList();
		for (int i = 0; i < processedMessages.Count; i++)
		{
			processedMessages[i].Message.AssertEqual(orderedMessages[i].Message,
				$"Message at position {i} is out of order");
		}

		// Verify specific order: tick1, order1, tick2, order2, tick3, order3
		processedMessages[0].Message.AssertEqual(tick1);
		processedMessages[1].Message.AssertEqual(order1);
		processedMessages[2].Message.AssertEqual(tick2);
		processedMessages[3].Message.AssertEqual(order2);
		processedMessages[4].Message.AssertEqual(tick3);
		processedMessages[5].Message.AssertEqual(order3);
	}

	[TestMethod]
	public void InMemoryChannel_MessageCount_ReturnsQueueCount()
	{
		var channel = new InMemoryMessageChannel(new MessageByOrderQueue(), "TestChannel", _ => { });

		// When closed, messages are dropped
		channel.MessageCount.AssertEqual(0);

		channel.Dispose();
	}

	#endregion
}
