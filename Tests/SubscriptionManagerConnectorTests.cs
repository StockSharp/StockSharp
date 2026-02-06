namespace StockSharp.Tests;

[TestClass]
public class SubscriptionManagerConnectorTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver { }

	#region Helpers

	private static ConnectorSubscriptionManager CreateManager(bool sendUnsubscribeWhenDisconnected = true)
		=> new(new TestReceiver(), new IncrementalIdGenerator(), sendUnsubscribeWhenDisconnected);

	private static Subscription CreateTickSubscription()
		=> new(new MarketDataMessage
		{
			IsSubscribe = true,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		});

	private static Subscription CreateOrderStatusSubscription()
		=> new(new OrderStatusMessage { IsSubscribe = true });

	/// <summary>
	/// Subscribe and process success response → state becomes Active.
	/// </summary>
	private static long SubscribeAndActivate(ConnectorSubscriptionManager manager, Subscription subscription)
	{
		manager.Subscribe(subscription);
		var transId = subscription.TransactionId;

		manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = transId },
			out _, out _, out _);

		return transId;
	}

	/// <summary>
	/// Subscribe, activate, then go Online.
	/// </summary>
	private static long SubscribeAndGoOnline(ConnectorSubscriptionManager manager, Subscription subscription)
	{
		var transId = SubscribeAndActivate(manager, subscription);

		manager.ProcessSubscriptionOnlineMessage(
			new SubscriptionOnlineMessage { OriginalTransactionId = transId },
			out _);

		return transId;
	}

	private static ISubscriptionIdMessage CreateDataMessage(params long[] subscriptionIds)
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
		};
		msg.SetSubscriptionIds(subscriptionIds);
		return msg;
	}

	#endregion

	#region Basic Subscribe Lifecycle

	[TestMethod]
	public void Subscribe_AssignsTransactionId_AndSendsRequest()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();

		var actions = manager.Subscribe(subscription);

		subscription.TransactionId.AssertNotEqual(0);
		actions.Items.Length.AssertEqual(1);
		actions.Items[0].Type.AssertEqual(ConnectorSubscriptionManager.Actions.Item.Types.SendInMessage);

		var sent = (MarketDataMessage)actions.Items[0].Message;
		sent.TransactionId.AssertEqual(subscription.TransactionId);
	}

	[TestMethod]
	public void Subscribe_SuccessResponse_StateBecomesActive()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();

		SubscribeAndActivate(manager, subscription);

		subscription.State.AssertEqual(SubscriptionStates.Active);
	}

	[TestMethod]
	public void Subscribe_Online_StateBecomesOnline()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();

		SubscribeAndGoOnline(manager, subscription);

		subscription.State.AssertEqual(SubscriptionStates.Online);
	}

	[TestMethod]
	public void Subscribe_Finished_StateBecomesFinished_AndRemoved()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		var transId = SubscribeAndActivate(manager, subscription);

		var result = manager.ProcessSubscriptionFinishedMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = transId },
			out _);

		IsNotNull(result);
		subscription.State.AssertEqual(SubscriptionStates.Finished);

		// Subscription should be removed from manager
		manager.Subscriptions.Count(s => s.TransactionId == transId)
			.AssertEqual(0, "Finished subscription should be removed");
	}

	[TestMethod]
	public void Unsubscribe_Response_StateStopped_AndRemoved()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		var transId = SubscribeAndActivate(manager, subscription);

		var unsubActions = manager.UnSubscribe(subscription);
		unsubActions.Items.Length.AssertEqual(1, "Unsubscribe should produce SendInMessage action");

		// Get the unsubscribe transaction ID
		var unsubMsg = (MarketDataMessage)unsubActions.Items[0].Message;
		var unsubTransId = unsubMsg.TransactionId;

		// Process unsubscribe response
		var result = manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = unsubTransId },
			out var originalMsg, out _, out _);

		IsNotNull(result);
		subscription.State.AssertEqual(SubscriptionStates.Stopped);

		// Subscription should be removed
		manager.Subscriptions.Count(s => s.TransactionId == transId)
			.AssertEqual(0, "Unsubscribed subscription should be removed");
	}

	#endregion

	#region Unknown Subscription Edge Cases

	[TestMethod]
	public void ProcessResponse_UnknownTransactionId_ReturnsNull()
	{
		var manager = CreateManager();

		var result = manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = 999 },
			out var originalMsg, out var unexpectedCancelled, out _);

		result.AssertNull("Response for unknown ID should return null");
		originalMsg.AssertNull("Original message should be null for unknown ID");
		unexpectedCancelled.AssertFalse("unexpectedCancelled should be false for unknown ID");
	}

	[TestMethod]
	public void ProcessOnline_UnknownTransactionId_ReturnsNull()
	{
		var manager = CreateManager();

		var result = manager.ProcessSubscriptionOnlineMessage(
			new SubscriptionOnlineMessage { OriginalTransactionId = 999 },
			out _);

		result.AssertNull("Online for unknown ID should return null");
	}

	[TestMethod]
	public void ProcessFinished_UnknownTransactionId_ReturnsNull()
	{
		var manager = CreateManager();

		var result = manager.ProcessSubscriptionFinishedMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = 999 },
			out _);

		result.AssertNull("Finished for unknown ID should return null");
	}

	[TestMethod]
	public void GetSubscriptions_UnknownIds_ReturnsEmpty()
	{
		var manager = CreateManager();

		var subs = manager.GetSubscriptions(CreateDataMessage(999, 888)).ToArray();

		subs.Length.AssertEqual(0, "GetSubscriptions for unknown IDs should return empty");
	}

	[TestMethod]
	public void TryGetSubscription_UnknownId_ReturnsNull()
	{
		var manager = CreateManager();

		var result = manager.TryGetSubscription(999, false, false, null);

		result.AssertNull("TryGetSubscription for unknown ID should return null");
	}

	#endregion

	#region Error Handling

	[TestMethod]
	public void ProcessResponse_Error_RemovesSubscription()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		manager.Subscribe(subscription);
		var transId = subscription.TransactionId;

		var result = manager.ProcessResponse(
			new SubscriptionResponseMessage
			{
				OriginalTransactionId = transId,
				Error = new InvalidOperationException("test error"),
			},
			out _, out var unexpectedCancelled, out _);

		IsNotNull(result);
		subscription.State.AssertEqual(SubscriptionStates.Error);
		unexpectedCancelled.AssertFalse("Not unexpected if never was active");

		// Subscription should be removed
		manager.Subscriptions.Count(s => s.TransactionId == transId)
			.AssertEqual(0, "Errored subscription should be removed");
	}

	[TestMethod]
	public void ProcessResponse_ErrorAfterActive_ShouldMarkUnexpectedCancelled()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		SubscribeAndActivate(manager, subscription);

		var result = manager.ProcessResponse(
			new SubscriptionResponseMessage
			{
				OriginalTransactionId = subscription.TransactionId,
				Error = new InvalidOperationException("boom"),
			},
			out _, out var unexpectedCancelled, out _);

		unexpectedCancelled.AssertTrue("Should be unexpected if was active");
	}

	[TestMethod]
	public void ProcessResponse_ErrorAfterOnline_ShouldMarkUnexpectedCancelled()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		SubscribeAndGoOnline(manager, subscription);

		var result = manager.ProcessResponse(
			new SubscriptionResponseMessage
			{
				OriginalTransactionId = subscription.TransactionId,
				Error = new InvalidOperationException("boom"),
			},
			out _, out var unexpectedCancelled, out _);

		unexpectedCancelled.AssertTrue("Should be unexpected if was online");
	}

	[TestMethod]
	public void GetSubscriptions_AfterError_ReturnsEmpty()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		manager.Subscribe(subscription);
		var transId = subscription.TransactionId;

		// Error response
		manager.ProcessResponse(
			new SubscriptionResponseMessage
			{
				OriginalTransactionId = transId,
				Error = new InvalidOperationException("fail"),
			},
			out _, out _, out _);

		var subs = manager.GetSubscriptions(CreateDataMessage(transId)).ToArray();
		subs.Length.AssertEqual(0, "No subscriptions should be found after error");
	}

	[TestMethod]
	public void ProcessOnline_AfterError_ReturnsNull()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		manager.Subscribe(subscription);
		var transId = subscription.TransactionId;

		// Error
		manager.ProcessResponse(
			new SubscriptionResponseMessage
			{
				OriginalTransactionId = transId,
				Error = new InvalidOperationException("fail"),
			},
			out _, out _, out _);

		// Online after error
		var result = manager.ProcessSubscriptionOnlineMessage(
			new SubscriptionOnlineMessage { OriginalTransactionId = transId },
			out _);

		result.AssertNull("Online after error should return null (subscription removed)");
	}

	[TestMethod]
	public void ProcessFinished_AfterError_ReturnsNull()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		manager.Subscribe(subscription);
		var transId = subscription.TransactionId;

		// Error
		manager.ProcessResponse(
			new SubscriptionResponseMessage
			{
				OriginalTransactionId = transId,
				Error = new InvalidOperationException("fail"),
			},
			out _, out _, out _);

		// Finished after error
		var result = manager.ProcessSubscriptionFinishedMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = transId },
			out _);

		result.AssertNull("Finished after error should return null (subscription removed)");
	}

	#endregion

	#region Post-Finished Edge Cases

	[TestMethod]
	public void GetSubscriptions_AfterFinished_ReturnsEmpty()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		var transId = SubscribeAndActivate(manager, subscription);

		manager.ProcessSubscriptionFinishedMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = transId },
			out _);

		var subs = manager.GetSubscriptions(CreateDataMessage(transId)).ToArray();
		subs.Length.AssertEqual(0, "No subscriptions should be found after Finished");
	}

	[TestMethod]
	public void ProcessOnline_AfterFinished_ReturnsNull()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		var transId = SubscribeAndActivate(manager, subscription);

		manager.ProcessSubscriptionFinishedMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = transId },
			out _);

		var result = manager.ProcessSubscriptionOnlineMessage(
			new SubscriptionOnlineMessage { OriginalTransactionId = transId },
			out _);

		result.AssertNull("Online after Finished should return null");
	}

	[TestMethod]
	public void ProcessFinished_Twice_SecondReturnsNull()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		var transId = SubscribeAndActivate(manager, subscription);

		var first = manager.ProcessSubscriptionFinishedMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = transId },
			out _);
		IsNotNull(first, "First Finished should return subscription");

		var second = manager.ProcessSubscriptionFinishedMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = transId },
			out _);
		second.AssertNull("Second Finished should return null (already removed)");
	}

	[TestMethod]
	public void ProcessResponse_AfterFinished_ReturnsNull()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		var transId = SubscribeAndActivate(manager, subscription);

		// Finished first (removes subscription)
		manager.ProcessSubscriptionFinishedMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = transId },
			out _);

		// Late response arrives
		var result = manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = transId },
			out _, out _, out _);

		result.AssertNull("Response after Finished should return null");
	}

	#endregion

	#region Post-Unsubscribe Edge Cases

	[TestMethod]
	public void GetSubscriptions_BetweenUnsubscribeAndResponse_ShouldNotReturnSubscription()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		var transId = SubscribeAndGoOnline(manager, subscription);

		// Send unsubscribe but do NOT process response yet
		manager.UnSubscribe(subscription);

		// Data arrives for this subscription — should it still be found?
		var subs = manager.GetSubscriptions(CreateDataMessage(transId)).ToArray();

		// After user called UnSubscribe, data should NOT be delivered
		subs.Length.AssertEqual(0,
			"Data should not be delivered for subscription that is being unsubscribed");
	}

	[TestMethod]
	public void GetSubscriptions_AfterUnsubscribeResponse_ReturnsEmpty()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		var transId = SubscribeAndGoOnline(manager, subscription);

		var unsubActions = manager.UnSubscribe(subscription);
		var unsubMsg = (MarketDataMessage)unsubActions.Items[0].Message;

		// Process unsubscribe response
		manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = unsubMsg.TransactionId },
			out _, out _, out _);

		var subs = manager.GetSubscriptions(CreateDataMessage(transId)).ToArray();
		subs.Length.AssertEqual(0, "No subscriptions after unsubscribe response");
	}

	[TestMethod]
	public void ProcessOnline_AfterUnsubscribeResponse_ReturnsNull()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		var transId = SubscribeAndGoOnline(manager, subscription);

		var unsubActions = manager.UnSubscribe(subscription);
		var unsubMsg = (MarketDataMessage)unsubActions.Items[0].Message;

		manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = unsubMsg.TransactionId },
			out _, out _, out _);

		var result = manager.ProcessSubscriptionOnlineMessage(
			new SubscriptionOnlineMessage { OriginalTransactionId = transId },
			out _);

		result.AssertNull("Online after unsubscribe should return null");
	}

	[TestMethod]
	public void Unsubscribe_WhenDisconnected_ShouldRemoveLocally()
	{
		// sendUnsubscribeWhenDisconnected=false, ConnectionState=Disconnected
		var manager = CreateManager(sendUnsubscribeWhenDisconnected: false);
		manager.ConnectionState = ConnectionStates.Disconnected;

		var subscription = CreateTickSubscription();
		SubscribeAndActivate(manager, subscription);
		var transId = subscription.TransactionId;

		var actions = manager.UnSubscribe(subscription);

		// Should NOT produce SendInMessage (removed locally without sending)
		actions.Items.Count(i => i.Type == ConnectorSubscriptionManager.Actions.Item.Types.SendInMessage)
			.AssertEqual(0, "No message should be sent when disconnected with sendUnsubscribeWhenDisconnected=false");

		// Subscription should be removed locally
		manager.Subscriptions.Count(s => s.TransactionId == transId)
			.AssertEqual(0, "Subscription should be removed locally on disconnect");
	}

	#endregion

	#region Subscription Reuse (Subscribe/Unsubscribe cycle)

	[TestMethod]
	public void Resubscribe_AfterUnsubscribe_ShouldGenerateNewTransactionId()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();

		// First subscribe
		manager.Subscribe(subscription);
		var firstTransId = subscription.TransactionId;
		firstTransId.AssertNotEqual(0);

		SubscribeAndActivate(manager, subscription);

		// Unsubscribe
		var unsubActions = manager.UnSubscribe(subscription);
		var unsubMsg = (MarketDataMessage)unsubActions.Items[0].Message;

		manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = unsubMsg.TransactionId },
			out _, out _, out _);

		subscription.State.AssertEqual(SubscriptionStates.Stopped);

		// Re-subscribe — must get NEW TransactionId
		manager.Subscribe(subscription);
		var secondTransId = subscription.TransactionId;

		secondTransId.AssertNotEqual(firstTransId,
			"Re-subscribe must generate a new TransactionId, not reuse the old one");
		secondTransId.AssertNotEqual(0);
	}

	[TestMethod]
	public void Resubscribe_AfterError_ShouldGenerateNewTransactionId()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();

		// Subscribe → error
		manager.Subscribe(subscription);
		var firstTransId = subscription.TransactionId;

		manager.ProcessResponse(
			new SubscriptionResponseMessage
			{
				OriginalTransactionId = firstTransId,
				Error = new InvalidOperationException("fail"),
			},
			out _, out _, out _);

		subscription.State.AssertEqual(SubscriptionStates.Error);

		// Re-subscribe after error — must get NEW TransactionId
		manager.Subscribe(subscription);
		var secondTransId = subscription.TransactionId;

		secondTransId.AssertNotEqual(firstTransId,
			"Re-subscribe after error must generate a new TransactionId");
	}

	[TestMethod]
	public void Resubscribe_AfterFinished_ShouldGenerateNewTransactionId()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();

		// Subscribe → Active → Finished
		var firstTransId = SubscribeAndActivate(manager, subscription);

		manager.ProcessSubscriptionFinishedMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = firstTransId },
			out _);

		subscription.State.AssertEqual(SubscriptionStates.Finished);

		// Re-subscribe after finished — must get NEW TransactionId
		manager.Subscribe(subscription);
		var secondTransId = subscription.TransactionId;

		secondTransId.AssertNotEqual(firstTransId,
			"Re-subscribe after finished must generate a new TransactionId");
	}

	[TestMethod]
	public void Resubscribe_AfterUnsubscribe_FullLifecycle()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();

		// === Cycle 1: Subscribe → Online → Unsubscribe ===
		manager.Subscribe(subscription);
		var transId1 = subscription.TransactionId;

		manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = transId1 },
			out _, out _, out _);
		manager.ProcessSubscriptionOnlineMessage(
			new SubscriptionOnlineMessage { OriginalTransactionId = transId1 },
			out _);

		subscription.State.AssertEqual(SubscriptionStates.Online);

		// Data should be found for this subscription
		manager.GetSubscriptions(CreateDataMessage(transId1)).Count()
			.AssertEqual(1, "Subscription should be active in cycle 1");

		// Unsubscribe
		var unsub1 = manager.UnSubscribe(subscription);
		var unsubMsg1 = (MarketDataMessage)unsub1.Items[0].Message;
		manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = unsubMsg1.TransactionId },
			out _, out _, out _);

		subscription.State.AssertEqual(SubscriptionStates.Stopped);

		// Old TransactionId should no longer work
		manager.GetSubscriptions(CreateDataMessage(transId1)).Count()
			.AssertEqual(0, "Old subscription ID should not work after unsubscribe");

		// === Cycle 2: Re-subscribe → Online ===
		manager.Subscribe(subscription);
		var transId2 = subscription.TransactionId;

		transId2.AssertNotEqual(transId1, "Second cycle must use new TransactionId");

		manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = transId2 },
			out _, out _, out _);
		manager.ProcessSubscriptionOnlineMessage(
			new SubscriptionOnlineMessage { OriginalTransactionId = transId2 },
			out _);

		subscription.State.AssertEqual(SubscriptionStates.Online);

		// Data should be found for new subscription ID
		manager.GetSubscriptions(CreateDataMessage(transId2)).Count()
			.AssertEqual(1, "Subscription should be active in cycle 2");

		// Old ID should still not work
		manager.GetSubscriptions(CreateDataMessage(transId1)).Count()
			.AssertEqual(0, "Old ID from cycle 1 should still not work");
	}

	[TestMethod]
	public void Resubscribe_WithoutUnsubscribeResponse_ShouldCleanupOldEntry()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();

		// Subscribe and go online
		var transId1 = SubscribeAndGoOnline(manager, subscription);

		// Call Subscribe again WITHOUT unsubscribing first
		// This simulates reconnect or user re-subscribing
		manager.Subscribe(subscription);
		var transId2 = subscription.TransactionId;

		transId2.AssertNotEqual(transId1,
			"Re-subscribe without unsubscribe must still generate new TransactionId");

		// Manager should have exactly 1 entry for this subscription
		// (old entry should be cleaned up or replaced)
		var count = manager.Subscriptions.Count(s => ReferenceEquals(s, subscription));
		count.AssertEqual(1,
			"Manager should have exactly one entry for re-subscribed subscription object");
	}

	[TestMethod]
	public void Subscribe_SameObjectTwice_ShouldNotThrow()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();

		manager.Subscribe(subscription);

		// Second Subscribe with same object must not crash
		manager.Subscribe(subscription);

		// Should have valid state
		subscription.TransactionId.AssertNotEqual(0);
	}

	#endregion

	#region Duplicate Response / Out-of-Order Messages

	[TestMethod]
	public void ProcessResponse_SuccessTwice_SecondShouldBeHandledGracefully()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		manager.Subscribe(subscription);
		var transId = subscription.TransactionId;

		// First success response
		var first = manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = transId },
			out _, out _, out _);
		IsNotNull(first);
		subscription.State.AssertEqual(SubscriptionStates.Active);

		// Second success response (duplicate)
		var second = manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = transId },
			out _, out _, out _);

		// Should either return subscription (idempotent) or null (already processed)
		// But should NOT crash or corrupt state
		subscription.State.AssertEqual(SubscriptionStates.Active,
			"State should remain Active after duplicate response");
	}

	[TestMethod]
	public void ProcessOnline_BeforeResponse_ShouldHandleGracefully()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		manager.Subscribe(subscription);
		var transId = subscription.TransactionId;

		// Online arrives BEFORE SubscriptionResponse
		var result = manager.ProcessSubscriptionOnlineMessage(
			new SubscriptionOnlineMessage { OriginalTransactionId = transId },
			out _);

		// Stopped→Online is valid per StateValidator
		if (result != null)
		{
			subscription.State.AssertEqual(SubscriptionStates.Online,
				"Online before Response: state should be Online");
		}
	}

	[TestMethod]
	public void ProcessFinished_BeforeResponse_ShouldHandleGracefully()
	{
		var manager = CreateManager();
		var subscription = CreateTickSubscription();
		manager.Subscribe(subscription);
		var transId = subscription.TransactionId;

		// Finished arrives BEFORE SubscriptionResponse
		var result = manager.ProcessSubscriptionFinishedMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = transId },
			out _);

		if (result != null)
		{
			subscription.State.AssertEqual(SubscriptionStates.Finished);

			// Subscription removed — late response should return null
			var lateResponse = manager.ProcessResponse(
				new SubscriptionResponseMessage { OriginalTransactionId = transId },
				out _, out _, out _);

			lateResponse.AssertNull("Late response after early Finished should return null");
		}
	}

	#endregion

	#region OrderStatus Special Handling

	[TestMethod]
	public void Subscribe_OrderStatus_ProducesAddOrderStatusAction()
	{
		var manager = CreateManager();
		var subscription = CreateOrderStatusSubscription();

		var actions = manager.Subscribe(subscription);

		// Should have both SendInMessage and AddOrderStatus actions
		actions.Items.Count(i => i.Type == ConnectorSubscriptionManager.Actions.Item.Types.SendInMessage)
			.AssertEqual(1, "Should produce SendInMessage");
		actions.Items.Count(i => i.Type == ConnectorSubscriptionManager.Actions.Item.Types.AddOrderStatus)
			.AssertEqual(1, "Should produce AddOrderStatus action");
	}

	[TestMethod]
	public void Unsubscribe_OrderStatus_ProducesRemoveOrderStatusAction()
	{
		var manager = CreateManager();
		var subscription = CreateOrderStatusSubscription();
		SubscribeAndActivate(manager, subscription);

		var actions = manager.UnSubscribe(subscription);

		actions.Items.Count(i => i.Type == ConnectorSubscriptionManager.Actions.Item.Types.RemoveOrderStatus)
			.AssertEqual(1, "Should produce RemoveOrderStatus action");
	}

	#endregion

	#region Multiple Subscriptions

	[TestMethod]
	public void GetSubscriptions_MultipleActive_ReturnsAll()
	{
		var manager = CreateManager();

		var sub1 = CreateTickSubscription();
		var sub2 = CreateTickSubscription();

		var transId1 = SubscribeAndGoOnline(manager, sub1);
		var transId2 = SubscribeAndGoOnline(manager, sub2);

		// Message with both subscription IDs
		var subs = manager.GetSubscriptions(CreateDataMessage(transId1, transId2)).ToArray();

		subs.Length.AssertEqual(2, "Should return both active subscriptions");
	}

	[TestMethod]
	public void GetSubscriptions_OneErroredOneActive_ReturnsOnlyActive()
	{
		var manager = CreateManager();

		var sub1 = CreateTickSubscription();
		var sub2 = CreateTickSubscription();

		var transId1 = SubscribeAndActivate(manager, sub1);
		manager.Subscribe(sub2);
		var transId2 = sub2.TransactionId;

		// Error on sub2
		manager.ProcessResponse(
			new SubscriptionResponseMessage
			{
				OriginalTransactionId = transId2,
				Error = new InvalidOperationException("fail"),
			},
			out _, out _, out _);

		var subs = manager.GetSubscriptions(CreateDataMessage(transId1, transId2)).ToArray();

		subs.Length.AssertEqual(1, "Should return only the active subscription");
		subs[0].TransactionId.AssertEqual(transId1);
	}

	[TestMethod]
	public void GetSubscriptions_OneFinishedOneOnline_ReturnsOnlyOnline()
	{
		var manager = CreateManager();

		var sub1 = CreateTickSubscription();
		var sub2 = CreateTickSubscription();

		var transId1 = SubscribeAndGoOnline(manager, sub1);
		var transId2 = SubscribeAndActivate(manager, sub2);

		// Finished on sub2
		manager.ProcessSubscriptionFinishedMessage(
			new SubscriptionFinishedMessage { OriginalTransactionId = transId2 },
			out _);

		var subs = manager.GetSubscriptions(CreateDataMessage(transId1, transId2)).ToArray();

		subs.Length.AssertEqual(1, "Should return only the online subscription");
		subs[0].TransactionId.AssertEqual(transId1);
	}

	[TestMethod]
	public void GetSubscriptions_MultipleIds_SameSecurity_ReturnsAll()
	{
		var manager = CreateManager();

		// Two subscriptions to same security+datatype (simulates what happens after join)
		var secId = Helper.CreateSecurityId();
		var sub1 = new Subscription(new MarketDataMessage
		{
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});
		var sub2 = new Subscription(new MarketDataMessage
		{
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});

		var transId1 = SubscribeAndGoOnline(manager, sub1);
		var transId2 = SubscribeAndGoOnline(manager, sub2);

		// Data message with both IDs (as if SubscriptionOnlineManager joined them)
		var subs = manager.GetSubscriptions(CreateDataMessage(transId1, transId2)).ToArray();

		subs.Length.AssertEqual(2, "Should return both subscriptions for joined message");
		subs.Count(s => s.TransactionId == transId1).AssertEqual(1, "Should contain first subscription");
		subs.Count(s => s.TransactionId == transId2).AssertEqual(1, "Should contain second subscription");
	}

	[TestMethod]
	public void GetSubscriptions_MultipleIds_OneUnsubscribed_ReturnsOnlyActive()
	{
		var manager = CreateManager();

		var secId = Helper.CreateSecurityId();
		var sub1 = new Subscription(new MarketDataMessage
		{
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});
		var sub2 = new Subscription(new MarketDataMessage
		{
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		});

		var transId1 = SubscribeAndGoOnline(manager, sub1);
		var transId2 = SubscribeAndGoOnline(manager, sub2);

		// Unsubscribe sub1
		var unsubActions = manager.UnSubscribe(sub1);
		var unsubMsg = (MarketDataMessage)unsubActions.Items[0].Message;
		manager.ProcessResponse(
			new SubscriptionResponseMessage { OriginalTransactionId = unsubMsg.TransactionId },
			out _, out _, out _);

		// Data still arrives with both IDs (adapter doesn't know about unsub yet)
		var subs = manager.GetSubscriptions(CreateDataMessage(transId1, transId2)).ToArray();

		subs.Length.AssertEqual(1, "Should return only the active subscription");
		subs[0].TransactionId.AssertEqual(transId2);
	}

	[TestMethod]
	public void GetSubscriptions_MultipleIds_SomeUnknown_ReturnsOnlyKnown()
	{
		var manager = CreateManager();

		var sub = CreateTickSubscription();
		var transId = SubscribeAndGoOnline(manager, sub);

		// Data with known + unknown IDs
		var subs = manager.GetSubscriptions(CreateDataMessage(transId, 9999, 8888)).ToArray();

		subs.Length.AssertEqual(1, "Should return only the known subscription");
		subs[0].TransactionId.AssertEqual(transId);
	}

	[TestMethod]
	public void GetSubscriptions_MultipleIds_AllUnknown_ReturnsEmpty()
	{
		var manager = CreateManager();

		var subs = manager.GetSubscriptions(CreateDataMessage(9999, 8888, 7777)).ToArray();

		subs.Length.AssertEqual(0, "Should return empty for all unknown IDs");
	}

	#endregion

	#region GetSubscribers

	[TestMethod]
	public void GetSubscribers_ReturnsSecurityIdsOfActiveSubscriptions()
	{
		var manager = CreateManager();

		var sub = CreateTickSubscription();
		SubscribeAndActivate(manager, sub);

		var secIds = manager.GetSubscribers(DataType.Ticks).ToArray();
		secIds.Length.AssertEqual(1, "Should return security IDs for active tick subscriptions");
	}

	[TestMethod]
	public void GetSubscribers_AfterError_ReturnsEmpty()
	{
		var manager = CreateManager();

		var sub = CreateTickSubscription();
		manager.Subscribe(sub);
		var transId = sub.TransactionId;

		// Error
		manager.ProcessResponse(
			new SubscriptionResponseMessage
			{
				OriginalTransactionId = transId,
				Error = new InvalidOperationException("fail"),
			},
			out _, out _, out _);

		var secIds = manager.GetSubscribers(DataType.Ticks).ToArray();
		secIds.Length.AssertEqual(0, "No subscribers after error");
	}

	#endregion

	#region ClearCache

	[TestMethod]
	public void ClearCache_RemovesAllSubscriptions()
	{
		var manager = CreateManager();

		var sub1 = CreateTickSubscription();
		var sub2 = CreateTickSubscription();
		SubscribeAndActivate(manager, sub1);
		SubscribeAndActivate(manager, sub2);

		manager.Subscriptions.Count().AssertEqual(2);

		manager.ClearCache();

		manager.Subscriptions.Count().AssertEqual(0, "ClearCache should remove all subscriptions");
	}

	[TestMethod]
	public void GetSubscriptions_AfterClearCache_ReturnsEmpty()
	{
		var manager = CreateManager();

		var sub = CreateTickSubscription();
		var transId = SubscribeAndActivate(manager, sub);

		manager.ClearCache();

		var subs = manager.GetSubscriptions(CreateDataMessage(transId)).ToArray();
		subs.Length.AssertEqual(0, "No subscriptions after ClearCache");
	}

	#endregion
}
