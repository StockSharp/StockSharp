namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

[TestClass]
public class AdapterConnectionManagerTests : BaseTestClass
{
	private static AdapterConnectionManager CreateManager(out AdapterConnectionState state)
	{
		state = new AdapterConnectionState();
		return new AdapterConnectionManager(state);
	}

	private static IMessageAdapter CreateAdapter()
	{
		var mock = new Mock<IMessageAdapter>();
		mock.Setup(a => a.ToString()).Returns($"MockAdapter-{Guid.NewGuid():N}");
		return mock.Object;
	}

	[TestMethod]
	public void CurrentState_Initially_Disconnected()
	{
		var manager = CreateManager(out _);

		AreEqual(ConnectionStates.Disconnected, manager.CurrentState);
	}

	[TestMethod]
	public void BeginConnect_SetsConnecting()
	{
		var manager = CreateManager(out _);

		manager.BeginConnect();

		AreEqual(ConnectionStates.Connecting, manager.CurrentState);
	}

	[TestMethod]
	public void ProcessConnect_FirstAdapter_OnFirst_ReturnsConnectMessage()
	{
		var manager = CreateManager(out _);
		var adapter = CreateAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter);

		var messages = manager.ProcessConnect(adapter, null);

		AreEqual(ConnectionStates.Connected, manager.CurrentState);
		AreEqual(1, messages.Length);
		IsTrue(messages[0] is ConnectMessage);
		IsNull(((ConnectMessage)messages[0]).Error);
	}

	[TestMethod]
	public void ProcessConnect_SecondAdapter_NoMessage()
	{
		var manager = CreateManager(out _);
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);
		manager.ProcessConnect(adapter1, null);

		var messages = manager.ProcessConnect(adapter2, null);

		AreEqual(ConnectionStates.Connected, manager.CurrentState);
		AreEqual(0, messages.Length);
	}

	[TestMethod]
	public void ProcessConnect_WaitAll_DoesNotReturnUntilAllConnected()
	{
		var manager = CreateManager(out _);
		manager.ConnectDisconnectEventOnFirstAdapter = false;

		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);
		manager.BeginConnect();

		var result1 = manager.ProcessConnect(adapter1, null);
		AreEqual(0, result1.Length);
		AreEqual(ConnectionStates.Connecting, manager.CurrentState);

		var result2 = manager.ProcessConnect(adapter2, null);
		AreEqual(1, result2.Length);
		IsTrue(result2[0] is ConnectMessage);
		AreEqual(ConnectionStates.Connected, manager.CurrentState);
	}

	[TestMethod]
	public void ProcessConnect_OneAdapterError_BasketStaysConnected()
	{
		var manager = CreateManager(out _);
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);
		manager.ProcessConnect(adapter1, null);

		var messages = manager.ProcessConnect(adapter2, new Exception("Connection failed"));

		AreEqual(ConnectionStates.Connected, manager.CurrentState);
		AreEqual(0, messages.Length);
	}

	[TestMethod]
	public void ProcessConnect_MixedResults_FailThenSuccess()
	{
		var manager = CreateManager(out _);
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);
		manager.BeginConnect();

		var result1 = manager.ProcessConnect(adapter1, new InvalidOperationException("connection failed"));
		AreEqual(0, result1.Length);

		var result2 = manager.ProcessConnect(adapter2, null);
		AreEqual(1, result2.Length);
		IsTrue(result2[0] is ConnectMessage);
		AreEqual(ConnectionStates.Connected, manager.CurrentState);
	}

	[TestMethod]
	public void ProcessConnect_AllFailed_BasketFails()
	{
		var manager = CreateManager(out _);
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);

		manager.ProcessConnect(adapter1, new Exception("Error 1"));
		var messages = manager.ProcessConnect(adapter2, new Exception("Error 2"));

		AreEqual(ConnectionStates.Failed, manager.CurrentState);
		AreEqual(1, messages.Length);
		var connectMsg = (ConnectMessage)messages[0];
		IsNotNull(connectMsg.Error);
		IsTrue(connectMsg.Error is AggregateException);
	}

	[TestMethod]
	public void ProcessDisconnect_AllDisconnected_ReturnsDisconnectMessage()
	{
		var manager = CreateManager(out _);
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);
		manager.ProcessConnect(adapter1, null);
		manager.ProcessConnect(adapter2, null);

		manager.ProcessDisconnect(adapter1, null);
		AreEqual(ConnectionStates.Connected, manager.CurrentState);

		var messages = manager.ProcessDisconnect(adapter2, null);

		AreEqual(ConnectionStates.Disconnected, manager.CurrentState);
		AreEqual(1, messages.Length);
		IsTrue(messages[0] is DisconnectMessage);
	}

	[TestMethod]
	public void FullConnectionCycle_ThreeAdapters()
	{
		var manager = CreateManager(out _);
		var adapters = new[] { CreateAdapter(), CreateAdapter(), CreateAdapter() };

		manager.BeginConnect();
		foreach (var a in adapters)
			manager.InitializeAdapter(a);

		var r1 = manager.ProcessConnect(adapters[0], null);
		AreEqual(1, r1.Length);
		AreEqual(ConnectionStates.Connected, manager.CurrentState);

		var r2 = manager.ProcessConnect(adapters[1], null);
		AreEqual(0, r2.Length);

		var r3 = manager.ProcessConnect(adapters[2], null);
		AreEqual(0, r3.Length);

		var r4 = manager.ProcessDisconnect(adapters[0], null);
		AreEqual(0, r4.Length);
		AreEqual(ConnectionStates.Connected, manager.CurrentState);

		var r5 = manager.ProcessDisconnect(adapters[1], null);
		AreEqual(0, r5.Length);

		var r6 = manager.ProcessDisconnect(adapters[2], null);
		AreEqual(1, r6.Length);
		AreEqual(ConnectionStates.Disconnected, manager.CurrentState);
	}

	[TestMethod]
	public void Reset_ClearsState()
	{
		var manager = CreateManager(out var state);
		var adapter = CreateAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter);
		manager.ProcessConnect(adapter, null);

		AreEqual(ConnectionStates.Connected, manager.CurrentState);
		AreEqual(1, state.TotalCount);

		manager.Reset();

		AreEqual(ConnectionStates.Disconnected, manager.CurrentState);
		AreEqual(0, state.TotalCount);
	}

	[TestMethod]
	public void WithMockState_VerifiesStateCalls()
	{
		var mockState = new Mock<IAdapterConnectionState>();
		mockState.Setup(s => s.CurrentState).Returns(ConnectionStates.Connecting);
		mockState.Setup(s => s.GetAllStates()).Returns([]);

		var manager = new AdapterConnectionManager(mockState.Object);
		var adapter = CreateAdapter();

		manager.ProcessConnect(adapter, null);

		mockState.Verify(s => s.SetAdapterState(adapter, ConnectionStates.Connected, null), Times.Once);
		mockState.VerifySet(s => s.CurrentState = ConnectionStates.Connected, Times.Once);
	}

	[TestMethod]
	public void BeginDisconnect_SetsDisconnecting()
	{
		var manager = CreateManager(out _);
		var adapter = CreateAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter);
		manager.ProcessConnect(adapter, null);

		AreEqual(ConnectionStates.Connected, manager.CurrentState);

		manager.BeginDisconnect();

		AreEqual(ConnectionStates.Disconnecting, manager.CurrentState);
	}

	[TestMethod]
	public void HasPendingAdapters_WhenConnecting_ReturnsTrue()
	{
		var manager = CreateManager(out _);
		var adapter = CreateAdapter();

		IsFalse(manager.HasPendingAdapters);

		manager.BeginConnect();
		manager.InitializeAdapter(adapter);

		IsTrue(manager.HasPendingAdapters);
	}

	[TestMethod]
	public void HasPendingAdapters_WhenConnected_ReturnsFalse()
	{
		var manager = CreateManager(out _);
		var adapter = CreateAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter);
		manager.ProcessConnect(adapter, null);

		IsFalse(manager.HasPendingAdapters);
	}

	[TestMethod]
	public void HasPendingAdapters_MultipleAdapters_TrueUntilAllConnected()
	{
		var manager = CreateManager(out _);
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);

		IsTrue(manager.HasPendingAdapters);

		manager.ProcessConnect(adapter1, null);

		// Still has pending - adapter2 not connected yet
		IsTrue(manager.HasPendingAdapters);

		manager.ProcessConnect(adapter2, null);

		IsFalse(manager.HasPendingAdapters);
	}

	/// <summary>
	/// BUG: With ConnectDisconnectEventOnFirstAdapter=false, when one adapter connects successfully
	/// and the other fails, no ConnectMessage is ever emitted. The basket stays in Connecting state.
	/// </summary>
	[TestMethod]
	public void ProcessConnect_WaitAll_SuccessThenFail_ShouldEmitConnect()
	{
		var manager = CreateManager(out _);
		manager.ConnectDisconnectEventOnFirstAdapter = false;

		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);
		manager.BeginConnect();

		// First adapter connects successfully
		var result1 = manager.ProcessConnect(adapter1, null);
		AreEqual(0, result1.Length, "Still waiting for adapter2");
		AreEqual(ConnectionStates.Connecting, manager.CurrentState);

		// Second adapter fails
		var result2 = manager.ProcessConnect(adapter2, new Exception("Connection failed"));

		// All adapters have responded. At least one succeeded.
		// Expected: ConnectMessage should be emitted, basket should be Connected.
		// BUG: result2 is empty, basket stays in Connecting state forever.
		AreEqual(ConnectionStates.Connected, manager.CurrentState,
			"Should transition to Connected since at least one adapter succeeded");
		AreEqual(1, result2.Length,
			"Should emit ConnectMessage when all adapters responded and at least one succeeded");
		IsTrue(result2[0] is ConnectMessage);
	}
}
