namespace StockSharp.Tests;

[TestClass]
public class OrderBookTruncateManagerTests : BaseTestClass
{
	private class MockLogReceiver : BaseLogReceiver { }

	private class MockOrderBookTruncateManagerState : IOrderBookTruncateManagerState
	{
		public List<(long id, int depth)> AddedDepths { get; } = [];
		public List<long> RemovedDepths { get; } = [];
		public int ClearCount { get; set; }

		private readonly Dictionary<long, int> _depths = [];

		public void AddDepth(long transactionId, int depth) { _depths[transactionId] = depth; AddedDepths.Add((transactionId, depth)); }
		public int? TryGetDepth(long transactionId) => _depths.TryGetValue(transactionId, out var d) ? d : null;
		public bool RemoveDepth(long transactionId) { RemovedDepths.Add(transactionId); return _depths.Remove(transactionId); }
		public bool HasDepths => _depths.Count > 0;
		public IEnumerable<(int? depth, long[] ids)> GroupByDepth(long[] subscriptionIds)
			=> subscriptionIds.GroupBy(id => _depths.TryGetValue(id, out var d) ? (int?)d : null).Select(g => (g.Key, g.ToArray()));
		public void Clear() { _depths.Clear(); ClearCount++; }
	}

	private static OrderBookTruncateManager CreateManager(MockOrderBookTruncateManagerState state, Func<int, int?> nearestSupportedDepth = null)
	{
		return new OrderBookTruncateManager(
			new MockLogReceiver(),
			nearestSupportedDepth ?? (depth => depth < 10 ? 10 : depth),
			state);
	}

	[TestMethod]
	public void ProcessInMessage_Reset_ClearsState()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		// Add some state first
		state.AddDepth(1, 10);

		var (toInner, toOut) = manager.ProcessInMessage(new ResetMessage());

		AreEqual(MessageTypes.Reset, toInner.Type);
		AreEqual(0, toOut.Length);
		AreEqual(1, state.ClearCount);
	}

	[TestMethod]
	public void ProcessInMessage_MarketDataSubscribe_WithDepth_AddsToState()
	{
		var state = new MockOrderBookTruncateManagerState();
		// nearestSupportedDepth returns 20 for input 10, so supportedDepth != actualDepth
		var manager = CreateManager(state, depth => 20);

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			TransactionId = 1,
			MaxDepth = 10
		};

		var (toInner, toOut) = manager.ProcessInMessage(mdMsg);

		AreEqual(0, toOut.Length);
		AreEqual(1, state.AddedDepths.Count);
		AreEqual(1L, state.AddedDepths[0].id);
		AreEqual(10, state.AddedDepths[0].depth);

		// The outgoing message should have MaxDepth changed to 20
		var outMd = (MarketDataMessage)toInner;
		AreEqual(20, outMd.MaxDepth);
	}

	[TestMethod]
	public void ProcessInMessage_MarketDataUnsubscribe_RemovesFromState()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		// First subscribe to add depth
		state.AddDepth(1, 10);

		var unsubMsg = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 2,
			OriginalTransactionId = 1,
		};

		manager.ProcessInMessage(unsubMsg);

		IsTrue(state.RemovedDepths.Contains(1L));
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionResponse_Error_RemovesFromState()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		// Add depth to state
		state.AddDepth(1, 10);

		var resp = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("test")
		};

		manager.ProcessOutMessage(resp);

		IsTrue(state.RemovedDepths.Contains(1L));
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionFinished_RemovesFromState()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		// Add depth to state
		state.AddDepth(1, 10);

		var finished = new SubscriptionFinishedMessage
		{
			OriginalTransactionId = 1
		};

		manager.ProcessOutMessage(finished);

		IsTrue(state.RemovedDepths.Contains(1L));
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionFinished_OneOfTwo_OtherStillTruncates()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		state.AddDepth(1, 2);
		state.AddDepth(2, 3);

		// Finished for subscription 1
		manager.ProcessOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = 1 });

		IsTrue(state.RemovedDepths.Contains(1L), "Finished subscription should be removed");

		// Quote arrives with subscription 2 only
		var quoteMsg = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			ServerTime = DateTime.UtcNow,
			Bids = [
				new QuoteChange(105, 10),
				new QuoteChange(104, 20),
				new QuoteChange(103, 30),
				new QuoteChange(102, 40),
			],
			Asks = [
				new QuoteChange(106, 10),
				new QuoteChange(107, 20),
				new QuoteChange(108, 30),
				new QuoteChange(109, 40),
			],
		};
		quoteMsg.SetSubscriptionIds([2L]);

		var (forward, extraOut) = manager.ProcessOutMessage(quoteMsg);

		var totalBooks = extraOut.OfType<QuoteChangeMessage>().ToList();
		if (forward is QuoteChangeMessage fwd)
			totalBooks.Add(fwd);

		totalBooks.Count.AssertGreater(0, "Remaining subscription should still get truncated book");
		var book = totalBooks[0];
		book.GetSubscriptionIds().Contains(2L).AssertTrue("Should have remaining subscription ID");
		book.Bids.Length.AssertEqual(3, "Should truncate to depth 3");
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionError_OneOfTwo_OtherStillTruncates()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		state.AddDepth(1, 2);
		state.AddDepth(2, 4);

		// Error for subscription 1
		manager.ProcessOutMessage(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("fail"),
		});

		IsTrue(state.RemovedDepths.Contains(1L), "Errored subscription should be removed");

		// Quote arrives with subscription 2
		var quoteMsg = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			ServerTime = DateTime.UtcNow,
			Bids = [
				new QuoteChange(105, 10),
				new QuoteChange(104, 20),
				new QuoteChange(103, 30),
				new QuoteChange(102, 40),
				new QuoteChange(101, 50),
			],
			Asks = [
				new QuoteChange(106, 10),
				new QuoteChange(107, 20),
				new QuoteChange(108, 30),
				new QuoteChange(109, 40),
				new QuoteChange(110, 50),
			],
		};
		quoteMsg.SetSubscriptionIds([2L]);

		var (forward, extraOut) = manager.ProcessOutMessage(quoteMsg);

		var totalBooks = extraOut.OfType<QuoteChangeMessage>().ToList();
		if (forward is QuoteChangeMessage fwd)
			totalBooks.Add(fwd);

		totalBooks.Count.AssertGreater(0, "Remaining subscription should still get truncated book");
		var book = totalBooks[0];
		book.Bids.Length.AssertEqual(4, "Should truncate to depth 4");
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionFinished_Both_QuotePassesThrough()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		state.AddDepth(1, 2);
		state.AddDepth(2, 3);

		// Finished for both
		manager.ProcessOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = 1 });
		manager.ProcessOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = 2 });

		IsFalse(state.HasDepths, "No depths should remain");

		// Quote arrives
		var quoteMsg = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(105, 10), new QuoteChange(104, 20), new QuoteChange(103, 30)],
			Asks = [new QuoteChange(106, 10), new QuoteChange(107, 20), new QuoteChange(108, 30)],
		};
		quoteMsg.SetSubscriptionIds([1L, 2L]);

		var (forward, extraOut) = manager.ProcessOutMessage(quoteMsg);

		// No depths registered — should pass through without truncation
		if (forward != null)
		{
			var fwd = (QuoteChangeMessage)forward;
			fwd.Bids.Length.AssertEqual(3, "No truncation when no depths registered");
		}
	}

	[TestMethod]
	public void ProcessOutMessage_QuoteChange_TwoSubscriptions_DifferentDepths_ProducesTwoBooks()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		// Two subscriptions with different depths
		state.AddDepth(1, 2);
		state.AddDepth(2, 4);

		var quoteMsg = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			ServerTime = DateTime.UtcNow,
			Bids = [
				new QuoteChange(105, 10),
				new QuoteChange(104, 20),
				new QuoteChange(103, 30),
				new QuoteChange(102, 40),
				new QuoteChange(101, 50),
			],
			Asks = [
				new QuoteChange(106, 10),
				new QuoteChange(107, 20),
				new QuoteChange(108, 30),
				new QuoteChange(109, 40),
				new QuoteChange(110, 50),
			],
		};
		quoteMsg.SetSubscriptionIds([1L, 2L]);

		var (forward, extraOut) = manager.ProcessOutMessage(quoteMsg);

		// Should produce separate truncated books for each depth
		var totalBooks = extraOut.OfType<QuoteChangeMessage>().ToArray();
		if (forward is QuoteChangeMessage fwd)
			totalBooks = [.. totalBooks, fwd];

		totalBooks.Length.AssertGreater(0, "Should produce truncated books");

		// Check that subscription IDs are distributed correctly
		var allIds = totalBooks.SelectMany(b => b.GetSubscriptionIds()).Distinct().OrderBy(x => x).ToArray();
		allIds.Contains(1L).AssertTrue("Should contain subscription 1");
		allIds.Contains(2L).AssertTrue("Should contain subscription 2");
	}

	[TestMethod]
	public void ProcessOutMessage_QuoteChange_TwoSubscriptions_SameDepth_ProducesOneBook()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		// Two subscriptions with same depth
		state.AddDepth(1, 3);
		state.AddDepth(2, 3);

		var quoteMsg = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			ServerTime = DateTime.UtcNow,
			Bids = [
				new QuoteChange(105, 10),
				new QuoteChange(104, 20),
				new QuoteChange(103, 30),
				new QuoteChange(102, 40),
				new QuoteChange(101, 50),
			],
			Asks = [
				new QuoteChange(106, 10),
				new QuoteChange(107, 20),
				new QuoteChange(108, 30),
				new QuoteChange(109, 40),
				new QuoteChange(110, 50),
			],
		};
		quoteMsg.SetSubscriptionIds([1L, 2L]);

		var (forward, extraOut) = manager.ProcessOutMessage(quoteMsg);

		// With same depth, should produce one book with both IDs
		var totalBooks = extraOut.OfType<QuoteChangeMessage>().ToList();
		if (forward is QuoteChangeMessage fwd)
			totalBooks.Add(fwd);

		totalBooks.Count.AssertGreater(0, "Should produce at least one book");

		// Find the book that contains both IDs
		var bookWithBothIds = totalBooks.FirstOrDefault(b =>
		{
			var ids = b.GetSubscriptionIds();
			return ids.Contains(1L) && ids.Contains(2L);
		});
		bookWithBothIds.AssertNotNull("Same-depth subscriptions should share one book with both IDs");
		bookWithBothIds.Bids.Length.AssertEqual(3, "Should truncate to depth 3");
		bookWithBothIds.Asks.Length.AssertEqual(3, "Should truncate to depth 3");
	}

	[TestMethod]
	public void ProcessOutMessage_QuoteChange_MixKnownAndUnknownIds()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		// Only subscription 1 has depth
		state.AddDepth(1, 2);

		var quoteMsg = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			ServerTime = DateTime.UtcNow,
			Bids = [
				new QuoteChange(105, 10),
				new QuoteChange(104, 20),
				new QuoteChange(103, 30),
			],
			Asks = [
				new QuoteChange(106, 10),
				new QuoteChange(107, 20),
				new QuoteChange(108, 30),
			],
		};
		quoteMsg.SetSubscriptionIds([1L, 999L]);

		var (forward, extraOut) = manager.ProcessOutMessage(quoteMsg);

		// Should handle gracefully: truncated book for ID 1, pass-through for ID 999
		var totalBooks = extraOut.OfType<QuoteChangeMessage>().ToList();
		if (forward is QuoteChangeMessage fwd)
			totalBooks.Add(fwd);

		totalBooks.Count.AssertGreater(0, "Should produce at least one book");

		var allIds = totalBooks.SelectMany(b => b.GetSubscriptionIds()).Distinct().ToArray();
		allIds.Contains(1L).AssertTrue("Known ID should be present");
	}

	#region Status Message Handling

	[TestMethod]
	public void StatusMessage_Response_OK_TwoDepths_ForwardedNoDepthRemoved()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		state.AddDepth(1, 5);
		state.AddDepth(2, 10);

		// Response OK for sub1
		var resp = new SubscriptionResponseMessage { OriginalTransactionId = 1 };
		var (forward, extraOut) = manager.ProcessOutMessage(resp);

		forward.AssertNotNull("Response OK should be forwarded");
		forward.AssertSame(resp);
		((SubscriptionResponseMessage)forward).OriginalTransactionId.AssertEqual(1);
		extraOut.Length.AssertEqual(0);

		// No depths should be removed on success
		IsFalse(state.RemovedDepths.Contains(1L), "Depth for sub1 should NOT be removed on OK response");
		IsFalse(state.RemovedDepths.Contains(2L), "Depth for sub2 should NOT be removed");
	}

	[TestMethod]
	public void StatusMessage_Response_Error_TwoDepths_OnlyErroredDepthRemoved()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		state.AddDepth(1, 5);
		state.AddDepth(2, 10);

		// Error for sub1
		var errorResp = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("fail"),
		};
		var (forward, extraOut) = manager.ProcessOutMessage(errorResp);

		forward.AssertNotNull("Error response should be forwarded");
		forward.AssertSame(errorResp);
		((SubscriptionResponseMessage)forward).OriginalTransactionId.AssertEqual(1);
		((SubscriptionResponseMessage)forward).Error.AssertNotNull();
		extraOut.Length.AssertEqual(0);

		// Only sub1 depth removed
		IsTrue(state.RemovedDepths.Contains(1L), "Errored sub depth should be removed");
		IsFalse(state.RemovedDepths.Contains(2L), "Other sub depth should NOT be removed");

		// Sub2 depth still exists
		state.TryGetDepth(2).AssertEqual(10);
	}

	[TestMethod]
	public void StatusMessage_Finished_TwoDepths_OnlyFinishedDepthRemoved()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		state.AddDepth(1, 5);
		state.AddDepth(2, 10);

		// Finished for sub1
		var finished = new SubscriptionFinishedMessage { OriginalTransactionId = 1 };
		var (forward, extraOut) = manager.ProcessOutMessage(finished);

		forward.AssertNotNull("Finished should be forwarded");
		forward.AssertSame(finished);
		((SubscriptionFinishedMessage)forward).OriginalTransactionId.AssertEqual(1);
		extraOut.Length.AssertEqual(0);

		// Only sub1 depth removed
		IsTrue(state.RemovedDepths.Contains(1L), "Finished sub depth should be removed");
		IsFalse(state.RemovedDepths.Contains(2L), "Other sub depth should NOT be removed");

		// Sub2 depth still exists
		state.TryGetDepth(2).AssertEqual(10);
	}

	[TestMethod]
	public void StatusMessage_Online_ForwardedUnmodified()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		state.AddDepth(1, 5);

		// Online — not specifically handled, should pass through
		var online = new SubscriptionOnlineMessage { OriginalTransactionId = 1 };
		var (forward, extraOut) = manager.ProcessOutMessage(online);

		forward.AssertNotNull("Online should be forwarded");
		forward.AssertSame(online);
		((SubscriptionOnlineMessage)forward).OriginalTransactionId.AssertEqual(1);
		extraOut.Length.AssertEqual(0);

		// Depth should NOT be removed
		IsFalse(state.RemovedDepths.Contains(1L), "Online should not remove depth");
		state.TryGetDepth(1).AssertEqual(5);
	}

	#endregion

	[TestMethod]
	public void ProcessOutMessage_QuoteChange_WithDepths_TruncatesBids()
	{
		var state = new MockOrderBookTruncateManagerState();
		var manager = CreateManager(state);

		// Add depth=2 for subscription 1
		state.AddDepth(1, 2);

		var quoteMsg = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			ServerTime = DateTime.UtcNow,
			Bids = [
				new QuoteChange(105, 10),
				new QuoteChange(104, 20),
				new QuoteChange(103, 30),
				new QuoteChange(102, 40),
				new QuoteChange(101, 50),
			],
			Asks = [
				new QuoteChange(106, 10),
				new QuoteChange(107, 20),
				new QuoteChange(108, 30),
				new QuoteChange(109, 40),
				new QuoteChange(110, 50),
			],
		};
		quoteMsg.SetSubscriptionIds([1L]);

		var (forward, extraOut) = manager.ProcessOutMessage(quoteMsg);

		// Original should be null since all subscription ids were truncated
		IsNull(forward);
		AreEqual(1, extraOut.Length);

		var truncated = (QuoteChangeMessage)extraOut[0];
		IsTrue(truncated.Bids.Length <= 2, $"Expected max 2 bids, got {truncated.Bids.Length}");
		IsTrue(truncated.Asks.Length <= 2, $"Expected max 2 asks, got {truncated.Asks.Length}");
	}
}
