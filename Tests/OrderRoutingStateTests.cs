namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

[TestClass]
public class OrderRoutingStateTests : BaseTestClass
{
	[TestMethod]
	public void TryAddOrderAdapter_TryGet_ReturnsTrue()
	{
		var state = new OrderRoutingState();
		var adapter = new Mock<IMessageAdapter>().Object;

		state.TryAddOrderAdapter(1, adapter);

		var found = state.TryGetOrderAdapter(1, out var result);

		found.AssertTrue();
		result.AssertEqual(adapter);
	}

	[TestMethod]
	public void TryGetOrderAdapter_NonExistent_ReturnsFalse()
	{
		var state = new OrderRoutingState();

		state.TryGetOrderAdapter(999, out var result).AssertFalse();
		result.AssertNull();
	}

	[TestMethod]
	public void TryAddOrderAdapter_DuplicateId_DoesNotThrow()
	{
		var state = new OrderRoutingState();
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		state.TryAddOrderAdapter(1, adapter1);
		state.TryAddOrderAdapter(1, adapter2);

		// should still return the first adapter (TryAdd semantics)
		state.TryGetOrderAdapter(1, out var result).AssertTrue();
		result.AssertEqual(adapter1);
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var state = new OrderRoutingState();
		var adapter = new Mock<IMessageAdapter>().Object;

		state.TryAddOrderAdapter(1, adapter);
		state.TryAddOrderAdapter(2, adapter);

		state.Clear();

		state.TryGetOrderAdapter(1, out _).AssertFalse();
		state.TryGetOrderAdapter(2, out _).AssertFalse();
	}
}
