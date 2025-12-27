namespace StockSharp.Tests;

[TestClass]
public class OrderBookIncrementManagerTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	private static QuoteChangeMessage CreateIncrement(SecurityId securityId, DateTime serverTime, QuoteChangeStates state, long[] subscriptionIds, QuoteChange[] bids = null, QuoteChange[] asks = null)
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = securityId,
			ServerTime = serverTime,
			LocalTime = serverTime,
			State = state,
			Bids = bids ?? [],
			Asks = asks ?? [],
		};

		msg.SetSubscriptionIds(subscriptionIds);

		return msg;
	}

	[TestMethod]
	public void Reset_ClearsState()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe first
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		manager.ProcessInMessage(subscribeMsg);

		// Now reset
		var (toInner, toOut) = manager.ProcessInMessage(new ResetMessage());

		toInner.Length.AssertEqual(1);
		toInner[0].Type.AssertEqual(MessageTypes.Reset);
		toOut.Length.AssertEqual(0);

		// After reset, no subscriptions should exist - send a quote change to verify
		var quoteMsg = CreateIncrement(secId, DateTime.UtcNow, QuoteChangeStates.SnapshotComplete, [100],
			bids: [new QuoteChange(100m, 10m)],
			asks: [new QuoteChange(101m, 20m)]);

		var (forward, extraOut) = manager.ProcessOutMessage(quoteMsg);

		// Should pass through since no subscriptions after reset
		forward.AssertSame(quoteMsg);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Subscribe_MarketDepth_AddsSubscription()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		var (toInner, toOut) = manager.ProcessInMessage(subscribeMsg);

		toInner.Length.AssertEqual(1);
		toInner[0].AssertSame(subscribeMsg);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Subscribe_AllSecurity_AddsAllSecSubscription()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = default, // all securities
			DataType2 = DataType.MarketDepth,
		};

		var (toInner, toOut) = manager.ProcessInMessage(subscribeMsg);

		toInner.Length.AssertEqual(1);
		toInner[0].AssertSame(subscribeMsg);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Subscribe_DoNotBuild_AddsPassThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			DoNotBuildOrderBookIncrement = true,
		};

		var (toInner, toOut) = manager.ProcessInMessage(subscribeMsg);

		toInner.Length.AssertEqual(1);
		toInner[0].AssertSame(subscribeMsg);
		toOut.Length.AssertEqual(0);

		// Send increment - should pass through
		var quoteMsg = CreateIncrement(secId, DateTime.UtcNow, QuoteChangeStates.Increment, [100],
			bids: [new QuoteChange(100m, 10m)],
			asks: []);

		var (forward, extraOut) = manager.ProcessOutMessage(quoteMsg);

		forward.AssertSame(quoteMsg);
		forward.To<QuoteChangeMessage>().State.AssertEqual(QuoteChangeStates.Increment);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void QuoteChange_Increment_BuildsFullBook()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		manager.ProcessInMessage(subscribeMsg);

		var time = DateTime.UtcNow;

		// Send snapshot complete
		var snapshotMsg = CreateIncrement(secId, time, QuoteChangeStates.SnapshotComplete, [1],
			bids: [new QuoteChange(100m, 10m), new QuoteChange(99m, 5m)],
			asks: [new QuoteChange(101m, 20m)]);

		var (forward, extraOut) = manager.ProcessOutMessage(snapshotMsg);

		// Original should be suppressed
		forward.AssertNull();

		// Built book should be in extraOut
		extraOut.Length.AssertEqual(1);

		var built = (QuoteChangeMessage)extraOut[0];
		built.State.AssertNull();
		built.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();
		built.Bids.Length.AssertEqual(2);
		built.Asks.Length.AssertEqual(1);
	}

	[TestMethod]
	public void QuoteChange_ApplyIncrement_UpdatesBook()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		manager.ProcessInMessage(subscribeMsg);

		var time = DateTime.UtcNow;

		// Send snapshot complete
		var snapshotMsg = CreateIncrement(secId, time, QuoteChangeStates.SnapshotComplete, [1],
			bids: [new QuoteChange(100m, 10m)],
			asks: [new QuoteChange(101m, 20m)]);

		manager.ProcessOutMessage(snapshotMsg);

		// Send increment
		var incrementMsg = CreateIncrement(secId, time.AddSeconds(1), QuoteChangeStates.Increment, [1],
			bids: [new QuoteChange(100m, 15m)], // Update bid volume
			asks: []);

		var (forward, extraOut) = manager.ProcessOutMessage(incrementMsg);

		forward.AssertNull();
		extraOut.Length.AssertEqual(1);

		var built = (QuoteChangeMessage)extraOut[0];
		built.State.AssertNull();
		built.Bids[0].Price.AssertEqual(100m);
		built.Bids[0].Volume.AssertEqual(15m); // Updated volume
	}

	[TestMethod]
	public void QuoteChange_NoState_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		manager.ProcessInMessage(subscribeMsg);

		// Send quote change without state (full book, not incremental)
		var quoteMsg = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			State = null,
			Bids = [new QuoteChange(100m, 10m)],
			Asks = [new QuoteChange(101m, 20m)],
		};
		quoteMsg.SetSubscriptionIds([1]);

		var (forward, extraOut) = manager.ProcessOutMessage(quoteMsg);

		forward.AssertSame(quoteMsg);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void SubscriptionResponse_Error_RemovesSubscription()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		manager.ProcessInMessage(subscribeMsg);

		// Send error response
		var errorResponse = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("Test error")
		};

		var (forward, extraOut) = manager.ProcessOutMessage(errorResponse);

		forward.AssertSame(errorResponse);
		extraOut.Length.AssertEqual(0);

		// Subscription should be removed - quote change should pass through (no subscription to build)
		var quoteMsg = CreateIncrement(secId, DateTime.UtcNow, QuoteChangeStates.SnapshotComplete, [1],
			bids: [new QuoteChange(100m, 10m)],
			asks: [new QuoteChange(101m, 20m)]);

		(forward, extraOut) = manager.ProcessOutMessage(quoteMsg);

		// No subscription found so original message passes through
		forward.AssertSame(quoteMsg);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void SubscriptionFinished_RemovesSubscription()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		manager.ProcessInMessage(subscribeMsg);

		// Send finished
		var finishedMsg = new SubscriptionFinishedMessage { OriginalTransactionId = 1 };

		var (forward, extraOut) = manager.ProcessOutMessage(finishedMsg);

		forward.AssertSame(finishedMsg);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Unsubscribe_RemovesSubscription()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		manager.ProcessInMessage(subscribeMsg);

		// Unsubscribe
		var unsubscribeMsg = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 2,
			OriginalTransactionId = 1,
		};

		var (toInner, toOut) = manager.ProcessInMessage(unsubscribeMsg);

		toInner.Length.AssertEqual(1);
		toInner[0].AssertSame(unsubscribeMsg);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void SubscriptionOnline_MovesToOnline()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		manager.ProcessInMessage(subscribeMsg);

		// Mark as online
		var onlineMsg = new SubscriptionOnlineMessage { OriginalTransactionId = 1 };

		var (forward, extraOut) = manager.ProcessOutMessage(onlineMsg);

		forward.AssertSame(onlineMsg);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void AllSecSubscription_AppendsToBuiltBook()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe to specific security
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		};

		manager.ProcessInMessage(subscribeMsg);

		// Subscribe to all securities
		var allSecSubscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 99,
			SecurityId = default,
			DataType2 = DataType.MarketDepth,
		};

		manager.ProcessInMessage(allSecSubscribeMsg);

		// Send snapshot
		var snapshotMsg = CreateIncrement(secId, DateTime.UtcNow, QuoteChangeStates.SnapshotComplete, [1],
			bids: [new QuoteChange(100m, 10m)],
			asks: [new QuoteChange(101m, 20m)]);

		var (forward, extraOut) = manager.ProcessOutMessage(snapshotMsg);

		extraOut.Length.AssertEqual(1);
		var built = (QuoteChangeMessage)extraOut[0];
		built.GetSubscriptionIds().OrderBy(i => i).SequenceEqual([1L, 99L]).AssertTrue();
	}

	[TestMethod]
	public void NonMarketDataMessage_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var connectMsg = new ConnectMessage();

		var (toInner, toOut) = manager.ProcessInMessage(connectMsg);

		toInner.Length.AssertEqual(1);
		toInner[0].AssertSame(connectMsg);
		toOut.Length.AssertEqual(0);

		var (forward, extraOut) = manager.ProcessOutMessage(new DisconnectMessage());

		forward.AssertNotNull();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void MarketData_NonDepth_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new OrderBookIncrementManager(logReceiver);

		var secId = Helper.CreateSecurityId();

		// Subscribe to ticks (not depth)
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var (toInner, toOut) = manager.ProcessInMessage(subscribeMsg);

		toInner.Length.AssertEqual(1);
		toInner[0].AssertSame(subscribeMsg);
		toOut.Length.AssertEqual(0);
	}
}
