namespace StockSharp.Tests;

[TestClass]
public class SubscriptionManagerMockTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver { }

	private static SubscriptionManager CreateManager(Mock<ISubscriptionManagerState> stateMock)
	{
		return new SubscriptionManager(
			new TestReceiver(),
			new IncrementalIdGenerator(),
			() => new ProcessSuspendedMessage(),
			stateMock.Object);
	}

	[TestMethod]
	public void Reset_ClearsState()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		var mgr = CreateManager(stateMock);

		var (toInner, toOut) = mgr.ProcessInMessage(new ResetMessage());

		stateMock.Verify(s => s.Clear(), Times.Once);
		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Subscribe_AddsSubscription()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		stateMock.Setup(s => s.ContainsReplaceId(It.IsAny<long>())).Returns(false);
		var mgr = CreateManager(stateMock);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			SecurityId = Helper.CreateSecurityId(),
			TransactionId = 1,
		};

		var (toInner, toOut) = mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddSubscription(1, It.IsAny<ISubscriptionMessage>(), SubscriptionStates.Stopped), Times.Once);
		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Subscribe_HistoryOnly_AddsHistoricalRequest()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		stateMock.Setup(s => s.ContainsReplaceId(It.IsAny<long>())).Returns(false);
		var mgr = CreateManager(stateMock);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			SecurityId = Helper.CreateSecurityId(),
			TransactionId = 1,
			From = DateTime.UtcNow.AddDays(-1),
			To = DateTime.UtcNow.AddHours(-1),
		};

		var (toInner, toOut) = mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddHistoricalRequest(1, It.IsAny<ISubscriptionMessage>()), Times.Once);
		stateMock.Verify(s => s.AddSubscription(It.IsAny<long>(), It.IsAny<ISubscriptionMessage>(), It.IsAny<SubscriptionStates>()), Times.Never);
		toInner.Length.AssertEqual(1);
	}

	[TestMethod]
	public void Subscribe_SpecificItemRequest_PassesThroughWithoutState()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		var mgr = CreateManager(stateMock);

		// Use OrderStatusMessage which has SpecificItemRequest = true when HasOrderId()
		var orderMsg = new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			OrderId = 123, // makes SpecificItemRequest = true
		};

		var (toInner, toOut) = mgr.ProcessInMessage(orderMsg);

		stateMock.Verify(s => s.AddSubscription(It.IsAny<long>(), It.IsAny<ISubscriptionMessage>(), It.IsAny<SubscriptionStates>()), Times.Never);
		stateMock.Verify(s => s.AddHistoricalRequest(It.IsAny<long>(), It.IsAny<ISubscriptionMessage>()), Times.Never);
		toInner.Length.AssertEqual(1);
	}

	[TestMethod]
	public void Unsubscribe_ExistingSubscription_UpdatesState()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		var subscription = (ISubscriptionMessage)new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
		};
		var state = SubscriptionStates.Active;
		stateMock.Setup(s => s.TryGetSubscription(1, out subscription, out state)).Returns(true);
		stateMock.Setup(s => s.TryGetNewId(1, out It.Ref<long>.IsAny)).Returns(false);

		var mgr = CreateManager(stateMock);

		var unsubMsg = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 100,
			OriginalTransactionId = 1,
		};

		var (toInner, toOut) = mgr.ProcessInMessage(unsubMsg);

		stateMock.Verify(s => s.UpdateSubscriptionState(1, It.IsAny<SubscriptionStates>()), Times.Once);
		toInner.Length.AssertEqual(1);
	}

	[TestMethod]
	public void Unsubscribe_NonExistent_ReturnsError()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		var sub = default(ISubscriptionMessage);
		var st = default(SubscriptionStates);
		stateMock.Setup(s => s.TryGetSubscription(1, out sub, out st)).Returns(false);
		stateMock.Setup(s => s.TryGetAndRemoveHistoricalRequest(1, out sub)).Returns(false);

		var mgr = CreateManager(stateMock);

		var unsubMsg = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 100,
			OriginalTransactionId = 1,
		};

		var (toInner, toOut) = mgr.ProcessInMessage(unsubMsg);

		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(1);
		toOut[0].Type.AssertEqual(MessageTypes.SubscriptionResponse);
	}

	[TestMethod]
	public void ProcessOut_SubscriptionResponseOk_UpdatesStateToActive()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		var subscription = (ISubscriptionMessage)new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
		};
		var state = SubscriptionStates.Stopped;
		stateMock.Setup(s => s.TryGetSubscription(1, out subscription, out state)).Returns(true);
		stateMock.Setup(s => s.TryGetOriginalId(It.IsAny<long>(), out It.Ref<long>.IsAny)).Returns(false);
		stateMock.Setup(s => s.ContainsReplaceId(It.IsAny<long>())).Returns(false);

		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 1 });

		stateMock.Verify(s => s.UpdateSubscriptionState(1, SubscriptionStates.Active), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_SubscriptionResponseError_RemovesSubscription()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		var subscription = (ISubscriptionMessage)new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
		};
		var state = SubscriptionStates.Stopped;
		stateMock.Setup(s => s.TryGetSubscription(1, out subscription, out state)).Returns(true);
		stateMock.Setup(s => s.TryGetOriginalId(It.IsAny<long>(), out It.Ref<long>.IsAny)).Returns(false);
		stateMock.Setup(s => s.RemoveHistoricalRequest(1)).Returns(false);

		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("fail"),
		});

		stateMock.Verify(s => s.UpdateSubscriptionState(1, SubscriptionStates.Error), Times.Once);
		stateMock.Verify(s => s.RemoveSubscription(1), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_SubscriptionOnline_UpdatesStateToOnline()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		var subscription = (ISubscriptionMessage)new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
		};
		var state = SubscriptionStates.Active;
		stateMock.Setup(s => s.TryGetSubscription(1, out subscription, out state)).Returns(true);
		stateMock.Setup(s => s.TryGetOriginalId(It.IsAny<long>(), out It.Ref<long>.IsAny)).Returns(false);
		stateMock.Setup(s => s.ContainsReplaceId(It.IsAny<long>())).Returns(false);

		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = 1 });

		stateMock.Verify(s => s.UpdateSubscriptionState(1, SubscriptionStates.Online), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_SubscriptionFinished_RemovesHistoricalRequest()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		stateMock.Setup(s => s.TryGetOriginalId(It.IsAny<long>(), out It.Ref<long>.IsAny)).Returns(false);
		stateMock.Setup(s => s.ContainsReplaceId(It.IsAny<long>())).Returns(false);

		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = 1 });

		stateMock.Verify(s => s.RemoveHistoricalRequest(1), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_ConnectionRestored_ReMapSubscriptions()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		stateMock.Setup(s => s.TryGetOriginalId(It.IsAny<long>(), out It.Ref<long>.IsAny)).Returns(false);
		var mdMsg = (ISubscriptionMessage)new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
		};
		stateMock.Setup(s => s.GetActiveSubscriptions())
			.Returns([(1L, mdMsg)]);
		stateMock.Setup(s => s.ReMapSubscriptionCount).Returns(1);

		var mgr = CreateManager(stateMock);

		var (forward, extraOut) = mgr.ProcessOutMessage(new ConnectionRestoredMessage { IsResetState = true });

		stateMock.Verify(s => s.ClearReplaceIds(), Times.Once);
		stateMock.Verify(s => s.ClearReMapSubscriptions(), Times.Once);
		stateMock.Verify(s => s.AddReplaceId(It.IsAny<long>(), 1), Times.Once);
		stateMock.Verify(s => s.AddReMapSubscription(It.IsAny<Message>()), Times.Once);
		forward.AssertNotNull();
		extraOut.Length.AssertEqual(1);
		extraOut[0].Type.AssertEqual(MessageTypes.ProcessSuspended);
	}

	[TestMethod]
	public void ProcessSuspended_GetsAndClearsReMapSubscriptions()
	{
		var stateMock = new Mock<ISubscriptionManagerState>();
		var reMaps = new Message[]
		{
			new MarketDataMessage { TransactionId = 10 },
			new MarketDataMessage { TransactionId = 20 },
		};
		stateMock.Setup(s => s.GetAndClearReMapSubscriptions()).Returns(reMaps);

		var mgr = CreateManager(stateMock);

		var (toInner, toOut) = mgr.ProcessInMessage(new ProcessSuspendedMessage());

		stateMock.Verify(s => s.GetAndClearReMapSubscriptions(), Times.Once);
		toInner.Length.AssertEqual(2);
		toOut.Length.AssertEqual(0);
	}

}
