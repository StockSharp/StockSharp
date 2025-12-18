namespace StockSharp.Tests;

[TestClass]
public class OrderBookTruncateMessageAdapterTests : BaseTestClass
{
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
	public async Task MarketDepthSubscribe_RewritesMaxDepth_ToNearestSupportedDepth()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);
		var depths = inner.SupportedOrderBookDepths.ToArray();
		AreEqual(1, depths.Length, "SupportedOrderBookDepths length.");
		AreEqual(10, depths[0], "SupportedOrderBookDepths[0].");
		AreEqual(10, inner.NearestSupportedDepth(5), "NearestSupportedDepth(5).");

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var md = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		};

		await adapter.SendInMessageAsync(md, token);

		md.MaxDepth.AssertEqual(5);

		inner.InMessages.Count.AssertEqual(1);
		AreEqual(10, ((MarketDataMessage)inner.InMessages[0]).MaxDepth, "MaxDepth passed to inner adapter.");
	}

	[TestMethod]
	public async Task MarketDepthSubscribe_WhenDoNotBuildOrderBookIncrement_DoesNotRewrite_AndDoesNotTruncateSnapshot()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
			DoNotBuildOrderBookIncrement = true,
		}, token);

		inner.InMessages.Count.AssertEqual(1);
		((MarketDataMessage)inner.InMessages[0]).MaxDepth.AssertEqual(5);

		output.Clear();

		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10);
		inner.SendOutMessage(snapshot);

		var outMsg = output.OfType<QuoteChangeMessage>().Single();
		ReferenceEquals(outMsg, snapshot).AssertTrue();
		outMsg.Bids.Length.AssertEqual(10);
		outMsg.Asks.Length.AssertEqual(10);
	}

	[TestMethod]
	public async Task MarketDepthSubscribe_WhenMaxDepthIsNotSpecified_DoesNotTruncateSnapshot()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		}, token);

		inner.InMessages.Count.AssertEqual(1);
		((MarketDataMessage)inner.InMessages[0]).MaxDepth.AssertNull();

		output.Clear();

		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10);
		inner.SendOutMessage(snapshot);

		var outMsg = output.OfType<QuoteChangeMessage>().Single();
		ReferenceEquals(outMsg, snapshot).AssertTrue();
		outMsg.Bids.Length.AssertEqual(10);
		outMsg.Asks.Length.AssertEqual(10);
	}

	[TestMethod]
	public async Task MarketDepthSubscribe_WhenNoSupportedDepths_ForwardsNullMaxDepth_AndTruncatesSnapshot()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: []);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		}, token);

		inner.InMessages.Count.AssertEqual(1);
		((MarketDataMessage)inner.InMessages[0]).MaxDepth.AssertNull();

		output.Clear();

		inner.SendOutMessage(CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10));

		var truncated = output.OfType<QuoteChangeMessage>().Single();
		truncated.Bids.Length.AssertEqual(5);
		truncated.Asks.Length.AssertEqual(5);
		truncated.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();
	}

	[TestMethod]
	public async Task QuoteChange_Snapshot_IsTruncatedPerSubscriptionId()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		}, token);

		output.Clear();

		inner.SendOutMessage(CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10));

		var truncated = output.OfType<QuoteChangeMessage>().Single();
		truncated.Bids.Length.AssertEqual(5);
		truncated.Asks.Length.AssertEqual(5);
		truncated.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();

		truncated.Bids[0].Price.AssertEqual(100m);
		truncated.Bids[^1].Price.AssertEqual(96m);
		truncated.Asks[0].Price.AssertEqual(101m);
		truncated.Asks[^1].Price.AssertEqual(105m);
	}

	[TestMethod]
	public async Task QuoteChange_Snapshot_SplitsGroupsByDepth_AndKeepsUntrackedIdsInOriginal()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		}, token);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 10,
		}, token);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 3,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 3,
		}, token);

		output.Clear();

		var original = CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1, 2, 3], depth: 10);
		inner.SendOutMessage(original);

		var quotes = output.OfType<QuoteChangeMessage>().ToArray();
		quotes.Length.AssertEqual(3);

		var kept = quotes.Single(q => ReferenceEquals(q, original));
		kept.GetSubscriptionIds().SequenceEqual([2L]).AssertTrue();
		kept.Bids.Length.AssertEqual(10);
		kept.Asks.Length.AssertEqual(10);

		var depth5 = quotes.Single(q => q.GetSubscriptionIds().SequenceEqual([1L]));
		depth5.Bids.Length.AssertEqual(5);
		depth5.Asks.Length.AssertEqual(5);

		var depth3 = quotes.Single(q => q.GetSubscriptionIds().SequenceEqual([3L]));
		depth3.Bids.Length.AssertEqual(3);
		depth3.Asks.Length.AssertEqual(3);
	}

	[TestMethod]
	public async Task QuoteChange_Increment_IsNotTruncated()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		}, token);

		output.Clear();

		var inc = CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10);
		inc.State = QuoteChangeStates.Increment;

		inner.SendOutMessage(inc);

		var outMsg = output.OfType<QuoteChangeMessage>().Single();
		ReferenceEquals(outMsg, inc).AssertTrue();
		outMsg.Bids.Length.AssertEqual(10);
		outMsg.Asks.Length.AssertEqual(10);
	}

	[TestMethod]
	public async Task SubscriptionResponse_Error_RemovesDepthTracking()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		}, token);

		output.Clear();

		inner.SendOutMessage(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("error"),
		});

		inner.SendOutMessage(CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10));

		var quote = output.OfType<QuoteChangeMessage>().Single();
		quote.Bids.Length.AssertEqual(10);
		quote.Asks.Length.AssertEqual(10);
	}
}
