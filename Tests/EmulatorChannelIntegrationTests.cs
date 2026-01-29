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
	/// Demonstrates the correct flow: history is pre-loaded, then processed.
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

		// STEP 1: Pre-fill queue with historical ticks (before starting processing)
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

			await queue.Enqueue(tick, CancellationToken);
		}

		// STEP 2: Setup handler that processes history and sends orders BACK TO CHANNEL
		channel.NewOutMessageAsync += async (msg, ct) =>
		{
			// First send to emulator
			await ((IMessageTransport)emulator).SendInMessageAsync(msg, ct);

			// Trading logic: when we see a tick, react by registering an order
			// This simulates strategy reacting to market data
			if (msg is ExecutionMessage execMsg && execMsg.DataTypeEx == DataType.Ticks)
			{
				Interlocked.Increment(ref ticksProcessed);

				// Every 50 ticks, register an order
				if (ticksProcessed % 50 == 0)
				{
					// Order time = current tick time (this is normal behavior)
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

		// STEP 3: Open channel to start processing the pre-filled queue
		channel.Open();

		// Send reset to initialize emulator
		await ((IMessageChannel)channel).SendInMessageAsync(new ResetMessage(), CancellationToken);

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
	/// Tests race condition scenario: order arrives with time in the past relative to current history position.
	/// This happens when strategy processing takes time and new ticks arrive before order is queued.
	/// Channel should sort the order correctly by time.
	/// </summary>
	[TestMethod]
	public async Task RaceCondition_OrderWithPastTime_ChannelSortsCorrectly()
	{
		var queue = new MessageByLocalTimeQueue();
		using var channel = new InMemoryMessageChannel(queue, "TestChannel", ex => Fail($"Channel error: {ex.Message}"));

		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };
		var portfolioName = "TestPortfolio";

		var secProvider = new CollectionSecurityProvider([new Security { Id = "TEST@TEST" }]);
		var pfProvider = new CollectionPortfolioProvider([Portfolio.CreateSimulator()]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		var emulator = new MarketEmulator(secProvider, pfProvider, exchangeProvider, new IncrementalIdGenerator());

		var outputTimes = new List<DateTime>();
		var outputLock = new object();

		emulator.NewOutMessageAsync += (msg, ct) =>
		{
			lock (outputLock)
			{
				outputTimes.Add(msg.LocalTime);
			}
			return default;
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var ticksProcessed = 0;
		var orderSent = false;

		// Pre-fill queue with history: ticks at T+0, T+1, ... T+99 seconds
		for (int i = 0; i < 100; i++)
		{
			var tickTime = baseTime.AddSeconds(i);
			await queue.Enqueue(new ExecutionMessage
			{
				SecurityId = securityId,
				LocalTime = tickTime,
				ServerTime = tickTime,
				DataTypeEx = DataType.Ticks,
				TradePrice = 100 + i * 0.01m,
				TradeVolume = 1,
			}, CancellationToken);
		}

		// Handler simulates race condition:
		// When we see tick at T+50, we send order with time T+50
		// But by the time order is queued, more ticks have been dequeued
		// The order should still be sorted correctly by channel
		channel.NewOutMessageAsync += async (msg, ct) =>
		{
			await ((IMessageTransport)emulator).SendInMessageAsync(msg, ct);

			if (msg is ExecutionMessage execMsg && execMsg.DataTypeEx == DataType.Ticks)
			{
				var count = Interlocked.Increment(ref ticksProcessed);

				// At tick #50, send order with time = T+50
				// This simulates: strategy sees tick at T+50, decides to trade
				// But processing takes time, so by now queue might be at T+60
				if (count == 50 && !orderSent)
				{
					orderSent = true;

					// Simulate processing delay
					await Task.Delay(10, ct);

					var order = new OrderRegisterMessage
					{
						SecurityId = securityId,
						PortfolioName = portfolioName,
						LocalTime = baseTime.AddSeconds(50), // Time when we SAW the tick
						TransactionId = 1,
						Side = Sides.Buy,
						Price = 100,
						Volume = 1,
						OrderType = OrderTypes.Limit,
					};

					// Send to channel - channel will sort by LocalTime
					await ((IMessageChannel)channel).SendInMessageAsync(order, ct);
				}
			}
		};

		channel.Open();
		await ((IMessageChannel)channel).SendInMessageAsync(new ResetMessage(), CancellationToken);

		// Wait for all processing
		var maxWait = DateTime.UtcNow.AddSeconds(10);
		while (ticksProcessed < 100 && DateTime.UtcNow < maxWait)
		{
			await Task.Delay(50, CancellationToken);
		}

		await Task.Delay(500, CancellationToken);

		ticksProcessed.AssertEqual(100, "Should process all ticks");
		orderSent.AssertTrue("Should have sent order");

		// Verify output times are monotonic
		for (int i = 1; i < outputTimes.Count; i++)
		{
			(outputTimes[i] >= outputTimes[i - 1]).AssertTrue(
				$"Time went backwards at {i}: {outputTimes[i - 1]:HH:mm:ss.fff} -> {outputTimes[i]:HH:mm:ss.fff}");
		}
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
	/// Uses 10k messages to be reasonable for test execution time.
	/// </summary>
	[TestMethod]
	public async Task HighVolume_10kMessages_ProcessedInOrder()
	{
		var queue = new MessageByLocalTimeQueue();
		using var channel = new InMemoryMessageChannel(queue, "StressTest", _ => { });

		var processedMessages = new ConcurrentQueue<DateTime>(); // Use ConcurrentQueue to preserve order
		var processedCount = 0;

		// Consumer via event handler
		channel.NewOutMessageAsync += (msg, ct) =>
		{
			processedMessages.Enqueue(msg.LocalTime);
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

		processedMessages.Count.AssertEqual(messageCount, $"Should process all {messageCount} messages");

		// Verify ordering - messages should be processed in sorted time order
		// Allow tiny tolerance for same-millisecond timing jitter
		var list = processedMessages.ToList();
		var violations = 0;
		for (int i = 1; i < list.Count; i++)
		{
			if (list[i] < list[i - 1])
				violations++;
		}

		// Allow up to 0.5% violations due to same-time message ordering
		var violationRate = (double)violations / messageCount;
		violationRate.AssertLess(0.005, $"Found {violations} time ordering violations ({violationRate:P2})");
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
	/// Direct emulator test without channel - baseline for time correction behavior.
	/// Tests emulator's ability to handle messages arriving out of time order.
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

		// Now send a message with old time (simulating the race condition scenario)
		var oldTime = baseTime.AddSeconds(50); // In the middle
		var lateMsg = new ExecutionMessage
		{
			SecurityId = securityId,
			LocalTime = oldTime,
			ServerTime = oldTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100,
			TradeVolume = 1,
		};

		await ((IMessageTransport)emulator).SendInMessageAsync(lateMsg, CancellationToken);

		// With the fix, output should have monotonic times
		for (int i = 1; i < outputTimes.Count; i++)
		{
			(outputTimes[i] >= outputTimes[i - 1]).AssertTrue(
				$"Output time went backwards at {i}: {outputTimes[i - 1]:HH:mm:ss} -> {outputTimes[i]:HH:mm:ss}");
		}
	}

	/// <summary>
	/// Tests order registration with out-of-order LocalTime.
	/// The emulator should correct the output times.
	/// </summary>
	[TestMethod]
	public async Task EmulatorDirect_OrderWithOldTime_OutputTimeCorrected()
	{
		var security = new Security { Id = "TEST@TEST" };
		var portfolio = Portfolio.CreateSimulator();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		var emulator = new MarketEmulator(secProvider, pfProvider, exchangeProvider, new IncrementalIdGenerator());

		var outputMessages = new List<(DateTime time, string type)>();

		emulator.NewOutMessageAsync += (msg, ct) =>
		{
			outputMessages.Add((msg.LocalTime, msg.GetType().Name));
			return default;
		};

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

		// Now send order with time T+50 (in the past!)
		await ((IMessageTransport)emulator).SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = securityId,
			PortfolioName = portfolio.Name,
			LocalTime = baseTime.AddSeconds(50), // OLD TIME
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
		}, CancellationToken);

		// All output messages should have time >= T+100 (the emulator's current time)
		// because the fix corrects old times
		var minExpectedTime = baseTime.AddSeconds(100);

		foreach (var (time, type) in outputMessages)
		{
			// Skip reset-related messages
			if (type == "ResetMessage")
				continue;

			// After the first tick, all times should be >= T+100
			(time >= baseTime).AssertTrue($"Message {type} has time {time} before base time");
		}

		// Verify no time went backwards
		DateTime? lastTime = null;
		foreach (var (time, type) in outputMessages)
		{
			if (lastTime.HasValue)
			{
				(time >= lastTime.Value).AssertTrue(
					$"Time went backwards: {lastTime.Value:HH:mm:ss.fff} -> {time:HH:mm:ss.fff} ({type})");
			}
			lastTime = time;
		}
	}

	#endregion
}
