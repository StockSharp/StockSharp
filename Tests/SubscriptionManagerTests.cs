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
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var message = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
			From = logReceiver.CurrentTime.AddHours(1),
		};

		var (toInner, toOut) = manager.ProcessInMessage(message);

		toOut.Length.AssertEqual(0);
		toInner.Length.AssertEqual(1);

		var sent = (MarketDataMessage)toInner[0];
		sent.From.AssertEqual(logReceiver.CurrentTime);
		message.From.AssertEqual(logReceiver.CurrentTime.AddHours(1));
	}

	[TestMethod]
	public void ConnectionRestored_RemapsSubscriptions()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

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
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

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
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

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
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

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
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

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
	public void OutMessage_WithUnknownSubscription_NotForwarded()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		// Create a data message with unknown subscription ID
		var dataMessage = new Level1ChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = logReceiver.CurrentTime,
		};
		dataMessage.TryAdd(Level1Fields.LastTradePrice, 100m);
		dataMessage.SetSubscriptionIds([999]); // Unknown subscription ID

		var (forward, extraOut) = manager.ProcessOutMessage(dataMessage);

		// Message should NOT be forwarded if subscription doesn't exist
		forward.AssertNull();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void OutMessage_WithNoSubscriptionId_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		// Create a data message with no subscription IDs
		var dataMessage = new Level1ChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = logReceiver.CurrentTime,
		};
		dataMessage.TryAdd(Level1Fields.LastTradePrice, 100m);
		// No subscription IDs set

		var (forward, extraOut) = manager.ProcessOutMessage(dataMessage);

		// Message should be forwarded (current behavior - not filtered)
		forward.AssertNotNull();
	}

	[TestMethod]
	public void OutMessage_AfterUnsubscribe_NotForwarded()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

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
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage.SetSubscriptionIds([100]); // Old subscription ID

		var (forward, extraOut) = manager.ProcessOutMessage(dataMessage);

		// Message should NOT be forwarded after unsubscribe
		forward.AssertNull();
	}

	[TestMethod]
	public void TwoSubscriptions_SameSecuritySameDataType_BothTracked()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

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
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

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

	#region Multiple SubscriptionIds in Data Messages

	[TestMethod]
	public void OutMessage_MultipleKnownIds_ForwardsAll()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Two subscriptions
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

		// Data with both IDs
		var dataMessage = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage.SetSubscriptionIds([100, 101]);

		var (forward, extraOut) = manager.ProcessOutMessage(dataMessage);

		forward.AssertNotNull("Data with multiple known IDs should be forwarded");
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Contains(100).AssertTrue("Should contain first ID");
		ids.Contains(101).AssertTrue("Should contain second ID");
	}

	[TestMethod]
	public void OutMessage_MixKnownAndUnknownIds_ForwardsOnlyKnown()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// One subscription
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		// Data with known + unknown IDs
		var dataMessage = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage.SetSubscriptionIds([100, 999]);

		var (forward, extraOut) = manager.ProcessOutMessage(dataMessage);

		forward.AssertNotNull("Data should be forwarded for known ID");
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Contains(100).AssertTrue("Should contain known ID");
		ids.Contains(999).AssertFalse("Should NOT contain unknown ID");
	}

	[TestMethod]
	public void OutMessage_MultipleIds_OneUnsubscribed_ForwardsOnlyActive()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Two subscriptions
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

		// Data still arrives with both IDs
		var dataMessage = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage.SetSubscriptionIds([100, 101]);

		var (forward, extraOut) = manager.ProcessOutMessage(dataMessage);

		forward.AssertNotNull("Data should be forwarded for remaining active subscription");
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Contains(100).AssertFalse("Should NOT contain unsubscribed ID");
		ids.Contains(101).AssertTrue("Should contain active ID");
	}

	[TestMethod]
	public void OutMessage_AllIdsUnknown_NotForwarded()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var dataMessage = new ExecutionMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMessage.SetSubscriptionIds([888, 999]);

		var (forward, extraOut) = manager.ProcessOutMessage(dataMessage);

		forward.AssertNull("Data with all unknown IDs should NOT be forwarded");
	}

	[TestMethod]
	public void SubscriptionFinished_OneOfTwo_OtherStillReceivesData()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Two subscriptions
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 100, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 101, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 101 });

		// Finished for first subscription
		var (finForward, finExtra) = manager.ProcessOutMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = 100 });

		// Data with both IDs arrives after finished
		var dataMessage = new ExecutionMessage
		{
			SecurityId = secId, ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks, TradePrice = 100m, TradeVolume = 10m,
		};
		dataMessage.SetSubscriptionIds([100, 101]);

		var (forward, _) = manager.ProcessOutMessage(dataMessage);

		forward.AssertNotNull("Data should still be forwarded for remaining subscription");
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Contains(100).AssertFalse("Finished subscription should be filtered out");
		ids.Contains(101).AssertTrue("Active subscription should remain");
	}

	[TestMethod]
	public void SubscriptionError_OneOfTwo_OtherStillReceivesData()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Two subscriptions
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 100, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 101, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 101 });

		// Error for first subscription
		manager.ProcessOutMessage(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 100,
			Error = new InvalidOperationException("fail"),
		});

		// Data with both IDs arrives
		var dataMessage = new ExecutionMessage
		{
			SecurityId = secId, ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks, TradePrice = 100m, TradeVolume = 10m,
		};
		dataMessage.SetSubscriptionIds([100, 101]);

		var (forward, _) = manager.ProcessOutMessage(dataMessage);

		forward.AssertNotNull("Data should still be forwarded for remaining subscription");
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Contains(100).AssertFalse("Errored subscription should be filtered out");
		ids.Contains(101).AssertTrue("Active subscription should remain");
	}

	[TestMethod]
	public void SubscriptionOnline_OneOfTwo_BothStillReceiveData()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Two subscriptions — both get response
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 100, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 101, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 101 });

		// Online for first only
		manager.ProcessOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = 100 });

		// Data with both IDs
		var dataMessage = new ExecutionMessage
		{
			SecurityId = secId, ServerTime = logReceiver.CurrentTime,
			DataTypeEx = DataType.Ticks, TradePrice = 100m, TradeVolume = 10m,
		};
		dataMessage.SetSubscriptionIds([100, 101]);

		var (forward, _) = manager.ProcessOutMessage(dataMessage);

		forward.AssertNotNull("Data should be forwarded");
		var ids = ((ISubscriptionIdMessage)forward).GetSubscriptionIds();
		ids.Contains(100).AssertTrue("Online subscription should be present");
		ids.Contains(101).AssertTrue("Active (not yet online) subscription should also be present");
	}

	#endregion

	#region Status Message Handling

	[TestMethod]
	public void StatusMessage_Response_TwoSubs_EachForwardedWithCorrectId()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Subscribe two
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 100, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 101, SecurityId = secId, DataType2 = DataType.Ticks,
		});

		// Response OK for sub1
		var resp1 = new SubscriptionResponseMessage { OriginalTransactionId = 100 };
		var (forward1, extra1) = manager.ProcessOutMessage(resp1);

		forward1.AssertNotNull("Response OK for sub1 should be forwarded");
		forward1.AssertSame(resp1);
		((SubscriptionResponseMessage)forward1).OriginalTransactionId.AssertEqual(100);
		extra1.Length.AssertEqual(0);

		// Response OK for sub2
		var resp2 = new SubscriptionResponseMessage { OriginalTransactionId = 101 };
		var (forward2, extra2) = manager.ProcessOutMessage(resp2);

		forward2.AssertNotNull("Response OK for sub2 should be forwarded");
		((SubscriptionResponseMessage)forward2).OriginalTransactionId.AssertEqual(101);
		extra2.Length.AssertEqual(0);
	}

	[TestMethod]
	public void StatusMessage_Response_Error_OneOfTwo_OtherStillActive()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Two subs, both confirmed
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 100, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 101, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 101 });

		// Error for sub1
		var errorResp = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 100,
			Error = new InvalidOperationException("fail"),
		};
		var (forward, extra) = manager.ProcessOutMessage(errorResp);

		forward.AssertNotNull("Error response should be forwarded");
		((SubscriptionResponseMessage)forward).OriginalTransactionId.AssertEqual(100);
		((SubscriptionResponseMessage)forward).Error.AssertNotNull();
		extra.Length.AssertEqual(0);

		// Sub2 should still be active (can unsubscribe)
		var (toInner, toOut) = manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = false, TransactionId = 102, OriginalTransactionId = 101,
			SecurityId = secId, DataType2 = DataType.Ticks,
		});
		toInner.Length.AssertEqual(1, "Sub2 should still be active after sub1 error");
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void StatusMessage_Online_TwoSubs_EachForwardedWithCorrectId()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Two subs, both confirmed
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 100, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 101, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 101 });

		// Online for sub1
		var online1 = new SubscriptionOnlineMessage { OriginalTransactionId = 100 };
		var (forward1, extra1) = manager.ProcessOutMessage(online1);

		forward1.AssertNotNull("Online for sub1 should be forwarded");
		((SubscriptionOnlineMessage)forward1).OriginalTransactionId.AssertEqual(100);
		extra1.Length.AssertEqual(0);

		// Online for sub2
		var online2 = new SubscriptionOnlineMessage { OriginalTransactionId = 101 };
		var (forward2, extra2) = manager.ProcessOutMessage(online2);

		forward2.AssertNotNull("Online for sub2 should be forwarded");
		((SubscriptionOnlineMessage)forward2).OriginalTransactionId.AssertEqual(101);
		extra2.Length.AssertEqual(0);
	}

	[TestMethod]
	public void StatusMessage_Finished_OneOfTwo_OtherStillActive()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Two subs, both confirmed
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 100, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 101, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 101 });

		// Finished for sub1
		var finished = new SubscriptionFinishedMessage { OriginalTransactionId = 100 };
		var (forward, extra) = manager.ProcessOutMessage(finished);

		forward.AssertNotNull("Finished should be forwarded");
		((SubscriptionFinishedMessage)forward).OriginalTransactionId.AssertEqual(100);
		extra.Length.AssertEqual(0);

		// Sub2 should still be active
		var (toInner, toOut) = manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = false, TransactionId = 102, OriginalTransactionId = 101,
			SecurityId = secId, DataType2 = DataType.Ticks,
		});
		toInner.Length.AssertEqual(1, "Sub2 should still be active after sub1 finished");
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void StatusMessage_ReSubscribe_ResponseSuppressed()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Subscribe and go Online
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 100, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });
		manager.ProcessOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = 100 });

		// Trigger re-subscribe
		manager.ProcessOutMessage(new ConnectionRestoredMessage { IsResetState = true });
		var (reSubMsgs, _) = manager.ProcessInMessage(new ProcessSuspendedMessage());
		reSubMsgs.Length.AssertGreater(0, "Should have re-subscribe message");

		var reSubMsg = (MarketDataMessage)reSubMsgs[0];
		var reSubTransId = reSubMsg.TransactionId;
		reSubTransId.AssertNotEqual(100, "Re-subscribe should have new TransactionId");

		// Response OK for re-subscribe — should be suppressed
		var resp = new SubscriptionResponseMessage { OriginalTransactionId = reSubTransId };
		var (forward, extra) = manager.ProcessOutMessage(resp);

		forward.AssertNull("Response for re-subscribe should be suppressed");
		extra.Length.AssertEqual(0);
	}

	[TestMethod]
	public void StatusMessage_ReSubscribe_OnlineSuppressedWhenAlreadyOnline()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Subscribe and go Online
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 100, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });
		manager.ProcessOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = 100 });

		// Trigger re-subscribe
		manager.ProcessOutMessage(new ConnectionRestoredMessage { IsResetState = true });
		var (reSubMsgs, _) = manager.ProcessInMessage(new ProcessSuspendedMessage());
		var reSubTransId = ((MarketDataMessage)reSubMsgs[0]).TransactionId;

		// Response OK for re-subscribe (suppressed)
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = reSubTransId });

		// Online for re-subscribe — already Online, should be suppressed
		var online = new SubscriptionOnlineMessage { OriginalTransactionId = reSubTransId };
		var (forward, extra) = manager.ProcessOutMessage(online);

		forward.AssertNull("Online for re-subscribe should be suppressed when already online");
		extra.Length.AssertEqual(0);
	}

	[TestMethod]
	public void StatusMessage_ReSubscribe_FinishedSuppressed()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Subscribe and go Online
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 100, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });
		manager.ProcessOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = 100 });

		// Trigger re-subscribe
		manager.ProcessOutMessage(new ConnectionRestoredMessage { IsResetState = true });
		var (reSubMsgs, _) = manager.ProcessInMessage(new ProcessSuspendedMessage());
		var reSubTransId = ((MarketDataMessage)reSubMsgs[0]).TransactionId;

		// Response OK for re-subscribe (suppressed)
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = reSubTransId });

		// Finished for re-subscribe — should be suppressed
		var finished = new SubscriptionFinishedMessage { OriginalTransactionId = reSubTransId };
		var (forward, extra) = manager.ProcessOutMessage(finished);

		forward.AssertNull("Finished for re-subscribe should be suppressed");
		extra.Length.AssertEqual(0);
	}

	[TestMethod]
	public void StatusMessage_ReSubscribe_OriginalIdRestored()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

		var secId = Helper.CreateSecurityId();

		// Subscribe and go Online
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 100, SecurityId = secId, DataType2 = DataType.Ticks,
		});
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		// Trigger re-subscribe
		manager.ProcessOutMessage(new ConnectionRestoredMessage { IsResetState = true });
		var (reSubMsgs, _) = manager.ProcessInMessage(new ProcessSuspendedMessage());
		var reSubTransId = ((MarketDataMessage)reSubMsgs[0]).TransactionId;

		// Response OK for re-subscribe (suppressed) — but check OriginalTransactionId was restored
		var resp = new SubscriptionResponseMessage { OriginalTransactionId = reSubTransId };
		manager.ProcessOutMessage(resp);

		// The message's OriginalTransactionId should be replaced with original ID
		resp.OriginalTransactionId.AssertEqual(100, "OriginalTransactionId should be restored to original");
	}

	#endregion

	#region Multiple Data Types Tests

	[TestMethod]
	public void MultipleDataTypes_SameSecurityId_IndependentSubscriptions()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage(), new SubscriptionManagerState());

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
