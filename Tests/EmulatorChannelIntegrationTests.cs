namespace StockSharp.Tests;

using System.Collections.Concurrent;

using Ecng.Collections;

using StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Low-level integration tests for Emulator + Channel without Connector/Strategy.
/// Tests message ordering at the raw message level.
/// </summary>
[TestClass]
public class EmulatorChannelIntegrationTests : BaseTestClass
{
	#region Channel + Emulator Integration Tests

	/// <summary>
	/// Demonstrates the correct flow: history is sent through the channel, then processed.
	/// Trading logic (order registration) happens from NewOutMessageAsync in response to history data.
	/// Orders are sent BACK TO THE CHANNEL (not directly to emulator).
	/// </summary>
	[TestMethod]
	public async Task HistoryProcessing_OrdersFromHandler_SentToChannel()
	{
		// Setup: Channel with time-sorted queue -> Emulator
		var queue = new MessageByLocalTimeQueue();
		using var channel = new InMemoryMessageChannel(queue, "TestChannel", ex => Fail($"Channel error: {ex.Message}"));

		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };
		var portfolioName = "TestPortfolio";

		// Create emulator
		var secProvider = new CollectionSecurityProvider([new Security { Id = "TEST@TEST" }]);
		var pfProvider = new CollectionPortfolioProvider([Portfolio.CreateSimulator()]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		var emulator = new MarketEmulator(secProvider, pfProvider, exchangeProvider, new IncrementalIdGenerator());

		// Track output message times
		var outputTimes = new ConcurrentBag<(DateTime time, string msgType)>();
		var timeViolations = new ConcurrentBag<string>();
		DateTime lastOutputTime = DateTime.MinValue;
		var outputLock = new object();

		emulator.NewOutMessageAsync += (msg, ct) =>
		{
			lock (outputLock)
			{
				var time = msg.LocalTime;
				if (time < lastOutputTime)
				{
					timeViolations.Add($"Time went backwards: {lastOutputTime:HH:mm:ss.fff} -> {time:HH:mm:ss.fff} ({msg.GetType().Name})");
				}
				lastOutputTime = time > lastOutputTime ? time : lastOutputTime;
				outputTimes.Add((time, msg.GetType().Name));
			}
			return default;
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var transactionId = 1000L;
		var ordersRegistered = 0;
		var ticksProcessed = 0;

		// Setup handler that processes messages and sends orders BACK TO CHANNEL
		channel.NewOutMessageAsync += async (msg, ct) =>
		{
			// First send to emulator
			await ((IMessageTransport)emulator).SendInMessageAsync(msg, ct);

			// Trading logic: when we see a tick, react by registering an order
			if (msg is ExecutionMessage execMsg && execMsg.DataTypeEx == DataType.Ticks)
			{
				Interlocked.Increment(ref ticksProcessed);

				// Every 50 ticks, register an order
				if (ticksProcessed % 50 == 0)
				{
					// Order time = current tick time (always monotonic since channel sorts)
					var order = new OrderRegisterMessage
					{
						SecurityId = securityId,
						PortfolioName = portfolioName,
						LocalTime = msg.LocalTime,
						TransactionId = Interlocked.Increment(ref transactionId),
						Side = Sides.Buy,
						Price = execMsg.TradePrice ?? 100,
						Volume = 1,
						OrderType = OrderTypes.Limit,
					};

					// Send order TO THE CHANNEL, not directly to emulator!
					// Channel will sort it and send to emulator at right time
					await ((IMessageChannel)channel).SendInMessageAsync(order, ct);
					Interlocked.Increment(ref ordersRegistered);
				}
			}
		};

		// Open channel to start processing
		channel.Open();

		// Send reset to initialize emulator
		await ((IMessageChannel)channel).SendInMessageAsync(new ResetMessage(), CancellationToken);

		// Send ticks through channel (not pre-fill queue — channel constructor closes queue)
		for (int i = 0; i < 1000; i++)
		{
			var tickTime = baseTime.AddSeconds(i);

			var tick = new ExecutionMessage
			{
				SecurityId = securityId,
				LocalTime = tickTime,
				ServerTime = tickTime,
				DataTypeEx = DataType.Ticks,
				TradePrice = 100 + i * 0.01m,
				TradeVolume = 1,
				OriginalTransactionId = 0,
			};

			await ((IMessageChannel)channel).SendInMessageAsync(tick, CancellationToken);
		}

		// Wait for processing to complete
		var maxWait = DateTime.UtcNow.AddSeconds(10);
		while (ticksProcessed < 1000 && DateTime.UtcNow < maxWait)
		{
			await Task.Delay(50, CancellationToken);
		}

		// Give time for final orders to process
		await Task.Delay(500, CancellationToken);

		// Verify results
		ticksProcessed.AssertEqual(1000, "Should have processed all ticks");
		ordersRegistered.AssertGreater(0, "Should have registered orders from handler");

		// With proper flow through channel, there should be no time violations
		if (timeViolations.Count > 0)
		{
			Fail($"Time ordering violations detected ({timeViolations.Count}):\n{string.Join("\n", timeViolations.Take(10))}");
		}
	}

	/// <summary>
	/// Tests that sending a message with backward time to the emulator throws an exception.
	/// When a strategy processes tick at T+50 and sends order with time T+50,
	/// but emulator has already processed ticks up to T+99, the emulator must reject it.
	/// </summary>
	[TestMethod]
	public async Task RaceCondition_OrderWithPastTime_EmulatorRejects()
	{
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		var secProvider = new CollectionSecurityProvider([new Security { Id = "TEST@TEST" }]);
		var pfProvider = new CollectionPortfolioProvider([Portfolio.CreateSimulator()]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		var emulator = new MarketEmulator(secProvider, pfProvider, exchangeProvider, new IncrementalIdGenerator());

		emulator.NewOutMessageAsync += (msg, ct) => default;

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		// Reset
		await ((IMessageTransport)emulator).SendInMessageAsync(new ResetMessage(), CancellationToken);

		// Send ticks at T+0 through T+99
		for (int i = 0; i < 100; i++)
		{
			var tickTime = baseTime.AddSeconds(i);
			await ((IMessageTransport)emulator).SendInMessageAsync(new ExecutionMessage
			{
				SecurityId = securityId,
				LocalTime = tickTime,
				ServerTime = tickTime,
				DataTypeEx = DataType.Ticks,
				TradePrice = 100 + i * 0.01m,
				TradeVolume = 1,
			}, CancellationToken);
		}

		// Emulator is now at T+99. Sending order with time T+50 should throw.
		await ThrowsAsync<InvalidOperationException>(async () =>
		{
			await ((IMessageTransport)emulator).SendInMessageAsync(new OrderRegisterMessage
			{
				SecurityId = securityId,
				PortfolioName = "TestPortfolio",
				LocalTime = baseTime.AddSeconds(50),
				TransactionId = 1,
				Side = Sides.Buy,
				Price = 100,
				Volume = 1,
				OrderType = OrderTypes.Limit,
			}, CancellationToken);
		});
	}

	/// <summary>
	/// Tests concurrent enqueue during dequeue with large dataset.
	/// Verifies all items are eventually processed (ordering is not guaranteed with concurrent operations).
	/// </summary>
	[TestMethod]
	public async Task ConcurrentEnqueueDuringDequeue_LargeDataset_AllItemsProcessed()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var baseTime = DateTime.UtcNow;
		const int totalMessages = 10000;
		const int producerCount = 4;
		var messagesPerProducer = totalMessages / producerCount;

		var dequeued = new ConcurrentBag<Message>();
		var cts = new CancellationTokenSource();
		var producersDone = new TaskCompletionSource<bool>();

		// Consumer task - continuously dequeue
		var consumerTask = Task.Run(async () =>
		{
			while (!cts.Token.IsCancellationRequested)
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

			// Drain remaining after cancellation
			while (queue.Count > 0)
			{
				try
				{
					using var drainCts = new CancellationTokenSource(100);
					var msg = await queue.DequeueAsync(drainCts.Token);
					dequeued.Add(msg);
				}
				catch { break; }
			}
		}, cts.Token);

		// Producer tasks - enqueue messages with random times
		var random = new Random(42);
		var producerTasks = Enumerable.Range(0, producerCount).Select(producerId => Task.Run(async () =>
		{
			var localRandom = new Random(42 + producerId);
			for (int i = 0; i < messagesPerProducer; i++)
			{
				// Random time within range to force reordering
				var randomOffset = localRandom.Next(0, totalMessages);
				var time = baseTime.AddMilliseconds(randomOffset);

				var msg = new TimeMessage { LocalTime = time };
				await queue.Enqueue(msg, CancellationToken);

				// Occasional yield to interleave with consumer
				if (i % 100 == 0)
					await Task.Yield();
			}
		})).ToArray();

		// Wait for producers
		await Task.WhenAll(producerTasks);

		// Give consumer time to finish draining
		await Task.Delay(2000, CancellationToken);
		cts.Cancel();

		try { await consumerTask; } catch (OperationCanceledException) { }

		// Verify all messages received
		dequeued.Count.AssertEqual(totalMessages, $"Expected {totalMessages} messages, got {dequeued.Count}");
	}

	/// <summary>
	/// Tests that new items are inserted in correct position during active dequeue.
	/// </summary>
	[TestMethod]
	public async Task InsertDuringDequeue_NewItemsInCorrectPosition()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var baseTime = DateTime.UtcNow;

		// Pre-fill queue with messages at t+100, t+200, t+300
		await queue.Enqueue(new TimeMessage { LocalTime = baseTime.AddMilliseconds(100) }, CancellationToken);
		await queue.Enqueue(new TimeMessage { LocalTime = baseTime.AddMilliseconds(200) }, CancellationToken);
		await queue.Enqueue(new TimeMessage { LocalTime = baseTime.AddMilliseconds(300) }, CancellationToken);

		// Dequeue first item (t+100)
		var first = await queue.DequeueAsync(CancellationToken);
		first.LocalTime.AssertEqual(baseTime.AddMilliseconds(100));

		// Now insert item at t+150 (between remaining t+200 and t+300)
		await queue.Enqueue(new TimeMessage { LocalTime = baseTime.AddMilliseconds(150) }, CancellationToken);

		// Also insert at t+50 (earlier than all remaining)
		await queue.Enqueue(new TimeMessage { LocalTime = baseTime.AddMilliseconds(50) }, CancellationToken);

		// Dequeue should return in sorted order: t+50, t+150, t+200, t+300
		var second = await queue.DequeueAsync(CancellationToken);
		second.LocalTime.AssertEqual(baseTime.AddMilliseconds(50), "t+50 should come first");

		var third = await queue.DequeueAsync(CancellationToken);
		third.LocalTime.AssertEqual(baseTime.AddMilliseconds(150), "t+150 should come second");

		var fourth = await queue.DequeueAsync(CancellationToken);
		fourth.LocalTime.AssertEqual(baseTime.AddMilliseconds(200), "t+200 should come third");

		var fifth = await queue.DequeueAsync(CancellationToken);
		fifth.LocalTime.AssertEqual(baseTime.AddMilliseconds(300), "t+300 should come fourth");

		queue.Count.AssertEqual(0, "Queue should be empty");
	}

	#endregion

	#region High Volume Stress Tests

	/// <summary>
	/// Stress test with high message volume through channel.
	/// Uses 10k messages. With concurrent send/process, strict ordering is not guaranteed
	/// because the consumer may dequeue a high-time message before a low-time message is enqueued.
	/// Verifies all messages are processed.
	/// </summary>
	[TestMethod]
	public async Task HighVolume_10kMessages_AllProcessed()
	{
		var queue = new MessageByLocalTimeQueue();
		using var channel = new InMemoryMessageChannel(queue, "StressTest", _ => { });

		var processedCount = 0;

		// Consumer via event handler
		channel.NewOutMessageAsync += (msg, ct) =>
		{
			Interlocked.Increment(ref processedCount);
			return default;
		};

		channel.Open();

		// Producer - send 10k messages with shuffled times
		var baseTime = DateTime.UtcNow;
		const int messageCount = 10_000;
		var times = Enumerable.Range(0, messageCount)
			.Select(i => baseTime.AddMilliseconds(i))
			.OrderBy(_ => Guid.NewGuid()) // Shuffle
			.ToList();

		foreach (var time in times)
		{
			await ((IMessageChannel)channel).SendInMessageAsync(new TimeMessage { LocalTime = time }, CancellationToken);
		}

		// Wait for processing to complete
		var maxWait = DateTime.UtcNow.AddSeconds(30);
		while (processedCount < messageCount && DateTime.UtcNow < maxWait)
		{
			await Task.Delay(100, CancellationToken);
		}

		processedCount.AssertEqual(messageCount, $"Should process all {messageCount} messages");
	}

	/// <summary>
	/// Tests behavior when queue is rapidly filled then drained.
	/// </summary>
	[TestMethod]
	public async Task BurstTraffic_FillThenDrain_MaintainsOrder()
	{
		var queue = new MessageByLocalTimeQueue();
		queue.Open();

		var baseTime = DateTime.UtcNow;
		const int batchSize = 5000;
		const int batchCount = 10;

		for (int batch = 0; batch < batchCount; batch++)
		{
			// Burst fill with random times
			var random = new Random(batch);
			var tasks = Enumerable.Range(0, batchSize).Select(i =>
			{
				var time = baseTime.AddMilliseconds(random.Next(0, batchSize * batchCount));
				return queue.Enqueue(new TimeMessage { LocalTime = time }, CancellationToken).AsTask();
			});

			await Task.WhenAll(tasks);

			// Drain and verify order
			var drained = new List<DateTime>();
			while (queue.Count > 0)
			{
				var msg = await queue.DequeueAsync(CancellationToken);
				drained.Add(msg.LocalTime);
			}

			// Verify sorted
			for (int i = 1; i < drained.Count; i++)
			{
				(drained[i] >= drained[i - 1]).AssertTrue($"Batch {batch}: time ordering violated at {i}");
			}
		}
	}

	#endregion

	#region Emulator Direct Tests (without Channel)

	/// <summary>
	/// Direct emulator test — sequential messages produce monotonic output,
	/// and a backward-time message is rejected with an exception.
	/// </summary>
	[TestMethod]
	public async Task EmulatorDirect_SequentialMessages_OutputOrdered()
	{
		var secProvider = new CollectionSecurityProvider([new Security { Id = "TEST@TEST" }]);
		var pfProvider = new CollectionPortfolioProvider([Portfolio.CreateSimulator()]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		var emulator = new MarketEmulator(secProvider, pfProvider, exchangeProvider, new IncrementalIdGenerator());

		var outputTimes = new List<DateTime>();

		emulator.NewOutMessageAsync += (msg, ct) =>
		{
			outputTimes.Add(msg.LocalTime);
			return default;
		};

		// Reset
		await ((IMessageTransport)emulator).SendInMessageAsync(new ResetMessage(), CancellationToken);

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Send messages in time order
		for (int i = 0; i < 100; i++)
		{
			var time = baseTime.AddSeconds(i);
			var msg = new ExecutionMessage
			{
				SecurityId = securityId,
				LocalTime = time,
				ServerTime = time,
				DataTypeEx = DataType.Ticks,
				TradePrice = 100,
				TradeVolume = 1,
			};

			await ((IMessageTransport)emulator).SendInMessageAsync(msg, CancellationToken);
		}

		// Now send a message with old time — emulator must reject backward-time messages
		var oldTime = baseTime.AddSeconds(50);
		var lateMsg = new ExecutionMessage
		{
			SecurityId = securityId,
			LocalTime = oldTime,
			ServerTime = oldTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100,
			TradeVolume = 1,
		};

		await ThrowsAsync<InvalidOperationException>(async () =>
		{
			await ((IMessageTransport)emulator).SendInMessageAsync(lateMsg, CancellationToken);
		});

		// Output from the 100 sequential messages should have monotonic times
		for (int i = 1; i < outputTimes.Count; i++)
		{
			(outputTimes[i] >= outputTimes[i - 1]).AssertTrue(
				$"Output time went backwards at {i}: {outputTimes[i - 1]:HH:mm:ss} -> {outputTimes[i]:HH:mm:ss}");
		}
	}

	/// <summary>
	/// Tests that sending an order with time in the past is rejected by the emulator.
	/// The emulator must throw InvalidOperationException for backward-time messages.
	/// </summary>
	[TestMethod]
	public async Task EmulatorDirect_OrderWithOldTime_ThrowsException()
	{
		var security = new Security { Id = "TEST@TEST" };
		var portfolio = Portfolio.CreateSimulator();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		var emulator = new MarketEmulator(secProvider, pfProvider, exchangeProvider, new IncrementalIdGenerator());

		emulator.NewOutMessageAsync += (msg, ct) => default;

		// Reset
		await ((IMessageTransport)emulator).SendInMessageAsync(new ResetMessage(), CancellationToken);

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Send tick at T+100
		await ((IMessageTransport)emulator).SendInMessageAsync(new ExecutionMessage
		{
			SecurityId = securityId,
			LocalTime = baseTime.AddSeconds(100),
			ServerTime = baseTime.AddSeconds(100),
			DataTypeEx = DataType.Ticks,
			TradePrice = 100,
			TradeVolume = 1,
		}, CancellationToken);

		// Now send order with time T+50 (in the past!) — should throw
		await ThrowsAsync<InvalidOperationException>(async () =>
		{
			await ((IMessageTransport)emulator).SendInMessageAsync(new OrderRegisterMessage
			{
				SecurityId = securityId,
				PortfolioName = portfolio.Name,
				LocalTime = baseTime.AddSeconds(50),
				TransactionId = 1,
				Side = Sides.Buy,
				Price = 100,
				Volume = 1,
				OrderType = OrderTypes.Limit,
			}, CancellationToken);
		});
	}

	#endregion
}
