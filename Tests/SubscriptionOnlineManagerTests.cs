namespace StockSharp.Tests;

[TestClass]
public class SubscriptionOnlineManagerTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	[TestMethod]
	public async Task Subscribe_SecondSubscription_JoinsAndReturnsResponseAndOnline()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		var first = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(first, token);
		toInner.Length.AssertEqual(1);
		toInner[0].AssertSame(first);
		toOut.Length.AssertEqual(0);

		var second = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var secondResult = await manager.ProcessInMessageAsync(second, token);
		secondResult.toInner.Length.AssertEqual(0);
		secondResult.toOut.Length.AssertEqual(2);

		secondResult.toOut.OfType<SubscriptionResponseMessage>().Single().OriginalTransactionId.AssertEqual(2);
		secondResult.toOut.OfType<SubscriptionOnlineMessage>().Single().OriginalTransactionId.AssertEqual(2);
	}

	[TestMethod]
	public async Task SubscriptionError_ShouldNotifyJoinedSubscribers()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		var error = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("boom"),
		};

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(error, token);

		extraOut.OfType<SubscriptionResponseMessage>().Count(msg => msg.OriginalTransactionId == 2 && msg.Error != null).AssertEqual(1);
	}

	#region Subscription Joining — No Duplicate Inner Requests

	[TestMethod]
	public async Task SecondSubscribe_WhenFirstAlreadyOnline_DoesNotGoToInner()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		// First subscription — fully online
		var first = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		var (toInner1, _) = await manager.ProcessInMessageAsync(first, token);
		toInner1.Length.AssertEqual(1, "First subscribe should go to inner");

		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 1 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 1 }, token);

		// Second subscription — same DataType + SecurityId
		var second = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		var (toInner2, toOut2) = await manager.ProcessInMessageAsync(second, token);

		toInner2.Length.AssertEqual(0, "Second subscribe must NOT go to inner when first is already online");
		toOut2.OfType<SubscriptionResponseMessage>().Single().OriginalTransactionId.AssertEqual(2);
		toOut2.OfType<SubscriptionOnlineMessage>().Single().OriginalTransactionId.AssertEqual(2);
	}

	[TestMethod]
	public async Task SecondSubscribe_WhenFirstActive_DoesNotGoToInner()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		// First subscription — active (response received, but no online yet)
		var first = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		await manager.ProcessInMessageAsync(first, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 1 }, token);
		// NOTE: no SubscriptionOnlineMessage yet — subscription is "active" but not "online"

		// Second subscription — same DataType + SecurityId
		var second = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		var (toInner2, toOut2) = await manager.ProcessInMessageAsync(second, token);

		toInner2.Length.AssertEqual(0, "Second subscribe must NOT go to inner when first is active (waiting for online)");
	}

	[TestMethod]
	public async Task SecondSubscribe_WhenFirstPending_DoesNotGoToInner()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		// First subscription — pending (no response at all)
		var first = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		await manager.ProcessInMessageAsync(first, token);
		// NOTE: no response, no online — subscription is "pending"

		// Second subscription — same DataType + SecurityId
		var second = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		var (toInner2, toOut2) = await manager.ProcessInMessageAsync(second, token);

		toInner2.Length.AssertEqual(0, "Second subscribe must NOT go to inner when first is pending");
	}

	#endregion

	#region Subscription Joining — Pending IDs in Data Messages

	[TestMethod]
	public async Task DataMessage_WhenFirstActive_SecondJoined_ContainsBothIds()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		// First subscription — active (response received, no online yet)
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, token);

		// Second subscription joins
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Data arrives from inner adapter (before online!) with first subscription ID
		var dataMessage = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage.OriginalTransactionId = 100;

		var (forward, _) = await manager.ProcessOutMessageAsync(dataMessage, token);
		forward.AssertNotNull("Data should be forwarded");
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Count(id => id == 100).AssertEqual(1, "Should contain first (active) subscription ID");
		ids.Count(id => id == 101).AssertEqual(1, "Should contain second (joined/pending) subscription ID");
	}

	[TestMethod]
	public async Task DataMessage_WhenFirstPending_SecondJoined_ContainsBothIds()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		// First subscription — pending (no response at all)
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Second subscription joins
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Data arrives from inner adapter (before any response!) with first subscription ID
		var dataMessage = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage.OriginalTransactionId = 100;

		var (forward, _) = await manager.ProcessOutMessageAsync(dataMessage, token);
		forward.AssertNotNull("Data should be forwarded");
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Count(id => id == 100).AssertEqual(1, "Should contain first (pending) subscription ID");
		ids.Count(id => id == 101).AssertEqual(1, "Should contain second (joined/pending) subscription ID");
	}

	[TestMethod]
	public async Task OnlineMessage_WhenSecondJoined_SecondGetsOnlineToo()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		// First subscription — pending
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Second subscription joins while first is pending
		var (_, toOut2) = await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Response for first
		var (_, extraOnResp) = await manager.ProcessOutMessageAsync(
			new SubscriptionResponseMessage { OriginalTransactionId = 100 }, token);

		// Online for first
		var (_, extraOnOnline) = await manager.ProcessOutMessageAsync(
			new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, token);

		// Second subscription should have received response + online
		// Either immediately on join (toOut2) or when first goes online (extraOnResp/extraOnOnline)
		var allExtra = toOut2.Concat(extraOnResp).Concat(extraOnOnline).ToArray();

		allExtra.OfType<SubscriptionResponseMessage>()
			.Count(m => m.OriginalTransactionId == 101)
			.AssertEqual(1, "Second (joined) subscription should get SubscriptionResponseMessage");

		allExtra.OfType<SubscriptionOnlineMessage>()
			.Count(m => m.OriginalTransactionId == 101)
			.AssertEqual(1, "Second (joined) subscription should get SubscriptionOnlineMessage");
	}

	[TestMethod]
	public async Task ErrorMessage_WhenSecondJoined_BothGetError()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		// First subscription — pending
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Second subscription joins while first is pending
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Error comes for first
		var (forward, extraOut) = await manager.ProcessOutMessageAsync(
			new SubscriptionResponseMessage
			{
				OriginalTransactionId = 100,
				Error = new InvalidOperationException("test error"),
			}, token);

		// Both subscriptions should get error notification
		var allMessages = new List<Message>();
		if (forward != null) allMessages.Add(forward);
		allMessages.AddRange(extraOut);

		allMessages.OfType<SubscriptionResponseMessage>()
			.Count(m => m.OriginalTransactionId == 100 && m.Error != null)
			.AssertEqual(1, "First subscription should get error");

		allMessages.OfType<SubscriptionResponseMessage>()
			.Count(m => m.OriginalTransactionId == 101 && m.Error != null)
			.AssertEqual(1, "Second (joined) subscription should also get error");
	}

	#endregion

	#region Subscribe/Unsubscribe Multiple Cycles Tests

	[TestMethod]
	public async Task SubscribeUnsubscribeResubscribe_WorksCorrectly()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribe1 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var (toInner1, toOut1) = await manager.ProcessInMessageAsync(subscribe1, token);
		toInner1.Length.AssertEqual(1);
		toOut1.Length.AssertEqual(0);

		// Confirm subscription (response + online)
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, token);

		// Unsubscribe
		var unsubscribe = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 101,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var (toInner2, toOut2) = await manager.ProcessInMessageAsync(unsubscribe, token);
		toInner2.Length.AssertEqual(1); // Unsubscribe sent to inner

		// Resubscribe with new transaction ID
		var subscribe2 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 102,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var (toInner3, toOut3) = await manager.ProcessInMessageAsync(subscribe2, token);
		toInner3.Length.AssertEqual(1); // New subscription sent to inner
		toOut3.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task MultipleSubscriptions_AllActiveNoneActiveSomeActive()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId1 = new SecurityId { SecurityCode = "SEC1", BoardCode = "BOARD" };
		var secId2 = new SecurityId { SecurityCode = "SEC2", BoardCode = "BOARD" };
		var secId3 = new SecurityId { SecurityCode = "SEC3", BoardCode = "BOARD" };

		// Subscribe to all three
		var secIds = new[] { secId1, secId2, secId3 };
		for (int i = 0; i < 3; i++)
		{
			var subscribe = new MarketDataMessage
			{
				IsSubscribe = true,
				TransactionId = 100 + i,
				SecurityId = secIds[i],
				DataType2 = DataType.Ticks,
			};
			await manager.ProcessInMessageAsync(subscribe, token);
			await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 + i }, token);
			await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 + i }, token);
		}

		// Unsubscribe from all
		for (int i = 0; i < 3; i++)
		{
			var unsubscribe = new MarketDataMessage
			{
				IsSubscribe = false,
				TransactionId = 200 + i,
				OriginalTransactionId = 100 + i,
				SecurityId = secIds[i],
				DataType2 = DataType.Ticks,
			};
			await manager.ProcessInMessageAsync(unsubscribe, token);
		}

		// Resubscribe only to the second one
		var resubscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 300,
			SecurityId = secId2,
			DataType2 = DataType.Ticks,
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(resubscribe, token);
		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task RapidSubscribeUnsubscribeCycles_HandlesCorrectly()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var transId = 100L;

		// 5 rapid cycles
		for (int cycle = 0; cycle < 5; cycle++)
		{
			// Subscribe
			var subscribe = new MarketDataMessage
			{
				IsSubscribe = true,
				TransactionId = transId++,
				SecurityId = secId,
				DataType2 = DataType.Ticks,
			};
			var (toInnerSub, toOutSub) = await manager.ProcessInMessageAsync(subscribe, token);
			toInnerSub.Length.AssertEqual(1);

			var subTransId = subscribe.TransactionId;
			await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = subTransId }, token);
			await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = subTransId }, token);

			// Unsubscribe
			var unsubscribe = new MarketDataMessage
			{
				IsSubscribe = false,
				TransactionId = transId++,
				OriginalTransactionId = subTransId,
				SecurityId = secId,
				DataType2 = DataType.Ticks,
			};
			await manager.ProcessInMessageAsync(unsubscribe, token);
		}

		// Final subscription should work
		var finalSubscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = transId++,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(finalSubscribe, token);
		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task UnsubscribeNonExistent_ReturnsError()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var unsubscribe = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 100,
			OriginalTransactionId = 999, // Non-existent
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(unsubscribe, token);

		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(1);
		toOut[0].Type.AssertEqual(MessageTypes.SubscriptionResponse);
		((SubscriptionResponseMessage)toOut[0]).Error.AssertNotNull();
	}

	#endregion

	#region Message Without Subscription Tests

	[TestMethod]
	public async Task OutMessage_WithNoSubscription_NotForwarded()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		// Create a data message with unknown subscription ID (no subscription registered)
		var dataMessage = new Level1ChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = logReceiver.CurrentTime,
		};
		dataMessage.TryAdd(Level1Fields.LastTradePrice, 100m);
		dataMessage.OriginalTransactionId = 999; // Unknown subscription ID

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(dataMessage, token);

		// Message should NOT be forwarded if no subscription exists
		forward.AssertNull();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task OutMessage_AfterUnsubscribe_ShouldNotReceiveData()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

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

		// Verify data is routed correctly while subscribed
		var dataMessage1 = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage1.OriginalTransactionId = 100;

		var (forward1, _) = await manager.ProcessOutMessageAsync(dataMessage1, token);
		forward1.AssertNotNull();
		((ISubscriptionIdMessage)forward1).GetSubscriptionIds().Count(id => id == 100).AssertEqual(1);

		// Unsubscribe
		var unsubscribe = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 101,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		await manager.ProcessInMessageAsync(unsubscribe, token);

		// After unsubscribe, data message should NOT be forwarded
		var dataMessage2 = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 101m,
			TradeVolume = 10m,
		};
		// Don't set subscription IDs - let manager handle it based on (dataType, secId) lookup

		var (forward2, _) = await manager.ProcessOutMessageAsync(dataMessage2, token);
		// Message should NOT be forwarded after unsubscribe
		forward2.AssertNull();
	}

	[TestMethod]
	public async Task OutMessage_WithOnlineSubscription_GetsCorrectIds()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

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

		// Data message should get the correct subscription ID
		var dataMessage = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		// No subscription IDs set - manager will assign based on lookup

		var (forward, _) = await manager.ProcessOutMessageAsync(dataMessage, token);
		forward.AssertNotNull();
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Count(id => id == 100).AssertEqual(1);
	}

	[TestMethod]
	public async Task TwoSubscriptions_SameSecuritySameDataType_DataHasBothIds()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		// First subscription
		var subscribe1 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		await manager.ProcessInMessageAsync(subscribe1, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, token);

		// Second subscription (same security, same DataType)
		var subscribe2 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		await manager.ProcessInMessageAsync(subscribe2, token);
		// Second subscription joins first, gets immediate response+online (no inner call)

		// Send data message - should have BOTH subscription IDs
		var dataMessage = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage.OriginalTransactionId = 100; // Original from source

		var (forward, _) = await manager.ProcessOutMessageAsync(dataMessage, token);
		forward.AssertNotNull();
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Count(id => id == 100).AssertEqual(1, "Should contain first subscription ID");
		ids.Count(id => id == 101).AssertEqual(1, "Should contain second (joined) subscription ID");
	}

	[TestMethod]
	public async Task TwoSubscriptions_UnsubscribeFirst_DataHasOnlySecondId()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

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

		// Verify both IDs present
		var dataMessage1 = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage1.OriginalTransactionId = 100;

		var (forward1, _) = await manager.ProcessOutMessageAsync(dataMessage1, token);
		var ids1 = ((ISubscriptionIdMessage)forward1).GetSubscriptionIds();
		ids1.Length.AssertEqual(2, "Should have both IDs before unsubscribe");

		// Unsubscribe FIRST subscription
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 102,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		// Now data should only have second subscription ID
		var dataMessage2 = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 101m,
			TradeVolume = 10m,
		};
		dataMessage2.OriginalTransactionId = 100; // Still from source with old ID

		var (forward2, _) = await manager.ProcessOutMessageAsync(dataMessage2, token);
		forward2.AssertNotNull();
		var ids2 = ((ISubscriptionIdMessage)forward2).GetSubscriptionIds();
		ids2.Count(id => id == 100).AssertEqual(0, "Should NOT contain unsubscribed ID");
		ids2.Count(id => id == 101).AssertEqual(1, "Should contain remaining subscription ID");
	}

	#endregion

	#region Multiple Data Types Tests

	[TestMethod]
	public async Task MultipleDataTypes_SameSecurityId_IndependentSubscriptions()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		// Subscribe to Level1
		var subscribeLevel1 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Level1,
		};
		await manager.ProcessInMessageAsync(subscribeLevel1, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, token);

		// Subscribe to Ticks
		var subscribeTicks = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		await manager.ProcessInMessageAsync(subscribeTicks, token);
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 101 }, token);
		await manager.ProcessOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 101 }, token);

		// Unsubscribe from Level1
		var unsubscribeLevel1 = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 102,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Level1,
		};
		await manager.ProcessInMessageAsync(unsubscribeLevel1, token);

		// Ticks data should still be routed correctly
		var ticksMessage = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};

		var (forward, _) = await manager.ProcessOutMessageAsync(ticksMessage, token);
		forward.AssertNotNull();
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Count(id => id == 101).AssertEqual(1);
	}

	#endregion

	#region Transaction Messages Tests

	/// <summary>
	/// Tests that transaction messages pass through even without explicit subscription.
	/// This is important for emulation mode where order responses come from nested adapters.
	/// </summary>
	[TestMethod]
	public async Task TransactionMessage_PassesThroughWithoutSubscription()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		// Create order execution message without any subscription
		var execMsg = new ExecutionMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Transactions,
			OrderId = 123,
			TransactionId = 456,
			OrderState = OrderStates.Active,
		};

		// Should pass through without throwing or returning null
		var (forward, extraOut) = await manager.ProcessOutMessageAsync(execMsg, token);

		// Message should be forwarded (not filtered out)
		forward.AssertNotNull("Transaction message should pass through without subscription");
	}

	/// <summary>
	/// Tests that transaction messages with OriginalTransactionId work correctly.
	/// </summary>
	[TestMethod]
	public async Task TransactionMessage_WithOriginalTransactionId_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		// Create order execution message with OriginalTransactionId (order response)
		var execMsg = new ExecutionMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 789,  // This is the order's transaction id
			OrderId = 123,
			OrderState = OrderStates.Done,
		};

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(execMsg, token);

		forward.AssertNotNull("Transaction message with OriginalTransactionId should pass through");
	}

	/// <summary>
	/// Tests that transaction messages get subscription IDs when OrderStatus subscription exists.
	/// This is critical for OrderReceived events to fire in Connector.
	/// </summary>
	[TestMethod]
	public async Task TransactionMessage_WithOrderStatusSubscription_GetsSubscriptionIds()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, new SubscriptionOnlineManagerState());
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		// Subscribe to OrderStatus (like Connector.OrderLookup)
		var orderStatus = new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(orderStatus, token);
		toInner.Length.AssertEqual(1, "OrderStatus should be sent to inner adapter");

		// Simulate SubscriptionOnline response
		var onlineMsg = new SubscriptionOnlineMessage { OriginalTransactionId = 100 };
		await manager.ProcessOutMessageAsync(onlineMsg, token);

		// Now send ExecutionMessage with order (like from MarketEmulator)
		// This simulates order response with DIFFERENT security ID than subscription
		var execMsg = new ExecutionMessage
		{
			SecurityId = secId,  // Specific security ID
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 200,  // Order's transaction id (not subscription id!)
			TransactionId = 200,
			OrderId = 1,
			OrderState = OrderStates.Active,
			HasOrderInfo = true,
		};

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(execMsg, token);

		forward.AssertNotNull("Transaction message should pass through");

		// Critical: subscription IDs should be set so OrderReceived fires
		// The manager should find OrderStatus subscription by fallback to default secId
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		IsTrue(ids.Length > 0, $"Transaction should have subscription IDs, got: [{string.Join(", ", ids)}]");
		IsTrue(ids.Contains(100), $"ID 100 should be in: [{string.Join(", ", ids)}]");
	}

	/// <summary>
	/// Debug test to verify OrderStatus creates subscription with correct key.
	/// </summary>
	[TestMethod]
	public async Task OrderStatus_CreatesSubscriptionWithDefaultSecurityId()
	{
		var logReceiver = new TestReceiver();
		var state = new SubscriptionOnlineManagerState();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true, state);
		var token = CancellationToken;

		// Subscribe to OrderStatus
		var orderStatus = new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
		};

		await manager.ProcessInMessageAsync(orderStatus, token);

		// Verify subscription created with correct key
		var found = state.TryGetSubscriptionByKey((DataType.Transactions, default(SecurityId)), out var info);
		IsTrue(found, "OrderStatus subscription should be found by (Transactions, default) key");
		IsNotNull(info, "Subscription info should not be null");
		AreEqual(100, info.Subscription.TransactionId, "Subscription TransactionId should match");
	}

	#endregion
}
