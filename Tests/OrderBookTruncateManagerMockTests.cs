namespace StockSharp.Tests;

[TestClass]
public class OrderBookTruncateManagerMockTests : BaseTestClass
{
	private static readonly SecurityId _secId = Helper.CreateSecurityId();

	private sealed class TestReceiver : TestLogReceiver { }

	private static OrderBookTruncateManager CreateManager(Mock<IOrderBookTruncateManagerState> stateMock, Func<int, int?> nearestDepth = null)
	{
		return new OrderBookTruncateManager(
			new TestReceiver(),
			nearestDepth ?? (d => d),
			stateMock.Object);
	}

	[TestMethod]
	public void Reset_ClearsState()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		var mgr = CreateManager(stateMock);

		var (toInner, toOut) = mgr.ProcessInMessage(new ResetMessage());

		stateMock.Verify(s => s.Clear(), Times.Once);
		toInner.AssertNotNull();
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Subscribe_DepthMismatch_AddsDepth()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		// nearest supported depth is 20, but requested 10
		var mgr = CreateManager(stateMock, d => d == 10 ? 20 : d);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = _secId,
			TransactionId = 1,
			MaxDepth = 10,
		};

		var (toInner, _) = mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddDepth(1, 10), Times.Once);
		// message should be modified to use supported depth
		var outMd = (MarketDataMessage)toInner;
		outMd.MaxDepth.AssertEqual(20);
	}

	[TestMethod]
	public void Subscribe_DepthMatch_NoAddDepth()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		// nearest is same as requested
		var mgr = CreateManager(stateMock, d => d);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = _secId,
			TransactionId = 1,
			MaxDepth = 10,
		};

		mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddDepth(It.IsAny<long>(), It.IsAny<int>()), Times.Never);
	}

	[TestMethod]
	public void Subscribe_NoMaxDepth_NoAddDepth()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		var mgr = CreateManager(stateMock);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = _secId,
			TransactionId = 1,
		};

		mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddDepth(It.IsAny<long>(), It.IsAny<int>()), Times.Never);
	}

	[TestMethod]
	public void Subscribe_DefaultSecurityId_PassThrough()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		var mgr = CreateManager(stateMock, d => d + 10);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			TransactionId = 1,
			MaxDepth = 10,
		};

		mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddDepth(It.IsAny<long>(), It.IsAny<int>()), Times.Never);
	}

	[TestMethod]
	public void Subscribe_DoNotBuild_PassThrough()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		var mgr = CreateManager(stateMock, d => d + 10);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = _secId,
			TransactionId = 1,
			MaxDepth = 10,
			DoNotBuildOrderBookIncrement = true,
		};

		mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddDepth(It.IsAny<long>(), It.IsAny<int>()), Times.Never);
	}

	[TestMethod]
	public void Unsubscribe_RemovesDepth()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		stateMock.Setup(s => s.RemoveDepth(5)).Returns(true);
		var mgr = CreateManager(stateMock);

		mgr.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = false,
			DataType2 = DataType.MarketDepth,
			OriginalTransactionId = 5,
		});

		stateMock.Verify(s => s.RemoveDepth(5), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_SubscriptionResponseError_RemovesDepth()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		stateMock.Setup(s => s.RemoveDepth(5)).Returns(true);
		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 5,
			Error = new InvalidOperationException("fail"),
		});

		stateMock.Verify(s => s.RemoveDepth(5), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_SubscriptionResponseOk_DoesNotRemove()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 5,
		});

		stateMock.Verify(s => s.RemoveDepth(It.IsAny<long>()), Times.Never);
	}

	[TestMethod]
	public void ProcessOut_SubscriptionFinished_RemovesDepth()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		stateMock.Setup(s => s.RemoveDepth(5)).Returns(true);
		var mgr = CreateManager(stateMock);

		mgr.ProcessOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = 5 });

		stateMock.Verify(s => s.RemoveDepth(5), Times.Once);
	}

	[TestMethod]
	public void ProcessOut_QuoteChange_NoDepths_PassThrough()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		stateMock.Setup(s => s.HasDepths).Returns(false);
		var mgr = CreateManager(stateMock);

		var quote = new QuoteChangeMessage
		{
			SecurityId = _secId,
			Bids = Enumerable.Range(0, 20).Select(i => new QuoteChange(100m - i, 10)).ToArray(),
			Asks = Enumerable.Range(0, 20).Select(i => new QuoteChange(101m + i, 10)).ToArray(),
			ServerTime = DateTime.UtcNow,
		};

		var (forward, extraOut) = mgr.ProcessOutMessage(quote);

		forward.AssertNotNull();
		extraOut.Length.AssertEqual(0);
		stateMock.Verify(s => s.GroupByDepth(It.IsAny<long[]>()), Times.Never);
	}

	[TestMethod]
	public void ProcessOut_QuoteChange_WithDepths_GroupsAndTruncates()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		stateMock.Setup(s => s.HasDepths).Returns(true);
		stateMock.Setup(s => s.GroupByDepth(It.IsAny<long[]>()))
			.Returns(new[] { (depth: (int?)5, ids: new long[] { 1 }) });
		var mgr = CreateManager(stateMock);

		var quote = new QuoteChangeMessage
		{
			SecurityId = _secId,
			Bids = Enumerable.Range(0, 20).Select(i => new QuoteChange(100m - i, 10)).ToArray(),
			Asks = Enumerable.Range(0, 20).Select(i => new QuoteChange(101m + i, 10)).ToArray(),
			ServerTime = DateTime.UtcNow,
		};
		quote.SetSubscriptionIds([1]);

		var (forward, extraOut) = mgr.ProcessOutMessage(quote);

		stateMock.Verify(s => s.GroupByDepth(It.IsAny<long[]>()), Times.Once);
		extraOut.Length.AssertEqual(1);
		var truncated = (QuoteChangeMessage)extraOut[0];
		truncated.Bids.Length.AssertEqual(5);
		truncated.Asks.Length.AssertEqual(5);
	}

	[TestMethod]
	public void ProcessOut_QuoteChange_WithState_SkipsProcessing()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		stateMock.Setup(s => s.HasDepths).Returns(true);
		var mgr = CreateManager(stateMock);

		var quote = new QuoteChangeMessage
		{
			SecurityId = _secId,
			State = QuoteChangeStates.SnapshotComplete,
			Bids = [],
			Asks = [],
			ServerTime = DateTime.UtcNow,
		};

		var (forward, extraOut) = mgr.ProcessOutMessage(quote);

		forward.AssertNotNull();
		extraOut.Length.AssertEqual(0);
		stateMock.Verify(s => s.GroupByDepth(It.IsAny<long[]>()), Times.Never);
	}

	[TestMethod]
	public void Subscribe_NoSupportedDepth_StillAddsDepth()
	{
		var stateMock = new Mock<IOrderBookTruncateManagerState>();
		// nearestSupportedDepth returns null (no supported depths)
		var mgr = CreateManager(stateMock, d => (int?)null);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = _secId,
			TransactionId = 1,
			MaxDepth = 10,
		};

		mgr.ProcessInMessage(mdMsg);

		stateMock.Verify(s => s.AddDepth(1, 10), Times.Once);
	}
}
