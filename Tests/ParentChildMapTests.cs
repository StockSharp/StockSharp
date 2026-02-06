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

	#region AddMapping / TryGetParent

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
	public void AddMapping_InvalidChildId_Throws()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter = new Mock<IMessageAdapter>().Object;

		Throws<ArgumentOutOfRangeException>(() => map.AddMapping(0, parentMsg, adapter));
		Throws<ArgumentOutOfRangeException>(() => map.AddMapping(-1, parentMsg, adapter));
	}

	[TestMethod]
	public void AddMapping_NullParentMsg_Throws()
	{
		var map = CreateMap();
		var adapter = new Mock<IMessageAdapter>().Object;

		Throws<ArgumentNullException>(() => map.AddMapping(1, null, adapter));
	}

	[TestMethod]
	public void AddMapping_NullAdapter_Throws()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);

		Throws<ArgumentNullException>(() => map.AddMapping(1, parentMsg, null));
	}

	[TestMethod]
	public void AddMapping_DuplicateChildId_Throws()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter);

		Throws<ArgumentException>(() => map.AddMapping(1, parentMsg, adapter));
	}

	#endregion

	#region GetChild

	[TestMethod]
	public void GetChild_ReturnsActiveChildAdapters()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);

		// Children start as Stopped — GetChild filters by IsActive()
		map.ProcessChildResponse(1, null, out _, out _, out _);
		map.ProcessChildResponse(2, null, out _, out _, out _);

		var children = map.GetChild(100);

		children.Count.AreEqual(2);
		children.ContainsKey(1L).AssertTrue();
		children.ContainsKey(2L).AssertTrue();
		children[1L].AssertEqual(adapter1);
		children[2L].AssertEqual(adapter2);
	}

	[TestMethod]
	public void GetChild_BeforeActivation_ReturnsEmpty()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter);

		// No ProcessChildResponse called — child is still Stopped
		var children = map.GetChild(100);

		children.Count.AreEqual(0);
	}

	[TestMethod]
	public void GetChild_ErrorChild_NotReturned()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);

		// Child 1 succeeds, child 2 fails
		map.ProcessChildResponse(1, null, out _, out _, out _);
		map.ProcessChildResponse(2, new Exception("fail"), out _, out _, out _);

		var children = map.GetChild(100);

		// Error child should NOT be active
		children.Count.AreEqual(1);
		children.ContainsKey(1L).AssertTrue();
		children.ContainsKey(2L).AssertFalse();
	}

	[TestMethod]
	public void GetChild_NonExistentParent_ReturnsEmpty()
	{
		var map = CreateMap();

		var children = map.GetChild(999);

		children.Count.AreEqual(0);
	}

	#endregion

	#region ProcessChildResponse — Single Child

	[TestMethod]
	public void ProcessChildResponse_SingleChild_Success_NeedsParentResponse()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter);

		var parentId = map.ProcessChildResponse(1, null, out var needParent, out var allError, out var innerErrors);

		parentId.AreEqual(100L);
		needParent.AssertTrue();
		allError.AssertFalse();
		innerErrors.Count().AssertEqual(0);
	}

	[TestMethod]
	public void ProcessChildResponse_SingleChild_Error_AllErrorTrue()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter);

		var error = new InvalidOperationException("connection failed");
		var parentId = map.ProcessChildResponse(1, error, out var needParent, out var allError, out var innerErrors);

		parentId.AreEqual(100L);
		needParent.AssertTrue();
		allError.AssertTrue();
		var errors = innerErrors.ToList();
		errors.Count.AssertEqual(1);
		errors[0].AssertEqual(error);
	}

	[TestMethod]
	public void ProcessChildResponse_UnknownChildId_ReturnsNull()
	{
		var map = CreateMap();

		var parentId = map.ProcessChildResponse(999, null, out var needParent, out _, out _);

		parentId.AssertNull();
	}

	[TestMethod]
	public void ProcessChildResponse_ZeroChildId_ReturnsNull()
	{
		var map = CreateMap();

		var parentId = map.ProcessChildResponse(0, null, out _, out _, out _);

		parentId.AssertNull();
	}

	#endregion

	#region ProcessChildResponse — Multiple Children

	[TestMethod]
	public void ProcessChildResponse_MultipleChildren_WaitsForAll()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);

		// Only first child responds
		var parentId = map.ProcessChildResponse(1, null, out var needParent, out _, out _);

		parentId.AreEqual(100L);
		needParent.AssertFalse();

		// Second child responds — now all have responded
		parentId = map.ProcessChildResponse(2, null, out needParent, out var allError, out var innerErrors);

		parentId.AreEqual(100L);
		needParent.AssertTrue();
		allError.AssertFalse();
		innerErrors.Count().AssertEqual(0);
	}

	[TestMethod]
	public void ProcessChildResponse_MixedResults_OneErrorOneSuccess_AllErrorFalse()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);

		// Child 1 fails
		var error = new Exception("adapter1 failed");
		map.ProcessChildResponse(1, error, out var needParent1, out _, out _);
		needParent1.AssertFalse(); // still waiting for child 2

		// Child 2 succeeds
		map.ProcessChildResponse(2, null, out var needParent2, out var allError, out var innerErrors);

		needParent2.AssertTrue();
		allError.AssertFalse(); // at least one child succeeded
		var errors = innerErrors.ToList();
		errors.Count.AssertEqual(1);
		errors[0].AssertEqual(error);
	}

	[TestMethod]
	public void ProcessChildResponse_AllErrors_AllErrorTrue()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;
		var adapter3 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);
		map.AddMapping(3, parentMsg, adapter3);

		var err1 = new Exception("error 1");
		var err2 = new Exception("error 2");
		var err3 = new Exception("error 3");

		map.ProcessChildResponse(1, err1, out var n1, out _, out _);
		n1.AssertFalse();

		map.ProcessChildResponse(2, err2, out var n2, out _, out _);
		n2.AssertFalse();

		map.ProcessChildResponse(3, err3, out var n3, out var allError, out var innerErrors);
		n3.AssertTrue();
		allError.AssertTrue();

		var errors = innerErrors.ToList();
		errors.Count.AssertEqual(3);
		errors.AssertContains(err1);
		errors.AssertContains(err2);
		errors.AssertContains(err3);
	}

	[TestMethod]
	public void ProcessChildResponse_ThreeChildren_TwoErrorOneSuccess_AllErrorFalse()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;
		var adapter3 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);
		map.AddMapping(3, parentMsg, adapter3);

		map.ProcessChildResponse(1, new Exception("err1"), out _, out _, out _);
		map.ProcessChildResponse(2, null, out _, out _, out _); // success!
		map.ProcessChildResponse(3, new Exception("err3"), out var needParent, out var allError, out var innerErrors);

		needParent.AssertTrue();
		allError.AssertFalse(); // child 2 succeeded
		innerErrors.Count().AssertEqual(2);
	}

	#endregion

	#region ProcessChildOnline

	[TestMethod]
	public void ProcessChildOnline_AllOnline_NeedsParentResponse()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);

		// First must be activated
		map.ProcessChildResponse(1, null, out _, out _, out _);
		map.ProcessChildResponse(2, null, out _, out _, out _);

		// First child online
		map.ProcessChildOnline(1, out var needParent);
		needParent.AssertFalse();

		// Second child online — all online now
		map.ProcessChildOnline(2, out needParent);
		needParent.AssertTrue();
	}

	[TestMethod]
	public void ProcessChildOnline_UnknownChildId_ReturnsNull()
	{
		var map = CreateMap();

		var parentId = map.ProcessChildOnline(999, out _);
		parentId.AssertNull();
	}

	[TestMethod]
	public void ProcessChildOnline_OneErrorOneOnline_NeedsParentResponse()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);

		// Child 1 responds with error, child 2 responds OK
		map.ProcessChildResponse(1, new Exception("fail"), out _, out _, out _);
		map.ProcessChildResponse(2, null, out _, out _, out _);

		// Child 2 goes online — error child is in Error state (not Online, but t.Second == Error is checked first)
		map.ProcessChildOnline(2, out var needParent);

		// Error state is treated as "done" — needParent should be true
		needParent.AssertTrue();
	}

	#endregion

	#region ProcessChildFinish

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

		// Second child finishes — all done
		map.ProcessChildFinish(2, out needParent);
		needParent.AssertTrue();
	}

	[TestMethod]
	public void ProcessChildFinish_UnknownChildId_ReturnsNull()
	{
		var map = CreateMap();

		var parentId = map.ProcessChildFinish(999, out _);
		parentId.AssertNull();
	}

	[TestMethod]
	public void ProcessChildFinish_OneErrorOneFinish_NeedsParentResponse()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter1);
		map.AddMapping(2, parentMsg, adapter2);

		// Child 1 errors
		map.ProcessChildResponse(1, new Exception("fail"), out _, out _, out _);
		// Child 2 succeeds
		map.ProcessChildResponse(2, null, out _, out _, out _);

		// Child 2 finishes — error child is in Error state, should be treated as "done"
		map.ProcessChildFinish(2, out var needParent);
		needParent.AssertTrue();
	}

	#endregion

	#region Clear

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var adapter = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, adapter);

		map.Clear();

		map.TryGetParent(1, out _).AssertFalse();
		map.GetChild(100).Count.AreEqual(0);
	}

	#endregion

	#region Full Broadcast Scenario

	[TestMethod]
	public void FullBroadcast_ThreeAdapters_AllSucceed_AggregatesCorrectly()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var a1 = new Mock<IMessageAdapter>().Object;
		var a2 = new Mock<IMessageAdapter>().Object;
		var a3 = new Mock<IMessageAdapter>().Object;

		// Parent broadcast creates 3 children
		map.AddMapping(1, parentMsg, a1);
		map.AddMapping(2, parentMsg, a2);
		map.AddMapping(3, parentMsg, a3);

		// All respond OK
		map.ProcessChildResponse(1, null, out var n1, out _, out _);
		n1.AssertFalse();
		map.ProcessChildResponse(2, null, out var n2, out _, out _);
		n2.AssertFalse();
		map.ProcessChildResponse(3, null, out var n3, out var allError, out _);
		n3.AssertTrue();
		allError.AssertFalse();

		// All go online
		map.ProcessChildOnline(1, out var o1);
		o1.AssertFalse();
		map.ProcessChildOnline(2, out var o2);
		o2.AssertFalse();
		map.ProcessChildOnline(3, out var o3);
		o3.AssertTrue();

		// Verify all children are active
		var children = map.GetChild(100);
		children.Count.AreEqual(3);

		// All finish
		map.ProcessChildFinish(1, out var f1);
		f1.AssertFalse();
		map.ProcessChildFinish(2, out var f2);
		f2.AssertFalse();
		map.ProcessChildFinish(3, out var f3);
		f3.AssertTrue();
	}

	[TestMethod]
	public void FullBroadcast_OneAdapterFails_ParentStillSucceeds()
	{
		var map = CreateMap();
		var parentMsg = CreateParentMessage(100);
		var a1 = new Mock<IMessageAdapter>().Object;
		var a2 = new Mock<IMessageAdapter>().Object;
		var a3 = new Mock<IMessageAdapter>().Object;

		map.AddMapping(1, parentMsg, a1);
		map.AddMapping(2, parentMsg, a2);
		map.AddMapping(3, parentMsg, a3);

		// Adapter 1 fails, adapters 2 and 3 succeed
		map.ProcessChildResponse(1, new Exception("adapter1 down"), out _, out _, out _);
		map.ProcessChildResponse(2, null, out _, out _, out _);
		map.ProcessChildResponse(3, null, out var needParent, out var allError, out var innerErrors);

		needParent.AssertTrue();
		allError.AssertFalse(); // at least two succeeded
		innerErrors.Count().AssertEqual(1);

		// Only active children go online
		map.ProcessChildOnline(2, out var o2);
		o2.AssertFalse();
		map.ProcessChildOnline(3, out var o3);
		o3.AssertTrue(); // error child doesn't need to go online

		// Only 2 active children
		var children = map.GetChild(100);
		children.Count.AreEqual(2);
		children.ContainsKey(1L).AssertFalse(); // failed adapter not active
	}

	#endregion
}
