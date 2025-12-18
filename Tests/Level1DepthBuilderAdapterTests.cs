namespace StockSharp.Tests;

[TestClass]
public class Level1DepthBuilderAdapterTests : BaseTestClass
{
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
	public async Task MarketDepthSubscribe_RewrittenToLevel1()
	{
		var token = CancellationToken;

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new Level1DepthBuilderAdapter(inner);

		var md = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.MarketDepth,
		};

		await adapter.SendInMessageAsync(md, token);

		md.DataType2.AssertEqual(DataType.MarketDepth);

		inner.InMessages.Count.AssertEqual(1);
		var sent = (MarketDataMessage)inner.InMessages[0];
		sent.DataType2.AssertEqual(DataType.Level1);
		sent.TransactionId.AssertEqual(1);
		sent.SecurityId.AssertEqual(md.SecurityId);
	}

	[TestMethod]
	public void Level1Change_SplitsSubscriptionIdsAndBuildsBook()
	{
		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new Level1DepthBuilderAdapter(inner);

		AsyncHelper.Run(() => adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		}, CancellationToken.None));

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		var now = DateTime.UtcNow;

		inner.SendOutMessage(CreateBestBidAsk(secId, now, [1, 99], bidPrice: 100, askPrice: 101, bidVolume: 10, askVolume: 20));

		var l1 = output.OfType<Level1ChangeMessage>().Single();
		l1.GetSubscriptionIds().SequenceEqual([99L]).AssertTrue();

		var book = output.OfType<QuoteChangeMessage>().Single();
		book.SecurityId.AssertEqual(secId);
		book.BuildFrom.AssertEqual(DataType.Level1);
		book.ServerTime.AssertEqual(now);
		book.LocalTime.AssertEqual(now);
		book.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();

		book.Bids.Length.AssertEqual(1);
		book.Bids[0].Price.AssertEqual(100m);
		book.Bids[0].Volume.AssertEqual(10m);

		book.Asks.Length.AssertEqual(1);
		book.Asks[0].Price.AssertEqual(101m);
		book.Asks[0].Volume.AssertEqual(20m);
	}

	[TestMethod]
	public async Task MultipleSubscriptions_MergedIds_AndUnsubscribeReassignsKey()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new Level1DepthBuilderAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		}, token);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		}, token);

		inner.SendOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = 1 });
		inner.SendOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = 2 });

		output.Clear();

		inner.SendOutMessage(CreateBestBidAsk(secId, DateTime.UtcNow, [1, 2], bidPrice: 100, askPrice: 101, bidVolume: 10, askVolume: 20));

		output.OfType<Level1ChangeMessage>().Any().AssertFalse();

		var book = output.OfType<QuoteChangeMessage>().Single();
		book.GetSubscriptionIds().OrderBy(i => i).SequenceEqual([1L, 2L]).AssertTrue();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			OriginalTransactionId = 1,
			DataType2 = DataType.MarketDepth,
			SecurityId = secId,
		}, token);

		output.Clear();

		inner.SendOutMessage(CreateBestBidAsk(secId, DateTime.UtcNow.AddSeconds(1), [2], bidPrice: 101, askPrice: 102, bidVolume: 11, askVolume: 21));

		output.OfType<Level1ChangeMessage>().Any().AssertFalse();

		book = output.OfType<QuoteChangeMessage>().Single();
		book.GetSubscriptionIds().SequenceEqual([2L]).AssertTrue();
	}
}

