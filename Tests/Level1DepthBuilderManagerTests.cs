namespace StockSharp.Tests;

[TestClass]
public class Level1DepthBuilderManagerTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	private static Level1ChangeMessage CreateBestBidAsk(SecurityId securityId, DateTime time, long[] subscriptionIds, decimal? bidPrice, decimal? askPrice, decimal? bidVolume = null, decimal? askVolume = null)
	{
		var msg = new Level1ChangeMessage
		{
			SecurityId = securityId,
			ServerTime = time,
			LocalTime = time,
		};

		if (bidPrice != null)
			msg.Add(Level1Fields.BestBidPrice, bidPrice.Value);

		if (askPrice != null)
			msg.Add(Level1Fields.BestAskPrice, askPrice.Value);

		if (bidVolume != null)
			msg.Add(Level1Fields.BestBidVolume, bidVolume.Value);

		if (askVolume != null)
			msg.Add(Level1Fields.BestAskVolume, askVolume.Value);

		msg.SetSubscriptionIds(subscriptionIds);

		return msg;
	}

	[TestMethod]
	public void ProcessInMessage_Reset_ClearsState()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe first
		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		manager.ProcessInMessage(subscribe);

		// Reset
		var (toInner, toOut) = manager.ProcessInMessage(new ResetMessage());

		toInner.Length.AssertEqual(1);
		toInner[0].Type.AssertEqual(MessageTypes.Reset);
		toOut.Length.AssertEqual(0);

		// After reset, Level1Change should not produce books
		var l1 = CreateBestBidAsk(secId, DateTime.UtcNow, [1], bidPrice: 100, askPrice: 101);
		var (forward, extraOut) = manager.ProcessOutMessage(l1);

		forward.AssertSame(l1);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_MarketDepthSubscribe_RewritesToLevel1()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		var (toInner, toOut) = manager.ProcessInMessage(subscribe);

		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);

		var sent = (MarketDataMessage)toInner[0];
		sent.DataType2.AssertEqual(DataType.Level1);
		sent.TransactionId.AssertEqual(1);
		sent.SecurityId.AssertEqual(secId);
	}

	[TestMethod]
	public void ProcessInMessage_MarketDepthSubscribe_DoesNotCloneOriginal()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		var (toInner, _) = manager.ProcessInMessage(subscribe);

		// Original message should NOT be modified
		subscribe.DataType2.AssertEqual(DataType.MarketDepth);

		// Returned message should be a different instance
		toInner[0].AssertNotSame(subscribe);
	}

	[TestMethod]
	public void ProcessInMessage_BuildModeLoad_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			BuildMode = MarketDataBuildModes.Load,
		};

		var (toInner, toOut) = manager.ProcessInMessage(subscribe);

		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);

		// Should pass through unchanged
		var sent = (MarketDataMessage)toInner[0];
		sent.DataType2.AssertEqual(DataType.MarketDepth);
	}

	[TestMethod]
	public void ProcessInMessage_BuildFromNotLevel1_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			BuildFrom = DataType.OrderLog, // Not Level1
		};

		var (toInner, toOut) = manager.ProcessInMessage(subscribe);

		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);

		// Should pass through unchanged
		var sent = (MarketDataMessage)toInner[0];
		sent.DataType2.AssertEqual(DataType.MarketDepth);
	}

	[TestMethod]
	public void ProcessInMessage_DefaultSecurityId_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = default, // No security
			DataType2 = DataType.MarketDepth,
		};

		var (toInner, toOut) = manager.ProcessInMessage(subscribe);

		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);

		// Should pass through unchanged
		var sent = (MarketDataMessage)toInner[0];
		sent.DataType2.AssertEqual(DataType.MarketDepth);
	}

	[TestMethod]
	public void ProcessOutMessage_Level1Change_BuildsOrderBook()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();
		var now = DateTime.UtcNow;

		// Subscribe
		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};
		manager.ProcessInMessage(subscribe);

		// Process Level1 change
		var l1 = CreateBestBidAsk(secId, now, [1], bidPrice: 100, askPrice: 101, bidVolume: 10, askVolume: 20);
		var (forward, extraOut) = manager.ProcessOutMessage(l1);

		// Level1 should be suppressed (no other subscribers)
		forward.AssertNull();

		// Should generate order book
		extraOut.Length.AssertEqual(1);
		var book = (QuoteChangeMessage)extraOut[0];

		book.SecurityId.AssertEqual(secId);
		book.ServerTime.AssertEqual(now);
		book.LocalTime.AssertEqual(now);
		book.BuildFrom.AssertEqual(DataType.Level1);
		book.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();

		book.Bids.Length.AssertEqual(1);
		book.Bids[0].Price.AssertEqual(100m);
		book.Bids[0].Volume.AssertEqual(10m);

		book.Asks.Length.AssertEqual(1);
		book.Asks[0].Price.AssertEqual(101m);
		book.Asks[0].Volume.AssertEqual(20m);
	}

	[TestMethod]
	public void ProcessOutMessage_Level1Change_WithMixedSubscriptionIds_SplitsCorrectly()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();
		var now = DateTime.UtcNow;

		// Subscribe to order book (will be converted to L1)
		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};
		manager.ProcessInMessage(subscribe);

		// Level1 with subscription IDs for both order book (1) and pure L1 (99)
		var l1 = CreateBestBidAsk(secId, now, [1, 99], bidPrice: 100, askPrice: 101, bidVolume: 10, askVolume: 20);
		var (forward, extraOut) = manager.ProcessOutMessage(l1);

		// Level1 should be forwarded with only remaining IDs
		forward.AssertNotNull();
		var forwardedL1 = (Level1ChangeMessage)forward;
		forwardedL1.GetSubscriptionIds().SequenceEqual([99L]).AssertTrue();

		// Should also generate order book
		extraOut.Length.AssertEqual(1);
		var book = (QuoteChangeMessage)extraOut[0];
		book.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();
	}

	[TestMethod]
	public void ProcessOutMessage_Level1Change_DuplicateValuesAreFiltered()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();
		var now = DateTime.UtcNow;

		// Subscribe
		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};
		manager.ProcessInMessage(subscribe);

		// First Level1 change
		var l1First = CreateBestBidAsk(secId, now, [1], bidPrice: 100, askPrice: 101, bidVolume: 10, askVolume: 20);
		var (_, extraOut1) = manager.ProcessOutMessage(l1First);
		extraOut1.Length.AssertEqual(1);
		var book1 = (QuoteChangeMessage)extraOut1[0];
		book1.SecurityId.AssertEqual(secId);
		book1.Bids[0].Price.AssertEqual(100m);
		book1.Asks[0].Price.AssertEqual(101m);

		// Same values again - should not produce a book
		var l1Second = CreateBestBidAsk(secId, now.AddSeconds(1), [1], bidPrice: 100, askPrice: 101, bidVolume: 10, askVolume: 20);
		var (forward, extraOut2) = manager.ProcessOutMessage(l1Second);

		// No book should be generated for duplicate values
		extraOut2.Length.AssertEqual(0);
		// Level1 should still be forwarded (duplicates don't affect forwarding)
		forward.AssertSame(l1Second);
	}

	[TestMethod]
	public void ProcessOutMessage_Level1Change_NoPrices_NoBook()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();
		var now = DateTime.UtcNow;

		// Subscribe
		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};
		manager.ProcessInMessage(subscribe);

		// Level1 with no bid/ask prices (only volume)
		var l1 = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = now,
			LocalTime = now,
		};
		l1.Add(Level1Fields.BestBidVolume, 10m);
		l1.Add(Level1Fields.BestAskVolume, 20m);
		l1.SetSubscriptionIds([1]);

		var (forward, extraOut) = manager.ProcessOutMessage(l1);

		// No book should be generated
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionResponse_Error_RemovesSubscription()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};
		manager.ProcessInMessage(subscribe);

		// Error response
		var response = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("Test error"),
		};
		manager.ProcessOutMessage(response);

		// Now Level1 should not produce a book
		var l1 = CreateBestBidAsk(secId, DateTime.UtcNow, [1], bidPrice: 100, askPrice: 101);
		var (forward, extraOut) = manager.ProcessOutMessage(l1);

		forward.AssertSame(l1);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionFinished_RemovesSubscription()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};
		manager.ProcessInMessage(subscribe);

		// Finished
		var finished = new SubscriptionFinishedMessage
		{
			OriginalTransactionId = 1,
		};
		manager.ProcessOutMessage(finished);

		// Now Level1 should not produce a book
		var l1 = CreateBestBidAsk(secId, DateTime.UtcNow, [1], bidPrice: 100, askPrice: 101);
		var (forward, extraOut) = manager.ProcessOutMessage(l1);

		forward.AssertSame(l1);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionOnline_MovesToOnlineState()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};
		manager.ProcessInMessage(subscribe);

		// Online
		var online = new SubscriptionOnlineMessage
		{
			OriginalTransactionId = 1,
		};
		manager.ProcessOutMessage(online);

		// Level1 should still produce a book
		var l1 = CreateBestBidAsk(secId, DateTime.UtcNow, [1], bidPrice: 100, askPrice: 101);
		var (forward, extraOut) = manager.ProcessOutMessage(l1);

		forward.AssertNull();
		extraOut.Length.AssertEqual(1);

		var book = (QuoteChangeMessage)extraOut[0];
		book.SecurityId.AssertEqual(secId);
		book.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();
		book.Bids.Length.AssertEqual(1);
		book.Bids[0].Price.AssertEqual(100m);
		book.Asks.Length.AssertEqual(1);
		book.Asks[0].Price.AssertEqual(101m);
	}

	[TestMethod]
	public void ProcessOutMessage_MultipleSubscriptions_MergeOnline()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe first
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		});

		// Subscribe second
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		});

		// Both go online
		manager.ProcessOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = 1 });
		manager.ProcessOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = 2 });

		// Level1 should produce a book with both IDs
		var l1 = CreateBestBidAsk(secId, DateTime.UtcNow, [1, 2], bidPrice: 100, askPrice: 101);
		var (forward, extraOut) = manager.ProcessOutMessage(l1);

		forward.AssertNull();
		extraOut.Length.AssertEqual(1);

		var book = (QuoteChangeMessage)extraOut[0];
		book.GetSubscriptionIds().OrderBy(i => i).SequenceEqual([1L, 2L]).AssertTrue();
	}

	[TestMethod]
	public void ProcessInMessage_Unsubscribe_RemovesSubscription()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		});

		// Unsubscribe
		var unsubscribe = new MarketDataMessage
		{
			IsSubscribe = false,
			OriginalTransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};
		var (toInner, toOut) = manager.ProcessInMessage(unsubscribe);

		toInner.Length.AssertEqual(1);
		toInner[0].Type.AssertEqual(MessageTypes.MarketData);
		((MarketDataMessage)toInner[0]).IsSubscribe.AssertFalse();
		toOut.Length.AssertEqual(0);

		// Now Level1 should not produce a book
		var l1 = CreateBestBidAsk(secId, DateTime.UtcNow, [1], bidPrice: 100, askPrice: 101);
		var (forward, extraOut) = manager.ProcessOutMessage(l1);

		forward.AssertSame(l1);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_OnlyBidPrice_CreatesBidOnlyBook()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		});

		// Level1 with only bid
		var l1 = CreateBestBidAsk(secId, DateTime.UtcNow, [1], bidPrice: 100, askPrice: null, bidVolume: 10);
		var (_, extraOut) = manager.ProcessOutMessage(l1);

		extraOut.Length.AssertEqual(1);
		var book = (QuoteChangeMessage)extraOut[0];

		book.Bids.Length.AssertEqual(1);
		book.Bids[0].Price.AssertEqual(100m);

		book.Asks.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_OnlyAskPrice_CreatesAskOnlyBook()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		});

		// Level1 with only ask
		var l1 = CreateBestBidAsk(secId, DateTime.UtcNow, [1], bidPrice: null, askPrice: 101, askVolume: 20);
		var (_, extraOut) = manager.ProcessOutMessage(l1);

		extraOut.Length.AssertEqual(1);
		var book = (QuoteChangeMessage)extraOut[0];

		book.Bids.Length.AssertEqual(0);

		book.Asks.Length.AssertEqual(1);
		book.Asks[0].Price.AssertEqual(101m);
	}

	[TestMethod]
	public void ProcessOutMessage_NoVolume_UsesZeroVolume()
	{
		var logReceiver = new TestReceiver();
		var manager = new Level1DepthBuilderManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		});

		// Level1 with price but no volume
		var l1 = CreateBestBidAsk(secId, DateTime.UtcNow, [1], bidPrice: 100, askPrice: 101);
		var (_, extraOut) = manager.ProcessOutMessage(l1);

		extraOut.Length.AssertEqual(1);
		var book = (QuoteChangeMessage)extraOut[0];

		book.Bids[0].Volume.AssertEqual(0m);
		book.Asks[0].Volume.AssertEqual(0m);
	}
}
