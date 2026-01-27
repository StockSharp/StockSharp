namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

[TestClass]
public class ParentChildMapTests : BaseTestClass
{
	private ParentChildMap CreateMap() => new();

	private static MarketDataMessage CreateParentMessage(long transactionId) => new()
	{
		TransactionId = transactionId,
		IsSubscribe = true,
		DataType2 = DataType.Ticks,
		SecurityId = new SecurityId { SecurityCode = "S", BoardCode = "B" },
	};

	[TestMethod]
	public void AddMapping_TryGetParent_ReturnsTrue()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter);

		var result = map.TryGetParent(1, out var parentId);

		result.AssertTrue();
		parentId.AreEqual(100L);
	}

	[TestMethod]
	public void TryGetParent_NonExistent_ReturnsFalse()
	{
		var map = CreateMap();

		map.TryGetParent(999, out _).AssertFalse();
	}

	[TestMethod]
	public void GetChild_ReturnsChildAdapters()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);

		// Children start as Stopped, so GetChild filters by IsActive() which excludes Stopped.
		// We need to activate them first via ProcessChildResponse with no error.
		map.ProcessChildResponse(1, null, out _, out _, out _);
		map.ProcessChildResponse(2, null, out _, out _, out _);

		var children = map.GetChild(100);

		children.Count.AreEqual(2);
		children.ContainsKey(1L).AssertTrue();
		children.ContainsKey(2L).AssertTrue();
	}

	[TestMethod]
	public void ProcessChildResponse_SingleChild_NeedsParentResponse()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter);

		var parentId = map.ProcessChildResponse(1, null, out var needParent, out var allError, out var innerErrors);

		parentId.AreEqual(100L);
		needParent.AssertTrue();
		allError.AssertFalse();
		innerErrors.Any().AssertFalse();
	}

	[TestMethod]
	public void ProcessChildResponse_MultipleChildren_WaitsForAll()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);

		// Only first child responds - should not need parent response yet
		var parentId = map.ProcessChildResponse(1, null, out var needParent, out _, out _);

		parentId.AreEqual(100L);
		needParent.AssertFalse();

		// Second child responds - now all have responded
		parentId = map.ProcessChildResponse(2, null, out needParent, out var allError, out _);

		parentId.AreEqual(100L);
		needParent.AssertTrue();
		allError.AssertFalse();
	}

	[TestMethod]
	public void ProcessChildOnline_AllOnline_NeedsParentResponse()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);

		// First must be activated via response before going online
		map.ProcessChildResponse(1, null, out _, out _, out _);
		map.ProcessChildResponse(2, null, out _, out _, out _);

		// First child online - second still active
		map.ProcessChildOnline(1, out var needParent);
		needParent.AssertFalse();

		// Second child online - all online now
		map.ProcessChildOnline(2, out needParent);
		needParent.AssertTrue();
	}

	[TestMethod]
	public void ProcessChildFinish_AllFinished_NeedsParentResponse()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);

		// Activate first
		map.ProcessChildResponse(1, null, out _, out _, out _);
		map.ProcessChildResponse(2, null, out _, out _, out _);

		// First child finishes
		map.ProcessChildFinish(1, out var needParent);
		needParent.AssertFalse();

		// Second child finishes - all done
		map.ProcessChildFinish(2, out needParent);
		needParent.AssertTrue();
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter);

		map.Clear();

		map.TryGetParent(1, out _).AssertFalse();
	}
}
