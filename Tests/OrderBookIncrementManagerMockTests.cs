namespace StockSharp.Tests;

[TestClass]
public class OrderBookIncrementManagerMockTests : BaseTestClass
{
	private static readonly SecurityId _secId = Helper.CreateSecurityId();

	private sealed class TestReceiver : TestLogReceiver { }

	private static OrderBookIncrementManager CreateManager(Mock<IOrderBookIncrementManagerState> stateMock)
	{
		return new OrderBookIncrementManager(new TestReceiver(), stateMock.Object);
	}

	[TestMethod]
	public void Reset_ClearsState()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		var mgr = CreateManager(stateMock);

		var (toInner, toOut) = mgr.ProcessInMessage(new ResetMessage());

		stateMock.Verify(s => s.Clear(), Times.Once);
		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Subscribe_MarketDepth_WithSecurity_AddsSubscription()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = _secId,
			TransactionId = 1,
		});

		stateMock.Verify(s => s.AddSubscription(1, _secId, It.IsAny<ILogReceiver>()), Times.Once);
	}

	[TestMethod]
	public void Subscribe_MarketDepth_DoNotBuild_AddsPassThrough()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = _secId,
			TransactionId = 1,
			DoNotBuildOrderBookIncrement = true,
		});

		stateMock.Verify(s => s.AddPassThrough(1), Times.Once);
		stateMock.Verify(s => s.AddSubscription(It.IsAny<long>(), It.IsAny<SecurityId>(), It.IsAny<ILogReceiver>()), Times.Never);
	}

	[TestMethod]
	public void Subscribe_MarketDepth_DefaultSecurity_AddsAllSecSubscription()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			TransactionId = 1,
		});

		stateMock.Verify(s => s.AddAllSecSubscription(1), Times.Once);
	}

	[TestMethod]
	public void Subscribe_MarketDepth_DefaultSecurity_DoNotBuild_AddsAllSecPassThrough()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			TransactionId = 1,
			DoNotBuildOrderBookIncrement = true,
		});

		stateMock.Verify(s => s.AddAllSecPassThrough(1), Times.Once);
	}

	[TestMethod]
	public void Subscribe_NotMarketDepth_NoStateChanges()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			SecurityId = _secId,
			TransactionId = 1,
		});

		stateMock.Verify(s => s.AddSubscription(It.IsAny<long>(), It.IsAny<SecurityId>(), It.IsAny<ILogReceiver>()), Times.Never);
		stateMock.Verify(s => s.AddPassThrough(It.IsAny<long>()), Times.Never);
		stateMock.Verify(s => s.AddAllSecSubscription(It.IsAny<long>()), Times.Never);
	}

	[TestMethod]
	public void Unsubscribe_RemovesSubscription()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = false,
			OriginalTransactionId = 5,
		});

		stateMock.Verify(s => s.RemoveSubscription(5), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_SubscriptionResponseError_RemovesSubscription()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 5,
			Error = new InvalidOperationException("fail"),
		});

		stateMock.Verify(s => s.RemoveSubscription(5), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_SubscriptionFinished_RemovesSubscription()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = 5 });

		stateMock.Verify(s => s.RemoveSubscription(5), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_SubscriptionOnline_CallsOnSubscriptionOnline()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = 5 });

		stateMock.Verify(s => s.OnSubscriptionOnline(5), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_QuoteChange_NoSubscriptions_PassThrough()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		stateMock.Setup(s => s.HasAnySubscriptions).Returns(false);
		var mgr = CreateManager(stateMock);

		var quote = new QuoteChangeMessage
		{
			SecurityId = _secId,
			State = QuoteChangeStates.Increment,
			Bids = [],
			Asks = [],
			ServerTime = DateTime.UtcNow,
		};

		var (forward, extraOut) = mgr.ProcessOutMessage(quote);

		forward.AssertNotNull();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOut_QuoteChange_NoState_PassThrough()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		stateMock.Setup(s => s.HasAnySubscriptions).Returns(true);
		var mgr = CreateManager(stateMock);

		var quote = new QuoteChangeMessage
		{
			SecurityId = _secId,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
			ServerTime = DateTime.UtcNow,
		};

		var (forward, extraOut) = mgr.ProcessOutMessage(quote);

		forward.AssertNotNull();
		extraOut.Length.AssertEqual(0);
		stateMock.Verify(s => s.TryApply(It.IsAny<long>(), It.IsAny<QuoteChangeMessage>(), out It.Ref<long[]>.IsAny), Times.Never);
	}

	[TestMethod]
	public void ProcessOut_QuoteChange_Increment_CallsTryApply()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		stateMock.Setup(s => s.HasAnySubscriptions).Returns(true);

		var builtQuote = new QuoteChangeMessage
		{
			SecurityId = _secId,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
			ServerTime = DateTime.UtcNow,
		};
		var ids = new long[] { 1 };
		stateMock.Setup(s => s.TryApply(1, It.IsAny<QuoteChangeMessage>(), out ids))
			.Returns(builtQuote);

		var mgr = CreateManager(stateMock);

		var quote = new QuoteChangeMessage
		{
			SecurityId = _secId,
			State = QuoteChangeStates.Increment,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
			ServerTime = DateTime.UtcNow,
		};
		quote.SetSubscriptionIds([1]);

		var (forward, extraOut) = mgr.ProcessOutMessage(quote);

		stateMock.Verify(s => s.TryApply(1, It.IsAny<QuoteChangeMessage>(), out It.Ref<long[]>.IsAny), Times.Once);
		extraOut.Length.AssertEqual(1);
		// original should be null (no pass-through ids)
		forward.AssertNull();
	}

	[TestMethod]
	public void ProcessOut_QuoteChange_PassThrough_ForwardsOriginal()
	{
		var stateMock = new Mock<IOrderBookIncrementManagerState>();
		stateMock.Setup(s => s.HasAnySubscriptions).Returns(true);

		var ids = default(long[]);
		stateMock.Setup(s => s.TryApply(1, It.IsAny<QuoteChangeMessage>(), out ids))
			.Returns((QuoteChangeMessage)null);
		stateMock.Setup(s => s.IsPassThrough(1)).Returns(true);

		var mgr = CreateManager(stateMock);

		var quote = new QuoteChangeMessage
		{
			SecurityId = _secId,
			State = QuoteChangeStates.Increment,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
			ServerTime = DateTime.UtcNow,
		};
		quote.SetSubscriptionIds([1]);

		var (forward, extraOut) = mgr.ProcessOutMessage(quote);

		forward.AssertNotNull();
		extraOut.Length.AssertEqual(0);
	}
}
