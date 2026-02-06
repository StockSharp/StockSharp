namespace StockSharp.Tests;

[TestClass]
public class Level1DepthBuilderManagerStateTests : BaseTestClass
{
	private static readonly SecurityId _secId = new() { SecurityCode = "AAPL", BoardCode = "NYSE" };

	private static Level1DepthBuilderManagerState CreateState() => new();

	private static Level1ChangeMessage CreateL1(decimal? bidPrice = null, decimal? askPrice = null, decimal? bidVolume = null, decimal? askVolume = null)
	{
		var l1 = new Level1ChangeMessage
		{
			SecurityId = _secId,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};

		if (bidPrice != null)
			l1.Changes.Add(Level1Fields.BestBidPrice, bidPrice.Value);
		if (askPrice != null)
			l1.Changes.Add(Level1Fields.BestAskPrice, askPrice.Value);
		if (bidVolume != null)
			l1.Changes.Add(Level1Fields.BestBidVolume, bidVolume.Value);
		if (askVolume != null)
			l1.Changes.Add(Level1Fields.BestAskVolume, askVolume.Value);

		return l1;
	}

	[TestMethod]
	public void AddSubscription_ContainsSubscription_ReturnsTrue()
	{
		var state = CreateState();

		state.AddSubscription(1, _secId);

		IsTrue(state.ContainsSubscription(1));
	}

	[TestMethod]
	public void ContainsSubscription_NonExistent_ReturnsFalse()
	{
		var state = CreateState();

		IsFalse(state.ContainsSubscription(999));
	}

	[TestMethod]
	public void HasAnySubscriptions_Empty_ReturnsFalse()
	{
		var state = CreateState();

		IsFalse(state.HasAnySubscriptions);
	}

	[TestMethod]
	public void HasAnySubscriptions_AfterAdd_ReturnsTrue()
	{
		var state = CreateState();

		state.AddSubscription(1, _secId);

		IsTrue(state.HasAnySubscriptions);
	}

	[TestMethod]
	public void TryBuildDepth_WithBidAndAsk_ReturnsQuoteChange()
	{
		var state = CreateState();

		state.AddSubscription(1, _secId);
		state.OnSubscriptionOnline(1);

		var l1 = CreateL1(bidPrice: 100m, askPrice: 101m, bidVolume: 10m, askVolume: 5m);

		var result = state.TryBuildDepth(1, l1, out var subIds);

		IsNotNull(result);
		AreEqual(_secId, result.SecurityId);
		AreEqual(1, result.Bids.Length);
		AreEqual(100m, result.Bids[0].Price);
		AreEqual(10m, result.Bids[0].Volume);
		AreEqual(1, result.Asks.Length);
		AreEqual(101m, result.Asks[0].Price);
		AreEqual(5m, result.Asks[0].Volume);
		IsNotNull(subIds);
		subIds.Count(id => id == 1).AssertEqual(1);
	}

	[TestMethod]
	public void TryBuildDepth_NoPrices_ReturnsNull()
	{
		var state = CreateState();

		state.AddSubscription(1, _secId);
		state.OnSubscriptionOnline(1);

		var l1 = CreateL1(); // no prices at all

		var result = state.TryBuildDepth(1, l1, out var subIds);

		IsNull(result);
	}

	[TestMethod]
	public void TryBuildDepth_DuplicateData_ReturnsNull()
	{
		var state = CreateState();

		state.AddSubscription(1, _secId);
		state.OnSubscriptionOnline(1);

		var l1 = CreateL1(bidPrice: 100m, askPrice: 101m, bidVolume: 10m, askVolume: 5m);

		// First call - should return data
		var result1 = state.TryBuildDepth(1, l1, out _);
		IsNotNull(result1);

		// Second call with same data - should return null (no change)
		var l1Dup = CreateL1(bidPrice: 100m, askPrice: 101m, bidVolume: 10m, askVolume: 5m);
		var result2 = state.TryBuildDepth(1, l1Dup, out _);
		IsNull(result2);
	}

	[TestMethod]
	public void OnSubscriptionOnline_SharesBuilder()
	{
		var state = CreateState();

		state.AddSubscription(1, _secId);
		state.AddSubscription(2, _secId);

		state.OnSubscriptionOnline(1);
		state.OnSubscriptionOnline(2);

		var l1 = CreateL1(bidPrice: 100m, askPrice: 101m, bidVolume: 10m, askVolume: 5m);

		var result = state.TryBuildDepth(1, l1, out var subIds);

		IsNotNull(result);
		IsNotNull(subIds);
		subIds.Count(id => id == 1).AssertEqual(1);
		subIds.Count(id => id == 2).AssertEqual(1);
	}

	[TestMethod]
	public void RemoveSubscription_Removes()
	{
		var state = CreateState();

		state.AddSubscription(1, _secId);
		state.OnSubscriptionOnline(1);

		state.RemoveSubscription(1);

		IsFalse(state.ContainsSubscription(1));
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var state = CreateState();

		state.AddSubscription(1, _secId);
		state.AddSubscription(2, _secId);
		state.OnSubscriptionOnline(1);

		state.Clear();

		IsFalse(state.HasAnySubscriptions);
		IsFalse(state.ContainsSubscription(1));
		IsFalse(state.ContainsSubscription(2));
	}
}
