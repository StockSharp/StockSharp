namespace StockSharp.Tests;

[TestClass]
public class OrderBookTruncateManagerStateTests : BaseTestClass
{
	private static OrderBookTruncateManagerState CreateState() => new();

	[TestMethod]
	public void AddDepth_TryGetDepth_ReturnsCorrectDepth()
	{
		var state = CreateState();

		state.AddDepth(1, 10);

		AreEqual(10, state.TryGetDepth(1));
	}

	[TestMethod]
	public void TryGetDepth_NonExistent_ReturnsNull()
	{
		var state = CreateState();

		IsNull(state.TryGetDepth(999));
	}

	[TestMethod]
	public void RemoveDepth_Existing_ReturnsTrue()
	{
		var state = CreateState();

		state.AddDepth(1, 10);

		IsTrue(state.RemoveDepth(1));
	}

	[TestMethod]
	public void RemoveDepth_NonExistent_ReturnsFalse()
	{
		var state = CreateState();

		IsFalse(state.RemoveDepth(999));
	}

	[TestMethod]
	public void HasDepths_Empty_ReturnsFalse()
	{
		var state = CreateState();

		IsFalse(state.HasDepths);
	}

	[TestMethod]
	public void HasDepths_AfterAdd_ReturnsTrue()
	{
		var state = CreateState();

		state.AddDepth(1, 10);

		IsTrue(state.HasDepths);
	}

	[TestMethod]
	public void GroupByDepth_GroupsCorrectly()
	{
		var state = CreateState();

		state.AddDepth(1, 10);
		state.AddDepth(2, 20);
		state.AddDepth(3, 10);

		var groups = state.GroupByDepth([1, 2, 3, 4])
			.OrderBy(g => g.depth ?? int.MaxValue)
			.ToArray();

		AreEqual(3, groups.Length);

		// depth=10 with ids [1,3]
		AreEqual(10, groups[0].depth);
		CollectionAssert.AreEquivalent(new long[] { 1, 3 }, groups[0].ids);

		// depth=20 with ids [2]
		AreEqual(20, groups[1].depth);
		CollectionAssert.AreEquivalent(new long[] { 2 }, groups[1].ids);

		// depth=null with ids [4]
		IsNull(groups[2].depth);
		CollectionAssert.AreEquivalent(new long[] { 4 }, groups[2].ids);
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var state = CreateState();

		state.AddDepth(1, 10);
		state.AddDepth(2, 20);

		state.Clear();

		IsFalse(state.HasDepths);
		IsNull(state.TryGetDepth(1));
		IsNull(state.TryGetDepth(2));
	}
}
