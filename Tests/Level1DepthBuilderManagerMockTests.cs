namespace StockSharp.Tests;

[TestClass]
public class Level1DepthBuilderManagerMockTests : BaseTestClass
{
	private static readonly SecurityId _secId = Helper.CreateSecurityId();

	private sealed class TestReceiver : TestLogReceiver { }

	private static Level1DepthBuilderManager CreateManager(Mock<ILevel1DepthBuilderManagerState> stateMock)
	{
		return new Level1DepthBuilderManager(new TestReceiver(), stateMock.Object);
	}

	[TestMethod]
	public void Reset_ClearsState()
	{
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
		var mgr = CreateManager(stateMock);

		var (toInner, toOut) = mgr.ProcessInMessage(new ResetMessage());

		stateMock.Verify(s => s.Clear(), Times.Once);
		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Subscribe_MarketDepth_AddsSubscription()
	{
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
		var mgr = CreateManager(stateMock);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = _secId,
			TransactionId = 1,
		};

		var (toInner, _) = mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddSubscription(1, _secId), Times.Once);
		// message should be modified to Level1
		var outMd = (MarketDataMessage)toInner[0];
		outMd.DataType2.AssertEqual(DataType.Level1);
	}

	[TestMethod]
	public void Subscribe_MarketDepth_BuildModeLoad_PassThrough()
	{
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
		var mgr = CreateManager(stateMock);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = _secId,
			TransactionId = 1,
			BuildMode = MarketDataBuildModes.Load,
		};

		mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddSubscription(It.IsAny<long>(), It.IsAny<SecurityId>()), Times.Never);
	}

	[TestMethod]
	public void Subscribe_MarketDepth_BuildFromNotLevel1_PassThrough()
	{
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
		var mgr = CreateManager(stateMock);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = _secId,
			TransactionId = 1,
			BuildFrom = DataType.OrderLog,
		};

		mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddSubscription(It.IsAny<long>(), It.IsAny<SecurityId>()), Times.Never);
	}

	[TestMethod]
	public void Subscribe_DefaultSecurityId_PassThrough()
	{
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
		var mgr = CreateManager(stateMock);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			TransactionId = 1,
		};

		mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddSubscription(It.IsAny<long>(), It.IsAny<SecurityId>()), Times.Never);
	}

	[TestMethod]
	public void Subscribe_NotMarketDepth_PassThrough()
	{
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
		var mgr = CreateManager(stateMock);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			SecurityId = _secId,
			TransactionId = 1,
		};

		mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddSubscription(It.IsAny<long>(), It.IsAny<SecurityId>()), Times.Never);
	}

	[TestMethod]
	public void Unsubscribe_RemovesSubscription()
	{
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
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
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
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
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = 5 });

		stateMock.Verify(s => s.RemoveSubscription(5), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_SubscriptionOnline_CallsOnSubscriptionOnline()
	{
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = 5 });

		stateMock.Verify(s => s.OnSubscriptionOnline(5), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_Level1_NoSubscriptions_PassThrough()
	{
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
		stateMock.Setup(s => s.HasAnySubscriptions).Returns(false);
		var mgr = CreateManager(stateMock);

		var l1 = new Level1ChangeMessage { SecurityId = _secId, ServerTime = DateTime.UtcNow };
		l1.Add(Level1Fields.BestBidPrice, 100m);

		var (forward, extraOut) = mgr.ProcessOutMessage(l1);

		forward.AssertNotNull();
		extraOut.Length.AssertEqual(0);
		stateMock.Verify(s => s.TryBuildDepth(It.IsAny<long>(), It.IsAny<Level1ChangeMessage>(), out It.Ref<long[]>.IsAny), Times.Never);
	}

	[TestMethod]
	public void ProcessOut_Level1_WithSubscription_CallsTryBuildDepth()
	{
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
		stateMock.Setup(s => s.HasAnySubscriptions).Returns(true);

		var builtQuote = new QuoteChangeMessage
		{
			SecurityId = _secId,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
			ServerTime = DateTime.UtcNow,
		};
		var subIds = new long[] { 1 };
		stateMock.Setup(s => s.TryBuildDepth(1, It.IsAny<Level1ChangeMessage>(), out subIds))
			.Returns(builtQuote);

		var mgr = CreateManager(stateMock);

		var l1 = new Level1ChangeMessage { SecurityId = _secId, ServerTime = DateTime.UtcNow };
		l1.Add(Level1Fields.BestBidPrice, 100m);
		l1.SetSubscriptionIds([1]);

		var (forward, extraOut) = mgr.ProcessOutMessage(l1);

		stateMock.Verify(s => s.TryBuildDepth(1, It.IsAny<Level1ChangeMessage>(), out It.Ref<long[]>.IsAny), Times.Once);
		extraOut.Length.AssertEqual(1);
		((QuoteChangeMessage)extraOut[0]).SecurityId.AssertEqual(_secId);
		// original L1 consumed when all ids are used
		forward.AssertNull();
	}

	[TestMethod]
	public void Subscribe_BuildFromLevel1_AddsSubscription()
	{
		var stateMock = new Mock<ILevel1DepthBuilderManagerState>();
		var mgr = CreateManager(stateMock);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = _secId,
			TransactionId = 1,
			BuildFrom = DataType.Level1,
		};

		var (toInner, _) = mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddSubscription(1, _secId), Times.Once);
		((MarketDataMessage)toInner[0]).DataType2.AssertEqual(DataType.Level1);
	}
}
