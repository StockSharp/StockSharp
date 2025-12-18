namespace StockSharp.Tests;

[TestClass]
public class OrderBookIncrementMessageAdapterTests : BaseTestClass
{
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
	public async Task QuoteChange_Increment_BuildsFullBook_AndSuppressesOriginal()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new OrderBookIncrementMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			DoNotBuildOrderBookIncrement = false,
		}, token);

		output.Clear();

		var time = DateTime.UtcNow;

		inner.SendOutMessage(CreateIncrement(secId, time, QuoteChangeStates.SnapshotComplete, subscriptionIds: [1],
			bids: [new QuoteChange(100m, 10m), new QuoteChange(99m, 5m)],
			asks: [new QuoteChange(101m, 20m)]));

		var built = output.OfType<QuoteChangeMessage>().Single();
		built.State.AssertNull();

		built.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();
		built.Bids.Length.AssertEqual(2);
		built.Asks.Length.AssertEqual(1);

		output.Clear();

		inner.SendOutMessage(CreateIncrement(secId, time.AddSeconds(1), QuoteChangeStates.Increment, subscriptionIds: [1],
			bids: [new QuoteChange(100m, 15m)],
			asks: []));

		built = output.OfType<QuoteChangeMessage>().Single();
		built.State.AssertNull();
		built.Bids[0].Price.AssertEqual(100m);
		built.Bids[0].Volume.AssertEqual(15m);
	}

	[TestMethod]
	public async Task QuoteChange_WhenDoNotBuildOrderBookIncrement_PassesThroughOriginal()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new OrderBookIncrementMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			DoNotBuildOrderBookIncrement = true,
		}, token);

		output.Clear();

		var time = DateTime.UtcNow;
		var original = CreateIncrement(secId, time, QuoteChangeStates.Increment, subscriptionIds: [1],
			bids: [new QuoteChange(100m, 10m)],
			asks: []);

		inner.SendOutMessage(original);

		var passThrough = output.OfType<QuoteChangeMessage>().Single();
		ReferenceEquals(passThrough, original).AssertTrue();
		passThrough.State.AssertEqual(QuoteChangeStates.Increment);
		passThrough.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();
	}

	[TestMethod]
	public async Task AllSecuritySubscription_IsAppendedToBuiltBookSubscriptionIds()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new OrderBookIncrementMessageAdapter(inner);

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
			TransactionId = 99,
			SecurityId = default,
			DataType2 = DataType.MarketDepth,
		}, token);

		output.Clear();

		inner.SendOutMessage(CreateIncrement(secId, DateTime.UtcNow, QuoteChangeStates.SnapshotComplete, subscriptionIds: [1],
			bids: [new QuoteChange(100m, 10m)],
			asks: [new QuoteChange(101m, 20m)]));

		var book = output.OfType<QuoteChangeMessage>().Single();
		book.State.AssertNull();
		book.GetSubscriptionIds().OrderBy(i => i).SequenceEqual([1L, 99L]).AssertTrue();
	}
}
