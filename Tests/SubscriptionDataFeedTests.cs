namespace StockSharp.Tests;

using System.Collections.Concurrent;

/// <summary>
/// Simulates a live data feed that generates market data messages.
/// </summary>
public sealed class DataFeedEmulator : IDisposable
{
	private readonly CancellationTokenSource _cts = new();
	private readonly BlockingCollection<Message> _outputQueue = new();
	private Task _generatorTask;

	public SecurityId SecurityId { get; }
	public DataType DataType { get; }
	public TimeSpan Interval { get; }

	private long _subscriptionId;
	private volatile bool _isGenerating;

	public DataFeedEmulator(SecurityId securityId, DataType dataType, TimeSpan interval)
	{
		SecurityId = securityId;
		DataType = dataType;
		Interval = interval;
	}

	public void SetSubscriptionId(long subscriptionId)
	{
		_subscriptionId = subscriptionId;
	}

	public void Start()
	{
		if (_isGenerating)
			return;

		_isGenerating = true;
		_generatorTask = Task.Run(GenerateDataLoop);

		// Wait for first message to be generated
		Thread.Sleep(50);
	}

	public void Stop()
	{
		_isGenerating = false;
	}

	private async Task GenerateDataLoop()
	{
		var price = 100m;
		var random = new Random(42);

		while (!_cts.Token.IsCancellationRequested)
		{
			if (_isGenerating && _subscriptionId != 0)
			{
				price += (decimal)(random.NextDouble() - 0.5) * 0.1m;

				var msg = CreateMessage(price);
				if (msg != null)
				{
					msg.SetSubscriptionIds([_subscriptionId]);
					_outputQueue.Add((Message)msg);
				}
			}

			try
			{
				await Task.Delay(Interval, _cts.Token);
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
	}

	private ISubscriptionIdMessage CreateMessage(decimal price)
	{
		var now = DateTime.UtcNow;

		if (DataType == DataType.Ticks)
		{
			return new ExecutionMessage
			{
				SecurityId = SecurityId,
				DataTypeEx = DataType.Ticks,
				ServerTime = now,
				LocalTime = now,
				TradePrice = price,
				TradeVolume = 1,
			};
		}

		if (DataType == DataType.Level1)
		{
			var msg = new Level1ChangeMessage
			{
				SecurityId = SecurityId,
				ServerTime = now,
				LocalTime = now,
			};
			msg.TryAdd(Level1Fields.LastTradePrice, price);
			msg.TryAdd(Level1Fields.BestBidPrice, price - 0.01m);
			msg.TryAdd(Level1Fields.BestAskPrice, price + 0.01m);
			return msg;
		}

		if (DataType == DataType.MarketDepth)
		{
			return new QuoteChangeMessage
			{
				SecurityId = SecurityId,
				ServerTime = now,
				LocalTime = now,
				Bids = [new QuoteChange(price - 0.01m, 10)],
				Asks = [new QuoteChange(price + 0.01m, 10)],
			};
		}

		return null;
	}

	public bool TryGetMessage(TimeSpan timeout, out Message message)
	{
		return _outputQueue.TryTake(out message, timeout);
	}

	public async Task<Message> WaitForMessageAsync(TimeSpan timeout, CancellationToken token)
	{
		var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
		cts.CancelAfter(timeout);

		try
		{
			while (!cts.Token.IsCancellationRequested)
			{
				if (_outputQueue.TryTake(out var msg, 10))
					return msg;
				await Task.Delay(5, cts.Token);
			}
		}
		catch (OperationCanceledException) { }

		return null;
	}

	public async Task<List<Message>> CollectMessagesAsync(int count, TimeSpan perMessageTimeout, CancellationToken token)
	{
		var result = new List<Message>();
		for (int i = 0; i < count; i++)
		{
			var msg = await WaitForMessageAsync(perMessageTimeout, token);
			if (msg != null)
				result.Add(msg);
		}
		return result;
	}

	public void Dispose()
	{
		_cts.Cancel();
		_generatorTask?.Wait(1000);
		_outputQueue.Dispose();
		_cts.Dispose();
	}
}

/// <summary>
/// Integration tests for SubscriptionManager with simulated data feed.
/// Tests subscribe/unsubscribe behavior with live data stream.
/// </summary>
[TestClass]
public class SubscriptionDataFeedTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	#region SubscriptionOnlineManager Tests

	[TestMethod]
	public async Task OnlineManager_Subscribe_ReceivesDataWithCorrectId()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		using var feed = new DataFeedEmulator(secId, DataType.Ticks, TimeSpan.FromMilliseconds(10));

		// Subscribe
		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		await manager.ProcessInMessageAsync(subscribe, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, token);

		// Start feed
		feed.SetSubscriptionId(100);
		feed.Start();

		// Collect messages
		var receivedMessages = new List<ISubscriptionIdMessage>();
		var collectTask = Task.Run(async () =>
		{
			for (int i = 0; i < 10; i++)
			{
				if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
				{
					var (forward, _) = await manager.ProcessOutMessageAsync(msg, token);
					if (forward is ISubscriptionIdMessage subMsg)
						receivedMessages.Add(subMsg);
				}
			}
		}, token);

		await collectTask;
		feed.Stop();

		// Verify
		receivedMessages.Count.AssertGreater(0, "Should receive messages");
		foreach (var msg in receivedMessages)
		{
			msg.GetSubscriptionIds().Contains(100).AssertTrue("All messages should have subscription ID 100");
		}
	}

	[TestMethod]
	public async Task OnlineManager_Unsubscribe_StopsReceivingData()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		using var feed = new DataFeedEmulator(secId, DataType.Ticks, TimeSpan.FromMilliseconds(10));

		// Subscribe
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, token);

		feed.SetSubscriptionId(100);
		feed.Start();

		// Receive some messages while subscribed
		var messagesBeforeUnsubscribe = new List<ISubscriptionIdMessage>();
		for (int i = 0; i < 5; i++)
		{
			if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = await manager.ProcessOutMessageAsync(msg, token);
				if (forward is ISubscriptionIdMessage subMsg)
					messagesBeforeUnsubscribe.Add(subMsg);
			}
		}

		messagesBeforeUnsubscribe.Count.AssertGreater(0, "Should receive messages before unsubscribe");

		// Unsubscribe
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 101,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Continue receiving - messages should not have subscription ID 100
		var messagesAfterUnsubscribe = new List<ISubscriptionIdMessage>();
		for (int i = 0; i < 5; i++)
		{
			if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = await manager.ProcessOutMessageAsync(msg, token);
				if (forward is ISubscriptionIdMessage subMsg)
					messagesAfterUnsubscribe.Add(subMsg);
			}
		}

		feed.Stop();

		// After unsubscribe, messages should NOT contain subscription ID 100
		foreach (var msg in messagesAfterUnsubscribe)
		{
			msg.GetSubscriptionIds().Contains(100).AssertFalse("Messages after unsubscribe should NOT have ID 100");
		}
	}

	[TestMethod]
	public async Task OnlineManager_TwoSubscriptions_BothReceiveData()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		using var feed = new DataFeedEmulator(secId, DataType.Ticks, TimeSpan.FromMilliseconds(10));

		// First subscription
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, token);

		// Second subscription (joins first)
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		feed.SetSubscriptionId(100);
		feed.Start();

		// Collect messages
		var receivedMessages = new List<ISubscriptionIdMessage>();
		for (int i = 0; i < 10; i++)
		{
			if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = await manager.ProcessOutMessageAsync(msg, token);
				if (forward is ISubscriptionIdMessage subMsg)
					receivedMessages.Add(subMsg);
			}
		}

		feed.Stop();

		// All messages should have BOTH subscription IDs
		receivedMessages.Count.AssertGreater(0, "Should receive messages");
		foreach (var msg in receivedMessages)
		{
			var ids = msg.GetSubscriptionIds();
			ids.Contains(100).AssertTrue("Should contain first subscription ID");
			ids.Contains(101).AssertTrue("Should contain second subscription ID");
		}
	}

	[TestMethod]
	public async Task OnlineManager_TwoSubscriptions_UnsubscribeFirst_SecondStillReceives()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		using var feed = new DataFeedEmulator(secId, DataType.Ticks, TimeSpan.FromMilliseconds(10));

		// Two subscriptions
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, token);

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		feed.SetSubscriptionId(100);
		feed.Start();

		// Verify both IDs present
		var messagesBefore = new List<ISubscriptionIdMessage>();
		for (int i = 0; i < 3; i++)
		{
			if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = await manager.ProcessOutMessageAsync(msg, token);
				if (forward is ISubscriptionIdMessage subMsg)
					messagesBefore.Add(subMsg);
			}
		}

		messagesBefore.Count.AssertGreater(0);
		messagesBefore[0].GetSubscriptionIds().Length.AssertEqual(2, "Should have both IDs before unsubscribe");

		// Unsubscribe first
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 102,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Now only second subscription should receive
		var messagesAfter = new List<ISubscriptionIdMessage>();
		for (int i = 0; i < 5; i++)
		{
			if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = await manager.ProcessOutMessageAsync(msg, token);
				if (forward is ISubscriptionIdMessage subMsg)
					messagesAfter.Add(subMsg);
			}
		}

		feed.Stop();

		messagesAfter.Count.AssertGreater(0);
		foreach (var msg in messagesAfter)
		{
			var ids = msg.GetSubscriptionIds();
			ids.Contains(100).AssertFalse("Should NOT contain unsubscribed ID 100");
			ids.Contains(101).AssertTrue("Should contain remaining ID 101");
		}
	}

	[TestMethod]
	public async Task OnlineManager_SubscribeUnsubscribeResubscribe_DataFlowCorrect()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		using var feed = new DataFeedEmulator(secId, DataType.Ticks, TimeSpan.FromMilliseconds(10));

		// Subscribe
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, token);

		feed.SetSubscriptionId(100);
		feed.Start();

		// Phase 1: Subscribed - should receive with ID 100
		var phase1 = new List<long[]>();
		for (int i = 0; i < 3; i++)
		{
			if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = await manager.ProcessOutMessageAsync(msg, token);
				if (forward is ISubscriptionIdMessage subMsg)
					phase1.Add(subMsg.GetSubscriptionIds());
			}
		}

		phase1.Count.AssertGreater(0);
		phase1.All(ids => ids.Contains(100)).AssertTrue("Phase 1: all should have ID 100");

		// Unsubscribe
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 101,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Phase 2: Unsubscribed - should NOT have ID 100
		var phase2 = new List<long[]>();
		for (int i = 0; i < 3; i++)
		{
			if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = await manager.ProcessOutMessageAsync(msg, token);
				if (forward is ISubscriptionIdMessage subMsg)
					phase2.Add(subMsg.GetSubscriptionIds());
			}
		}

		phase2.Count.AssertGreater(0);
		phase2.All(ids => !ids.Contains(100)).AssertTrue("Phase 2: none should have ID 100");

		// Resubscribe with new ID
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 200,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 200 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 200 }, token);

		// Update feed to use new subscription ID
		feed.SetSubscriptionId(200);

		// Phase 3: Resubscribed - should receive with ID 200
		var phase3 = new List<long[]>();
		for (int i = 0; i < 3; i++)
		{
			if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = await manager.ProcessOutMessageAsync(msg, token);
				if (forward is ISubscriptionIdMessage subMsg)
					phase3.Add(subMsg.GetSubscriptionIds());
			}
		}

		feed.Stop();

		phase3.Count.AssertGreater(0);
		phase3.All(ids => ids.Contains(200)).AssertTrue("Phase 3: all should have ID 200");
	}

	#endregion

	#region SubscriptionManager Tests

	[TestMethod]
	public void Manager_Subscribe_ReceivesDataWithCorrectId()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var secId = Helper.CreateSecurityId();
		using var feed = new DataFeedEmulator(secId, DataType.Ticks, TimeSpan.FromMilliseconds(10));

		// Subscribe
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		feed.SetSubscriptionId(100);
		feed.Start();

		// Collect messages
		var receivedMessages = new List<ISubscriptionIdMessage>();
		for (int i = 0; i < 10; i++)
		{
			if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = manager.ProcessOutMessage(msg);
				if (forward is ISubscriptionIdMessage subMsg)
					receivedMessages.Add(subMsg);
			}
		}

		feed.Stop();

		receivedMessages.Count.AssertGreater(0, "Should receive messages");
		foreach (var msg in receivedMessages)
		{
			msg.GetSubscriptionIds().Contains(100).AssertTrue("All messages should have subscription ID 100");
		}
	}

	[TestMethod]
	public void Manager_Unsubscribe_StopsReceivingData()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var secId = Helper.CreateSecurityId();
		using var feed = new DataFeedEmulator(secId, DataType.Ticks, TimeSpan.FromMilliseconds(10));

		// Subscribe
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		feed.SetSubscriptionId(100);
		feed.Start();

		// Receive some messages while subscribed
		var messagesBeforeUnsubscribe = new List<ISubscriptionIdMessage>();
		for (int i = 0; i < 5; i++)
		{
			if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = manager.ProcessOutMessage(msg);
				if (forward is ISubscriptionIdMessage subMsg)
					messagesBeforeUnsubscribe.Add(subMsg);
			}
		}

		messagesBeforeUnsubscribe.Count.AssertGreater(0, "Should receive messages before unsubscribe");

		// Unsubscribe
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 101,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});

		// Messages after unsubscribe should not have ID 100
		var messagesAfterUnsubscribe = new List<ISubscriptionIdMessage>();
		for (int i = 0; i < 5; i++)
		{
			if (feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = manager.ProcessOutMessage(msg);
				if (forward is ISubscriptionIdMessage subMsg)
					messagesAfterUnsubscribe.Add(subMsg);
			}
		}

		feed.Stop();

		foreach (var msg in messagesAfterUnsubscribe)
		{
			msg.GetSubscriptionIds().Contains(100).AssertFalse("Messages after unsubscribe should NOT have ID 100");
		}
	}

	[TestMethod]
	public void Manager_MultipleSecurities_IndependentSubscriptions()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var secId1 = new SecurityId { SecurityCode = "SEC1", BoardCode = "BOARD" };
		var secId2 = new SecurityId { SecurityCode = "SEC2", BoardCode = "BOARD" };

		using var feed1 = new DataFeedEmulator(secId1, DataType.Ticks, TimeSpan.FromMilliseconds(10));
		using var feed2 = new DataFeedEmulator(secId2, DataType.Ticks, TimeSpan.FromMilliseconds(10));

		// Subscribe to both
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId1,
			DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId2,
			DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 101 });

		feed1.SetSubscriptionId(100);
		feed2.SetSubscriptionId(101);
		feed1.Start();
		feed2.Start();

		// Collect messages from both feeds
		var messages1 = new List<ISubscriptionIdMessage>();
		var messages2 = new List<ISubscriptionIdMessage>();

		for (int i = 0; i < 10; i++)
		{
			if (feed1.TryGetMessage(TimeSpan.FromMilliseconds(50), out var msg1))
			{
				var (forward, _) = manager.ProcessOutMessage(msg1);
				if (forward is ISubscriptionIdMessage subMsg)
					messages1.Add(subMsg);
			}

			if (feed2.TryGetMessage(TimeSpan.FromMilliseconds(50), out var msg2))
			{
				var (forward, _) = manager.ProcessOutMessage(msg2);
				if (forward is ISubscriptionIdMessage subMsg)
					messages2.Add(subMsg);
			}
		}

		// Unsubscribe from first
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 102,
			OriginalTransactionId = 100,
			SecurityId = secId1,
			DataType2 = DataType.Ticks,
		});

		// Second should still work
		var messagesAfter = new List<ISubscriptionIdMessage>();
		for (int i = 0; i < 5; i++)
		{
			if (feed2.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = manager.ProcessOutMessage(msg);
				if (forward is ISubscriptionIdMessage subMsg)
					messagesAfter.Add(subMsg);
			}
		}

		feed1.Stop();
		feed2.Stop();

		messages1.Count.AssertGreater(0);
		messages2.Count.AssertGreater(0);
		messagesAfter.Count.AssertGreater(0);

		foreach (var msg in messagesAfter)
		{
			msg.GetSubscriptionIds().Contains(101).AssertTrue("Second subscription should still receive");
		}
	}

	#endregion

	#region Different Data Types Tests

	[TestMethod]
	public async Task OnlineManager_DifferentDataTypes_IndependentStreams()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		using var ticksFeed = new DataFeedEmulator(secId, DataType.Ticks, TimeSpan.FromMilliseconds(10));
		using var level1Feed = new DataFeedEmulator(secId, DataType.Level1, TimeSpan.FromMilliseconds(10));

		// Subscribe to Ticks
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, token);

		// Subscribe to Level1
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Level1,
		}, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 101 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 101 }, token);

		ticksFeed.SetSubscriptionId(100);
		level1Feed.SetSubscriptionId(101);
		ticksFeed.Start();
		level1Feed.Start();

		// Collect messages
		var ticksMessages = new List<ExecutionMessage>();
		var level1Messages = new List<Level1ChangeMessage>();

		for (int i = 0; i < 10; i++)
		{
			if (ticksFeed.TryGetMessage(TimeSpan.FromMilliseconds(50), out var tickMsg))
			{
				var (forward, _) = await manager.ProcessOutMessageAsync(tickMsg, token);
				if (forward is ExecutionMessage exec)
					ticksMessages.Add(exec);
			}

			if (level1Feed.TryGetMessage(TimeSpan.FromMilliseconds(50), out var l1Msg))
			{
				var (forward, _) = await manager.ProcessOutMessageAsync(l1Msg, token);
				if (forward is Level1ChangeMessage l1)
					level1Messages.Add(l1);
			}
		}

		// Unsubscribe from Ticks only
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 102,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Level1 should still work
		var level1After = new List<Level1ChangeMessage>();
		for (int i = 0; i < 5; i++)
		{
			if (level1Feed.TryGetMessage(TimeSpan.FromMilliseconds(200), out var msg))
			{
				var (forward, _) = await manager.ProcessOutMessageAsync(msg, token);
				if (forward is Level1ChangeMessage l1)
					level1After.Add(l1);
			}
		}

		ticksFeed.Stop();
		level1Feed.Stop();

		ticksMessages.Count.AssertGreater(0, "Should receive ticks");
		level1Messages.Count.AssertGreater(0, "Should receive level1");
		level1After.Count.AssertGreater(0, "Level1 should still work after ticks unsubscribed");

		foreach (var msg in level1After)
		{
			msg.GetSubscriptionIds().Contains(101).AssertTrue("Level1 should have ID 101");
		}
	}

	#endregion
}
