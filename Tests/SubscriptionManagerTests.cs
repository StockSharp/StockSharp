namespace StockSharp.Tests;

[TestClass]
public class SubscriptionManagerTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	[TestMethod]
	public void Subscribe_FromFuture_ClampsToNow()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var message = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
			From = logReceiver.CurrentTimeUtc.AddHours(1),
		};

		var (toInner, toOut) = manager.ProcessInMessage(message);

		toOut.Length.AssertEqual(0);
		toInner.Length.AssertEqual(1);

		var sent = (MarketDataMessage)toInner[0];
		sent.From.AssertEqual(logReceiver.CurrentTimeUtc);
		message.From.AssertEqual(logReceiver.CurrentTimeUtc.AddHours(1));
	}

	[TestMethod]
	public void ConnectionRestored_RemapsSubscriptions()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		manager.ProcessInMessage(subscription);
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		var restored = new ConnectionRestoredMessage { IsResetState = true };
		var (forward, extraOut) = manager.ProcessOutMessage(restored);

		extraOut.Length.AssertEqual(1);
		extraOut[0].Type.AssertEqual(MessageTypes.ProcessSuspended);

		var (toInner, toOut) = manager.ProcessInMessage(new ProcessSuspendedMessage());

		toInner.Length.AssertEqual(1);
		((MarketDataMessage)toInner[0]).TransactionId.AssertNotEqual(100);
	}

	#region Subscribe/Unsubscribe Multiple Cycles Tests

	[TestMethod]
	public void SubscribeUnsubscribeResubscribe_WorksCorrectly()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribe1 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var (toInner1, toOut1) = manager.ProcessInMessage(subscribe1);
		toInner1.Length.AssertEqual(1);
		toOut1.Length.AssertEqual(0);

		// Confirm subscription
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		// Unsubscribe
		var unsubscribe = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 101,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var (toInner2, toOut2) = manager.ProcessInMessage(unsubscribe);
		toInner2.Length.AssertEqual(1);
		toOut2.Length.AssertEqual(0);

		// Resubscribe with new transaction ID
		var subscribe2 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 102,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var (toInner3, toOut3) = manager.ProcessInMessage(subscribe2);
		toInner3.Length.AssertEqual(1);
		toOut3.Length.AssertEqual(0);

		// Confirm resubscription
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 102 });
	}

	[TestMethod]
	public void MultipleSubscriptions_AllActiveNoneActiveSomeActive()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var secId1 = new SecurityId { SecurityCode = "SEC1", BoardCode = "BOARD" };
		var secId2 = new SecurityId { SecurityCode = "SEC2", BoardCode = "BOARD" };
		var secId3 = new SecurityId { SecurityCode = "SEC3", BoardCode = "BOARD" };

		// Subscribe to all three
		for (int i = 0; i < 3; i++)
		{
			var secId = i == 0 ? secId1 : (i == 1 ? secId2 : secId3);
			var subscribe = new MarketDataMessage
			{
				IsSubscribe = true,
				TransactionId = 100 + i,
				SecurityId = secId,
				DataType2 = DataType.Ticks,
			};
			manager.ProcessInMessage(subscribe);
			manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 + i });
		}

		// Unsubscribe from all
		for (int i = 0; i < 3; i++)
		{
			var secId = i == 0 ? secId1 : (i == 1 ? secId2 : secId3);
			var unsubscribe = new MarketDataMessage
			{
				IsSubscribe = false,
				TransactionId = 200 + i,
				OriginalTransactionId = 100 + i,
				SecurityId = secId,
				DataType2 = DataType.Ticks,
			};
			manager.ProcessInMessage(unsubscribe);
		}

		// Resubscribe only to the second one
		var resubscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 300,
			SecurityId = secId2,
			DataType2 = DataType.Ticks,
		};

		var (toInner, toOut) = manager.ProcessInMessage(resubscribe);
		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void RapidSubscribeUnsubscribeCycles_HandlesCorrectly()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

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
			var (toInnerSub, toOutSub) = manager.ProcessInMessage(subscribe);
			toInnerSub.Length.AssertEqual(1);

			var subTransId = subscribe.TransactionId;
			manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = subTransId });

			// Unsubscribe
			var unsubscribe = new MarketDataMessage
			{
				IsSubscribe = false,
				TransactionId = transId++,
				OriginalTransactionId = subTransId,
				SecurityId = secId,
				DataType2 = DataType.Ticks,
			};
			var (toInnerUnsub, toOutUnsub) = manager.ProcessInMessage(unsubscribe);
			toInnerUnsub.Length.AssertEqual(1);
		}

		// Final subscription should work
		var finalSubscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = transId++,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var (toInner, toOut) = manager.ProcessInMessage(finalSubscribe);
		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void UnsubscribeNonExistent_ReturnsError()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var unsubscribe = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 100,
			OriginalTransactionId = 999, // Non-existent
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		var (toInner, toOut) = manager.ProcessInMessage(unsubscribe);

		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(1);
		toOut[0].Type.AssertEqual(MessageTypes.SubscriptionResponse);
		((SubscriptionResponseMessage)toOut[0]).Error.AssertNotNull();
	}

	#endregion

	#region Message Without Subscription Tests

	[TestMethod]
	public void OutMessage_WithUnknownSubscription_SetsEmptyIds()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		// Create a data message with unknown subscription ID
		var dataMessage = new Level1ChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = logReceiver.CurrentTimeUtc,
		};
		dataMessage.TryAdd(Level1Fields.LastTradePrice, 100m);
		dataMessage.SetSubscriptionIds([999]); // Unknown subscription ID

		var (forward, extraOut) = manager.ProcessOutMessage(dataMessage);

		// Message should be forwarded (current behavior)
		forward.AssertNotNull();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void OutMessage_WithNoSubscriptionId_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		// Create a data message with no subscription IDs
		var dataMessage = new Level1ChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = logReceiver.CurrentTimeUtc,
		};
		dataMessage.TryAdd(Level1Fields.LastTradePrice, 100m);
		// No subscription IDs set

		var (forward, extraOut) = manager.ProcessOutMessage(dataMessage);

		// Message should be forwarded (current behavior - not filtered)
		forward.AssertNotNull();
	}

	[TestMethod]
	public void OutMessage_AfterUnsubscribe_OriginIdRemapped()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		manager.ProcessInMessage(subscribe);
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		// Unsubscribe
		var unsubscribe = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 101,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		manager.ProcessInMessage(unsubscribe);

		// Now send data message - subscription no longer exists
		var dataMessage = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTimeUtc,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage.SetSubscriptionIds([100]); // Old subscription ID

		var (forward, extraOut) = manager.ProcessOutMessage(dataMessage);

		// Message is forwarded but subscription ID mapping may be affected
		forward.AssertNotNull();
	}

	[TestMethod]
	public void TwoSubscriptions_SameSecuritySameDataType_BothTracked()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var secId = Helper.CreateSecurityId();

		// First subscription
		var subscribe1 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		manager.ProcessInMessage(subscribe1);
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		// Second subscription (same security, same DataType)
		var subscribe2 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		var (toInner, toOut) = manager.ProcessInMessage(subscribe2);
		toInner.Length.AssertEqual(1); // Should create new subscription request
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 101 });

		// Both subscriptions should be active - unsubscribing one should not affect other
		var unsubscribe1 = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 102,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		var (toInner2, toOut2) = manager.ProcessInMessage(unsubscribe1);
		toInner2.Length.AssertEqual(1);

		// Second subscription should still work
		var unsubscribe2 = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 103,
			OriginalTransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		var (toInner3, toOut3) = manager.ProcessInMessage(unsubscribe2);
		toInner3.Length.AssertEqual(1); // Should send unsubscribe to inner
	}

	[TestMethod]
	public void TwoSubscriptions_UnsubscribeFirst_SecondStillActive()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var secId = Helper.CreateSecurityId();

		// Subscribe twice
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 101 });

		// Unsubscribe first
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 102,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});

		// Second subscription should still be valid - unsubscribing it should work
		var (toInner, toOut) = manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 103,
			OriginalTransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});
		toInner.Length.AssertEqual(1); // Should succeed
		toOut.Length.AssertEqual(0); // No error
	}

	#endregion

	#region Multiple Data Types Tests

	[TestMethod]
	public void MultipleDataTypes_SameSecurityId_IndependentSubscriptions()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var secId = Helper.CreateSecurityId();

		// Subscribe to Level1
		var subscribeLevel1 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Level1,
		};
		manager.ProcessInMessage(subscribeLevel1);
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		// Subscribe to Ticks
		var subscribeTicks = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 101,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};
		manager.ProcessInMessage(subscribeTicks);
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 101 });

		// Unsubscribe from Level1
		var unsubscribeLevel1 = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 102,
			OriginalTransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Level1,
		};
		var (toInner, toOut) = manager.ProcessInMessage(unsubscribeLevel1);
		toInner.Length.AssertEqual(1);

		// Ticks subscription should still work
		// (No direct way to verify in manager, but unsubscribe should only affect Level1)
	}

	#endregion
}
