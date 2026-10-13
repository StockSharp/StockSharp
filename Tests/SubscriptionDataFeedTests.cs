namespace StockSharp.Tests;

using System.Threading.Channels;

class DataFeedEmulator : IDisposable
{
	private readonly CancellationTokenSource _cts = new();
	private readonly Channel<Message> _output = Channel.CreateUnbounded<Message>(new() { SingleReader = true, SingleWriter = true });
	private Task _generatorTask;

	public SecurityId SecurityId { get; }
	public DataType DataType { get; }
	public TimeSpan Interval { get; }
	public bool StampSubscriptionIds { get; set; }

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
					msg.OriginalTransactionId = _subscriptionId;
					if (StampSubscriptionIds)
						msg.SetSubscriptionIds([_subscriptionId]);
					_output.Writer.TryWrite((Message)msg);
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

	// Waits without holding a thread: a test blocked on the feed takes one from the generator it waits for.
	public ValueTask<Message> NextAsync(CancellationToken cancellationToken)
		=> _output.Reader.ReadAsync(cancellationToken);

	public void Dispose()
	{
		_cts.Cancel();
		_generatorTask?.Wait(1000);
		_output.Writer.TryComplete();
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
	// A feed message comes within milliseconds; the margin is for a test host short of threads, where the feed can
	// stall for seconds.
	private static readonly TimeSpan _patience = TimeSpan.FromSeconds(30);

	private Func<Message, ValueTask<Message>> Through(SubscriptionOnlineManager manager)
		=> async message => (await manager.ProcessOutMessageAsync(message, CancellationToken)).forward;

	private static Func<Message, ValueTask<Message>> Through(SubscriptionManager manager)
		=> message => ValueTask.FromResult(manager.ProcessOutMessage(message).forward);

	// Passes feed messages through until `count` of them come out.
	private async Task<List<T>> ForwardedAsync<T>(DataFeedEmulator feed, Func<Message, ValueTask<Message>> process, int count)
		where T : class
	{
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		deadline.CancelAfter(_patience);

		var forwarded = new List<T>();

		try
		{
			while (forwarded.Count < count)
			{
				if (await process(await feed.NextAsync(deadline.Token)) is T message)
					forwarded.Add(message);
			}
		}
		catch (OperationCanceledException) when (!CancellationToken.IsCancellationRequested)
		{
			Fail($"{forwarded.Count} of {count} messages came through in {_patience}.");
		}

		return forwarded;
	}

	// Passes the next `count` feed messages through and returns those that came out.
	private async Task<List<Message>> PassNextAsync(DataFeedEmulator feed, Func<Message, ValueTask<Message>> process, int count)
	{
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		deadline.CancelAfter(_patience);

		var forwarded = new List<Message>();
		var passed = 0;

		try
		{
			for (; passed < count; passed++)
			{
				if (await process(await feed.NextAsync(deadline.Token)) is { } message)
					forwarded.Add(message);
			}
		}
		catch (OperationCanceledException) when (!CancellationToken.IsCancellationRequested)
		{
			Fail($"The feed gave {passed} of {count} messages in {_patience}.");
		}

		return forwarded;
	}

	#region SubscriptionOnlineManager Tests

	[TestMethod]
	public async Task OnlineManager_Subscribe_ReceivesDataWithCorrectId()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
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

		var receivedMessages = await ForwardedAsync<ISubscriptionIdMessage>(feed, Through(manager), 10);
		feed.Stop();

		// Verify
		foreach (var msg in receivedMessages)
		{
			msg.GetSubscriptionIds().Count(id => id == 100).AssertEqual(1, "All messages should have subscription ID 100");
		}
	}

	[TestMethod]
	public async Task OnlineManager_Unsubscribe_StopsReceivingData()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
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
		await ForwardedAsync<ISubscriptionIdMessage>(feed, Through(manager), 5);

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
		var messagesAfterUnsubscribe = await PassNextAsync(feed, Through(manager), 5);

		feed.Stop();

		messagesAfterUnsubscribe.Count.AssertEqual(0, "Messages must not be forwarded after unsubscribe");
	}

	[TestMethod]
	public async Task OnlineManager_TwoSubscriptions_BothReceiveData()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
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

		var receivedMessages = await ForwardedAsync<ISubscriptionIdMessage>(feed, Through(manager), 10);

		feed.Stop();

		// All messages should have BOTH subscription IDs
		foreach (var msg in receivedMessages)
		{
			var ids = msg.GetSubscriptionIds();
			ids.Count(id => id == 100).AssertEqual(1, "Should contain first subscription ID");
			ids.Count(id => id == 101).AssertEqual(1, "Should contain second subscription ID");
		}
	}

	[TestMethod]
	public async Task OnlineManager_TwoSubscriptions_UnsubscribeFirst_SecondStillReceives()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
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
		var messagesBefore = await ForwardedAsync<ISubscriptionIdMessage>(feed, Through(manager), 3);

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
		var messagesAfter = await ForwardedAsync<ISubscriptionIdMessage>(feed, Through(manager), 5);

		feed.Stop();

		foreach (var msg in messagesAfter)
		{
			var ids = msg.GetSubscriptionIds();
			ids.Count(id => id == 100).AssertEqual(0, "Should NOT contain unsubscribed ID 100");
			ids.Count(id => id == 101).AssertEqual(1, "Should contain remaining ID 101");
		}
	}

	[TestMethod]
	public async Task OnlineManager_SubscribeUnsubscribeResubscribe_DataFlowCorrect()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
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
		var phase1 = (await ForwardedAsync<ISubscriptionIdMessage>(feed, Through(manager), 3)).Select(m => m.GetSubscriptionIds()).ToList();

		phase1.All(ids => ids.Count(id => id == 100) == 1).AssertTrue("Phase 1: all should have ID 100");

		// Unsubscribe
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 101,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Phase 2: Unsubscribed - messages should NOT be forwarded (forward = null)
		var phase2Forwarded = await PassNextAsync(feed, Through(manager), 3);

		phase2Forwarded.Count.AssertEqual(0, "Phase 2: no messages should be forwarded after unsubscribe");

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
		var phase3 = (await ForwardedAsync<ISubscriptionIdMessage>(feed, Through(manager), 3)).Select(m => m.GetSubscriptionIds()).ToList();

		feed.Stop();

		phase3.All(ids => ids.Count(id => id == 200) == 1).AssertTrue("Phase 3: all should have ID 200");
	}

	#endregion

	#region SubscriptionManager Tests

	[TestMethod]
	public async Task Manager_LiveData_PassesThroughUntagged()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

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

		var receivedMessages = await ForwardedAsync<ISubscriptionIdMessage>(feed, Through(manager), 10);

		feed.Stop();

		foreach (var msg in receivedMessages)
		{
			msg.GetSubscriptionIds().Length.AssertEqual(0, "SubscriptionManager does not set subscription IDs for live subscriptions");
		}
	}

	[TestMethod]
	public async Task Manager_Unsubscribe_StopsReceivingData()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();
		using var feed = new DataFeedEmulator(secId, DataType.Ticks, TimeSpan.FromMilliseconds(10))
		{
			StampSubscriptionIds = true,
		};

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
		var messagesBeforeUnsubscribe = await ForwardedAsync<ISubscriptionIdMessage>(feed, Through(manager), 5);

		foreach (var msg in messagesBeforeUnsubscribe)
			msg.GetSubscriptionIds().SequenceEqual([100L]).AssertTrue();

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
		var messagesAfterUnsubscribe = await PassNextAsync(feed, Through(manager), 5);

		feed.Stop();

		messagesAfterUnsubscribe.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task Manager_MultipleSecurities_IndependentSubscriptions()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId1 = new SecurityId { SecurityCode = "SEC1", BoardCode = "BOARD" };
		var secId2 = new SecurityId { SecurityCode = "SEC2", BoardCode = "BOARD" };

		using var feed1 = new DataFeedEmulator(secId1, DataType.Ticks, TimeSpan.FromMilliseconds(10))
		{
			StampSubscriptionIds = true,
		};
		using var feed2 = new DataFeedEmulator(secId2, DataType.Ticks, TimeSpan.FromMilliseconds(10))
		{
			StampSubscriptionIds = true,
		};

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
		var messages1 = await ForwardedAsync<ISubscriptionIdMessage>(feed1, Through(manager), 10);
		var messages2 = await ForwardedAsync<ISubscriptionIdMessage>(feed2, Through(manager), 10);

		// Unsubscribe from first
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 102,
			OriginalTransactionId = 100,
			SecurityId = secId1,
			DataType2 = DataType.Ticks,
		});

		var firstAfter = await PassNextAsync(feed1, Through(manager), 5);
		var secondAfter = await ForwardedAsync<ISubscriptionIdMessage>(feed2, Through(manager), 5);

		feed1.Stop();
		feed2.Stop();

		firstAfter.Count.AssertEqual(0);

		foreach (var msg in messages1)
			msg.GetSubscriptionIds().SequenceEqual([100L]).AssertTrue();
		foreach (var msg in messages2)
			msg.GetSubscriptionIds().SequenceEqual([101L]).AssertTrue();
		foreach (var msg in secondAfter)
			msg.GetSubscriptionIds().SequenceEqual([101L]).AssertTrue();
	}

	#endregion

	#region Different Data Types Tests

	[TestMethod]
	public async Task OnlineManager_DifferentDataTypes_IndependentStreams()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
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
		await ForwardedAsync<ExecutionMessage>(ticksFeed, Through(manager), 10);
		await ForwardedAsync<Level1ChangeMessage>(level1Feed, Through(manager), 10);

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
		var level1After = await ForwardedAsync<Level1ChangeMessage>(level1Feed, Through(manager), 5);

		ticksFeed.Stop();
		level1Feed.Stop();

		foreach (var msg in level1After)
		{
			msg.GetSubscriptionIds().Count(id => id == 101).AssertEqual(1, "Level1 should have ID 101");
		}
	}

	#endregion
}
