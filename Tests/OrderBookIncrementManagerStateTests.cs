namespace StockSharp.Tests;

[TestClass]
public class OrderBookIncrementManagerStateTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	private static readonly SecurityId _secId = Helper.CreateSecurityId();

	private static QuoteChangeMessage CreateSnapshot(SecurityId securityId, long[] subscriptionIds)
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = securityId,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
			State = QuoteChangeStates.SnapshotComplete,
			Bids = [new QuoteChange(100m, 10m)],
			Asks = [new QuoteChange(101m, 20m)],
		};

		msg.SetSubscriptionIds(subscriptionIds);

		return msg;
	}

	[TestMethod]
	public void AddSubscription_ContainsSubscription_ReturnsTrue()
	{
		var state = new OrderBookIncrementManagerState();
		var logReceiver = new TestReceiver();

		state.AddSubscription(1, _secId, logReceiver);

		state.ContainsSubscription(1).AssertTrue();
	}

	[TestMethod]
	public void HasAnySubscriptions_Empty_ReturnsFalse()
	{
		var state = new OrderBookIncrementManagerState();

		state.HasAnySubscriptions.AssertFalse();
	}

	[TestMethod]
	public void HasAnySubscriptions_AfterAdd_ReturnsTrue()
	{
		var state = new OrderBookIncrementManagerState();
		var logReceiver = new TestReceiver();

		state.AddSubscription(1, _secId, logReceiver);

		state.HasAnySubscriptions.AssertTrue();
	}

	[TestMethod]
	public void AddPassThrough_IsPassThrough_ReturnsTrue()
	{
		var state = new OrderBookIncrementManagerState();

		state.AddPassThrough(1);

		state.IsPassThrough(1).AssertTrue();
	}

	[TestMethod]
	public void AddAllSecSubscription_GetAllSecIds_ReturnsId()
	{
		var state = new OrderBookIncrementManagerState();

		state.AddAllSecSubscription(42);

		var ids = state.GetAllSecSubscriptionIds();

		ids.Length.AssertEqual(1);
		ids[0].AssertEqual(42L);
	}

	[TestMethod]
	public void AddAllSecPassThrough_IsAllSecPassThrough_ReturnsTrue()
	{
		var state = new OrderBookIncrementManagerState();

		state.AddAllSecPassThrough(7);

		state.IsAllSecPassThrough(7).AssertTrue();
	}

	[TestMethod]
	public void OnSubscriptionOnline_SharesBuilder()
	{
		var state = new OrderBookIncrementManagerState();
		var logReceiver = new TestReceiver();

		// Add two subscriptions for the same security
		state.AddSubscription(1, _secId, logReceiver);
		state.AddSubscription(2, _secId, logReceiver);

		// Bring both online - second should share the builder of the first
		state.OnSubscriptionOnline(1);
		state.OnSubscriptionOnline(2);

		// Apply a snapshot via subscription 1
		var snapshot = CreateSnapshot(_secId, [1]);
		var result = state.TryApply(1, snapshot, out var subscriptionIds);

		result.AssertNotNull();
		// Both subscription IDs should be returned since they share the builder
		subscriptionIds.Contains(1L).AssertTrue();
		subscriptionIds.Contains(2L).AssertTrue();
	}

	[TestMethod]
	public void TryApply_AfterSnapshot_ReturnsBuiltBook()
	{
		var state = new OrderBookIncrementManagerState();
		var logReceiver = new TestReceiver();

		state.AddSubscription(1, _secId, logReceiver);
		state.OnSubscriptionOnline(1);

		var snapshot = CreateSnapshot(_secId, [1]);
		var result = state.TryApply(1, snapshot, out var subscriptionIds);

		result.AssertNotNull();
		result.Bids.Length.AssertEqual(1);
		result.Asks.Length.AssertEqual(1);
		result.Bids[0].Price.AssertEqual(100m);
		result.Bids[0].Volume.AssertEqual(10m);
		result.Asks[0].Price.AssertEqual(101m);
		result.Asks[0].Volume.AssertEqual(20m);
		subscriptionIds.AssertNotNull();
		subscriptionIds.Contains(1L).AssertTrue();
	}

	[TestMethod]
	public void RemoveSubscription_ContainsSubscription_ReturnsFalse()
	{
		var state = new OrderBookIncrementManagerState();
		var logReceiver = new TestReceiver();

		state.AddSubscription(1, _secId, logReceiver);
		state.RemoveSubscription(1);

		state.ContainsSubscription(1).AssertFalse();
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var state = new OrderBookIncrementManagerState();
		var logReceiver = new TestReceiver();

		state.AddSubscription(1, _secId, logReceiver);
		state.AddPassThrough(2);
		state.AddAllSecSubscription(3);
		state.AddAllSecPassThrough(4);

		state.Clear();

		state.HasAnySubscriptions.AssertFalse();
		state.ContainsSubscription(1).AssertFalse();
		state.IsPassThrough(2).AssertFalse();
		state.GetAllSecSubscriptionIds().Length.AssertEqual(0);
		state.IsAllSecPassThrough(4).AssertFalse();
	}
}
