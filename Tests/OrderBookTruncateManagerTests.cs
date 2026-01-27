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
