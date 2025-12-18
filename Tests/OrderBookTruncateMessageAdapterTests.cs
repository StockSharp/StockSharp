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
}
