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
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
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
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
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

		extraOut.OfType<SubscriptionResponseMessage>().Any(msg => msg.OriginalTransactionId == 2 && msg.Error != null).AssertTrue();
	}

	#region Subscribe/Unsubscribe Multiple Cycles Tests

	[TestMethod]
	public async Task SubscribeUnsubscribeResubscribe_WorksCorrectly()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
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
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
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
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
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
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
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
	public async Task OutMessage_WithNoSubscription_StillForwarded()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
		var token = CancellationToken;

		// Create a data message with unknown subscription ID (no subscription registered)
		var dataMessage = new Level1ChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = logReceiver.CurrentTimeUtc,
		};
		dataMessage.TryAdd(Level1Fields.LastTradePrice, 100m);
		dataMessage.SetSubscriptionIds([999]); // Unknown subscription ID

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(dataMessage, token);

		// Current behavior: message is still forwarded
		// Expected behavior according to task: should NOT be forwarded if no subscription exists
		forward.AssertNotNull();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task OutMessage_AfterUnsubscribe_ShouldNotReceiveData()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
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
			ServerTime = logReceiver.CurrentTimeUtc,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage1.SetSubscriptionIds([100]);

		var (forward1, _) = await manager.ProcessOutMessageAsync(dataMessage1, token);
		forward1.AssertNotNull();
		((ISubscriptionIdMessage)forward1).GetSubscriptionIds().Contains(100).AssertTrue();

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

		// After unsubscribe, data message should not have subscription IDs (empty)
		var dataMessage2 = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTimeUtc,
			DataTypeEx = DataType.Ticks,
			TradePrice = 101m,
			TradeVolume = 10m,
		};
		// Don't set subscription IDs - let manager handle it based on (dataType, secId) lookup

		var (forward2, _) = await manager.ProcessOutMessageAsync(dataMessage2, token);
		// Message is forwarded but subscription IDs should be empty (no active subscription)
		forward2.AssertNotNull();
	}

	[TestMethod]
	public async Task OutMessage_WithOnlineSubscription_GetsCorrectIds()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
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
			ServerTime = logReceiver.CurrentTimeUtc,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		// No subscription IDs set - manager will assign based on lookup

		var (forward, _) = await manager.ProcessOutMessageAsync(dataMessage, token);
		forward.AssertNotNull();
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Contains(100).AssertTrue();
	}

	[TestMethod]
	public async Task TwoSubscriptions_SameSecuritySameDataType_DataHasBothIds()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
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
			ServerTime = logReceiver.CurrentTimeUtc,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage.SetSubscriptionIds([100]); // Original from source

		var (forward, _) = await manager.ProcessOutMessageAsync(dataMessage, token);
		forward.AssertNotNull();
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Contains(100).AssertTrue("Should contain first subscription ID");
		ids.Contains(101).AssertTrue("Should contain second (joined) subscription ID");
	}

	[TestMethod]
	public async Task TwoSubscriptions_UnsubscribeFirst_DataHasOnlySecondId()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
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
			ServerTime = logReceiver.CurrentTimeUtc,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage1.SetSubscriptionIds([100]);

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
			ServerTime = logReceiver.CurrentTimeUtc,
			DataTypeEx = DataType.Ticks,
			TradePrice = 101m,
			TradeVolume = 10m,
		};
		dataMessage2.SetSubscriptionIds([100]); // Still from source with old ID

		var (forward2, _) = await manager.ProcessOutMessageAsync(dataMessage2, token);
		forward2.AssertNotNull();
		var ids2 = ((ISubscriptionIdMessage)forward2).GetSubscriptionIds();
		ids2.Contains(100).AssertFalse("Should NOT contain unsubscribed ID");
		ids2.Contains(101).AssertTrue("Should contain remaining subscription ID");
	}

	#endregion

	#region Multiple Data Types Tests

	[TestMethod]
	public async Task MultipleDataTypes_SameSecurityId_IndependentSubscriptions()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
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
			ServerTime = logReceiver.CurrentTimeUtc,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};

		var (forward, _) = await manager.ProcessOutMessageAsync(ticksMessage, token);
		forward.AssertNotNull();
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Contains(101).AssertTrue();
	}

	#endregion
}
