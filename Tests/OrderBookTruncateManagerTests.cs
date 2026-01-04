namespace StockSharp.Tests;

[TestClass]
public class OrderBookTruncateManagerTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	private static QuoteChangeMessage CreateSnapshot(SecurityId securityId, DateTime time, long[] subscriptionIds, int depth)
	{
		var bids = new QuoteChange[depth];
		var asks = new QuoteChange[depth];

		for (var i = 0; i < depth; i++)
		{
			bids[i] = new QuoteChange(100m - i, i + 1);
			asks[i] = new QuoteChange(101m + i, i + 1);
		}

		var msg = new QuoteChangeMessage
		{
			SecurityId = securityId,
			ServerTime = time,
			LocalTime = time,
			State = null,
			Bids = bids,
			Asks = asks,
		};

		msg.SetSubscriptionIds(subscriptionIds);

		return msg;
	}

	[TestMethod]
	public void ProcessInMessage_Reset_ClearsDepths()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		// Subscribe with depth 5
		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		};

		manager.ProcessInMessage(subscription);

		// Reset should clear depths
		var (toInner, toOut) = manager.ProcessInMessage(new ResetMessage());

		toInner.Type.AssertEqual(MessageTypes.Reset);
		toOut.Length.AssertEqual(0);

		// After reset, snapshot should not be truncated
		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, [1], 10);
		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);

		forward.AssertSame(snapshot);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_MarketDepth_WithMaxDepth_AddsToDepthsTracking()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		};

		var (toInner, toOut) = manager.ProcessInMessage(subscription);

		// MaxDepth should be changed to 10 (nearest supported)
		var mdMsg = (MarketDataMessage)toInner;
		mdMsg.MaxDepth.AssertEqual(10);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_MarketDepth_WhenDepthIsSupported_DoesNotModify()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth);

		var secId = Helper.CreateSecurityId();

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 10,
		};

		var (toInner, toOut) = manager.ProcessInMessage(subscription);

		// MaxDepth should remain 10 since it equals supportedDepth
		var mdMsg = (MarketDataMessage)toInner;
		mdMsg.MaxDepth.AssertEqual(10);
		toOut.Length.AssertEqual(0);

		// Snapshot should not be truncated since depth tracking wasn't added
		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, [1], 15);
		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);

		forward.AssertSame(snapshot);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_MarketDepth_WithDoNotBuildOrderBookIncrement_DoesNotTrack()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
			DoNotBuildOrderBookIncrement = true,
		};

		var (toInner, _) = manager.ProcessInMessage(subscription);

		// MaxDepth should not be modified
		var mdMsg = (MarketDataMessage)toInner;
		mdMsg.MaxDepth.AssertEqual(5);

		// Snapshot should not be truncated
		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, [1], 10);
		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);

		forward.AssertSame(snapshot);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_MarketDepth_WithDefaultSecurityId_DoesNotTrack()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = default,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		};

		var (toInner, _) = manager.ProcessInMessage(subscription);

		// MaxDepth should not be modified
		var mdMsg = (MarketDataMessage)toInner;
		mdMsg.MaxDepth.AssertEqual(5);
	}

	[TestMethod]
	public void ProcessInMessage_Unsubscribe_RemovesDepthTracking()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		});

		// Unsubscribe
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 2,
			OriginalTransactionId = 1,
		});

		// Snapshot should not be truncated after unsubscribe
		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, [1], 10);
		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);

		forward.AssertSame(snapshot);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionResponse_Error_RemovesDepthTracking()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		});

		// Error response
		manager.ProcessOutMessage(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("error"),
		});

		// Snapshot should not be truncated after error
		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, [1], 10);
		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);

		forward.AssertSame(snapshot);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionFinished_RemovesDepthTracking()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		});

		// Subscription finished
		manager.ProcessOutMessage(new SubscriptionFinishedMessage
		{
			OriginalTransactionId = 1,
		});

		// Snapshot should not be truncated after finish
		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, [1], 10);
		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);

		forward.AssertSame(snapshot);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_QuoteChange_TruncatesSnapshot()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		// Subscribe with depth 5
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		});

		// Send snapshot with 10 levels
		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, [1], 10);
		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);

		// Original should be null (all subscription ids were truncated)
		forward.AssertNull();
		extraOut.Length.AssertEqual(1);

		var truncated = (QuoteChangeMessage)extraOut[0];
		truncated.Bids.Length.AssertEqual(5);
		truncated.Asks.Length.AssertEqual(5);
		truncated.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();

		// Verify prices are in correct order
		truncated.Bids[0].Price.AssertEqual(100m);
		truncated.Bids[^1].Price.AssertEqual(96m);
		truncated.Asks[0].Price.AssertEqual(101m);
		truncated.Asks[^1].Price.AssertEqual(105m);
	}

	[TestMethod]
	public void ProcessOutMessage_QuoteChange_WithIncrement_DoesNotTruncate()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		// Subscribe with depth 5
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		});

		// Send increment with 10 levels
		var increment = CreateSnapshot(secId, DateTime.UtcNow, [1], 10);
		increment.State = QuoteChangeStates.Increment;

		var (forward, extraOut) = manager.ProcessOutMessage(increment);

		// Increment should pass through unchanged
		forward.AssertSame(increment);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_QuoteChange_SplitsGroupsByDepth()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		// Subscribe with different depths
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		});

		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 3,
		});

		// Snapshot with both subscription ids
		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, [1, 2], 10);
		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);

		// Original should be null (all subscription ids were truncated)
		forward.AssertNull();
		extraOut.Length.AssertEqual(2);

		var depth5 = extraOut.Cast<QuoteChangeMessage>().Single(q => q.GetSubscriptionIds().SequenceEqual([1L]));
		depth5.Bids.Length.AssertEqual(5);
		depth5.Asks.Length.AssertEqual(5);

		var depth3 = extraOut.Cast<QuoteChangeMessage>().Single(q => q.GetSubscriptionIds().SequenceEqual([2L]));
		depth3.Bids.Length.AssertEqual(3);
		depth3.Asks.Length.AssertEqual(3);
	}

	[TestMethod]
	public void ProcessOutMessage_QuoteChange_KeepsUntrackedIdsInOriginal()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		// Subscribe with depth 5
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		});

		// Snapshot with tracked and untracked subscription ids
		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, [1, 99], 10);
		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);

		// Original should keep untracked id
		forward.AssertSame(snapshot);
		((QuoteChangeMessage)forward).GetSubscriptionIds().SequenceEqual([99L]).AssertTrue();

		extraOut.Length.AssertEqual(1);
		var truncated = (QuoteChangeMessage)extraOut[0];
		truncated.Bids.Length.AssertEqual(5);
		truncated.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();
	}

	[TestMethod]
	public void ProcessOutMessage_QuoteChange_WhenNoDepthsTracked_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth);

		var secId = Helper.CreateSecurityId();

		// No subscriptions

		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, [1], 10);
		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);

		forward.AssertSame(snapshot);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_MarketDepth_WithNullMaxDepth_DoesNotTrack()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = null,
		};

		manager.ProcessInMessage(subscription);

		// Snapshot should not be truncated
		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, [1], 10);
		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);

		forward.AssertSame(snapshot);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_NonMarketData_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var connectMsg = new ConnectMessage();
		var (toInner, toOut) = manager.ProcessInMessage(connectMsg);

		toInner.AssertSame(connectMsg);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_NonQuoteChange_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
		};

		var (forward, extraOut) = manager.ProcessOutMessage(execMsg);

		forward.AssertSame(execMsg);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_MarketDepth_WithNonMarketDepthDataType_DoesNotTrack()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookTruncateManager(logReceiver, depth => depth < 10 ? 10 : depth);

		var secId = Helper.CreateSecurityId();

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			MaxDepth = 5,
		};

		var (toInner, _) = manager.ProcessInMessage(subscription);

		// MaxDepth should not be modified for non-MarketDepth data type
		var mdMsg = (MarketDataMessage)toInner;
		mdMsg.MaxDepth.AssertEqual(5);
	}
}
