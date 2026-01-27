namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

[TestClass]
public class AdapterConnectionStateTests : BaseTestClass
{
	private AdapterConnectionState CreateState() => new();

	[TestMethod]
	public void CurrentState_Initially_Disconnected()
	{
		var state = CreateState();

		state.CurrentState.AreEqual(ConnectionStates.Disconnected);
	}

	[TestMethod]
	public void SetAdapterState_TryGet_ReturnsTrue()
	{
		var state = CreateState();
		var adapter = new Mock<IMessageAdapter>().Object;
		var error = new InvalidOperationException("test");

		state.SetAdapterState(adapter, ConnectionStates.Failed, error);

		var result = state.TryGetAdapterState(adapter, out var outState, out var outError);

		result.AssertTrue();
		outState.AreEqual(ConnectionStates.Failed);
		outError.AssertNotNull();
		outError.Message.AreEqual("test");
	}

	[TestMethod]
	public void TryGetAdapterState_NonExistent_ReturnsFalse()
	{
		var state = CreateState();
		var adapter = new Mock<IMessageAdapter>().Object;

		state.TryGetAdapterState(adapter, out _, out _).AssertFalse();
	}

	[TestMethod]
	public void RemoveAdapter_Removes()
	{
		var state = CreateState();
		var adapter = new Mock<IMessageAdapter>().Object;

		state.SetAdapterState(adapter, ConnectionStates.Connected, null);

		state.RemoveAdapter(adapter).AssertTrue();
		state.TryGetAdapterState(adapter, out _, out _).AssertFalse();
	}

	[TestMethod]
	public void ConnectedCount_AfterConnecting()
	{
		var state = CreateState();
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;
		var adapter3 = new Mock<IMessageAdapter>().Object;

		state.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		state.SetAdapterState(adapter2, ConnectionStates.Connected, null);
		state.SetAdapterState(adapter3, ConnectionStates.Connecting, null);

		state.ConnectedCount.AreEqual(2);
	}

	[TestMethod]
	public void TotalCount_AfterAdding()
	{
		var state = CreateState();
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		state.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		state.SetAdapterState(adapter2, ConnectionStates.Disconnected, null);

		state.TotalCount.AreEqual(2);
	}

	[TestMethod]
	public void HasPendingAdapters_Connecting_ReturnsTrue()
	{
		var state = CreateState();
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		state.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		state.SetAdapterState(adapter2, ConnectionStates.Connecting, null);

		state.HasPendingAdapters.AssertTrue();
	}

	[TestMethod]
	public void AllFailed_WhenAllFailed_ReturnsTrue()
	{
		var state = CreateState();
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;

		state.SetAdapterState(adapter1, ConnectionStates.Failed, new Exception("err1"));
		state.SetAdapterState(adapter2, ConnectionStates.Failed, new Exception("err2"));

		state.AllFailed.AssertTrue();
	}

	[TestMethod]
	public void AllDisconnectedOrFailed_Mixed_ReturnsFalse()
	{
		var state = CreateState();
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;
		var adapter3 = new Mock<IMessageAdapter>().Object;

		state.SetAdapterState(adapter1, ConnectionStates.Disconnected, null);
		state.SetAdapterState(adapter2, ConnectionStates.Failed, new Exception("err"));
		state.SetAdapterState(adapter3, ConnectionStates.Connected, null);

		state.AllDisconnectedOrFailed.AssertFalse();
	}

	[TestMethod]
	public void GetErrors_ReturnsOnlyNonNull()
	{
		var state = CreateState();
		var adapter1 = new Mock<IMessageAdapter>().Object;
		var adapter2 = new Mock<IMessageAdapter>().Object;
		var adapter3 = new Mock<IMessageAdapter>().Object;

		state.SetAdapterState(adapter1, ConnectionStates.Failed, new Exception("err1"));
		state.SetAdapterState(adapter2, ConnectionStates.Connected, null);
		state.SetAdapterState(adapter3, ConnectionStates.Failed, new Exception("err2"));

		var errors = state.GetErrors();

		errors.Length.AreEqual(2);
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var state = CreateState();
		var adapter = new Mock<IMessageAdapter>().Object;

		state.SetAdapterState(adapter, ConnectionStates.Connected, null);
		state.CurrentState = ConnectionStates.Connected;

		state.Clear();

		state.TotalCount.AreEqual(0);
		state.ConnectedCount.AreEqual(0);
		state.CurrentState.AreEqual(ConnectionStates.Disconnected);
		state.TryGetAdapterState(adapter, out _, out _).AssertFalse();
	}
}
