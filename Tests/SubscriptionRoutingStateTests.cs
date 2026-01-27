namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

[TestClass]
public class SubscriptionRoutingStateTests : BaseTestClass
{
	private SubscriptionRoutingState CreateState() => new();

	private static MarketDataMessage CreateSubMessage(long transactionId) => new()
	{
		TransactionId = transactionId,
		IsSubscribe = true,
		DataType2 = DataType.Ticks,
		SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
	};

	[TestMethod]
	public void AddSubscription_TryGet_ReturnsTrue()
	{
		var state = CreateState();
		var msg = CreateSubMessage(1);
		var adapter = new Mock<IMessageAdapter>().Object;
		var adapters = new[] { adapter };

		state.AddSubscription(1, msg, adapters, DataType.Ticks);

		var result = state.TryGetSubscription(1, out var outMsg, out var outAdapters, out var outDt);

		result.AssertTrue();
		outMsg.AssertNotNull();
		outAdapters.Length.AreEqual(1);
		outDt.AreEqual(DataType.Ticks);
	}

	[TestMethod]
	public void TryGetSubscription_NonExistent_ReturnsFalse()
	{
		var state = CreateState();

		var result = state.TryGetSubscription(999, out var msg, out var adapters, out var dt);

		result.AssertFalse();
		msg.AssertNull();
		adapters.AssertNull();
		dt.AssertNull();
	}

	[TestMethod]
	public void RemoveSubscription_Removes()
	{
		var state = CreateState();
		var msg = CreateSubMessage(1);
		var adapters = new[] { new Mock<IMessageAdapter>().Object };

		state.AddSubscription(1, msg, adapters, DataType.Ticks);

		state.RemoveSubscription(1).AssertTrue();
		state.TryGetSubscription(1, out _, out _, out _).AssertFalse();
	}

	[TestMethod]
	public void GetSubscribers_ReturnsMatchingIds()
	{
		var state = CreateState();
		var adapter = new Mock<IMessageAdapter>().Object;

		state.AddSubscription(1, CreateSubMessage(1), new[] { adapter }, DataType.Ticks);
		state.AddSubscription(2, CreateSubMessage(2), new[] { adapter }, DataType.Ticks);
		state.AddSubscription(3, CreateSubMessage(3), new[] { adapter }, DataType.Level1);

		var subscribers = state.GetSubscribers(DataType.Ticks);

		subscribers.Length.AreEqual(2);
		subscribers.Contains(1L).AssertTrue();
		subscribers.Contains(2L).AssertTrue();
		subscribers.Contains(3L).AssertFalse();
	}

	[TestMethod]
	public void AddRequest_TryGet_ReturnsTrue()
	{
		var state = CreateState();
		var msg = CreateSubMessage(10);
		var adapter = new Mock<IMessageAdapter>().Object;

		state.AddRequest(10, msg, adapter);

		var result = state.TryGetRequest(10, out var outMsg, out var outAdapter);

		result.AssertTrue();
		outMsg.AssertNotNull();
		outAdapter.AssertNotNull();
	}

	[TestMethod]
	public void RemoveRequest_TryGet_ReturnsFalse()
	{
		var state = CreateState();
		var msg = CreateSubMessage(10);
		var adapter = new Mock<IMessageAdapter>().Object;

		state.AddRequest(10, msg, adapter);
		state.RemoveRequest(10).AssertTrue();

		state.TryGetRequest(10, out _, out _).AssertFalse();
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var state = CreateState();
		var adapter = new Mock<IMessageAdapter>().Object;

		state.AddSubscription(1, CreateSubMessage(1), new[] { adapter }, DataType.Ticks);
		state.AddRequest(2, CreateSubMessage(2), adapter);

		state.Clear();

		state.TryGetSubscription(1, out _, out _, out _).AssertFalse();
		state.TryGetRequest(2, out _, out _).AssertFalse();
		state.GetSubscribers(DataType.Ticks).Length.AreEqual(0);
	}
}
