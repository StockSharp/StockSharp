namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

/// <summary>
/// Tests for <see cref="AdapterConnectionManager"/> and <see cref="AdapterConnectionState"/>.
/// </summary>
[TestClass]
public class AdapterConnectionManagerTests
{
	#region AdapterConnectionState Tests

	[TestMethod]
	public void State_SetAndGet_ReturnsCorrectState()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var adapter = CreateMockAdapter();

		// Act
		state.SetAdapterState(adapter, ConnectionStates.Connected, null);
		var found = state.TryGetAdapterState(adapter, out var connState, out var error);

		// Assert
		found.AssertTrue();
		connState.AssertEqual(ConnectionStates.Connected);
		error.AssertNull();
	}

	[TestMethod]
	public void State_SetWithError_ReturnsError()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var adapter = CreateMockAdapter();
		var expectedError = new Exception("Test error");

		// Act
		state.SetAdapterState(adapter, ConnectionStates.Failed, expectedError);
		state.TryGetAdapterState(adapter, out var connState, out var error);

		// Assert
		connState.AssertEqual(ConnectionStates.Failed);
		error.AssertSame(expectedError);
	}

	[TestMethod]
	public void State_TryGetNonExistent_ReturnsFalse()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var adapter = CreateMockAdapter();

		// Act
		var found = state.TryGetAdapterState(adapter, out _, out _);

		// Assert
		found.AssertFalse();
	}

	[TestMethod]
	public void State_Remove_RemovesAdapter()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var adapter = CreateMockAdapter();
		state.SetAdapterState(adapter, ConnectionStates.Connected, null);

		// Act
		var removed = state.RemoveAdapter(adapter);
		var found = state.TryGetAdapterState(adapter, out _, out _);

		// Assert
		removed.AssertTrue();
		found.AssertFalse();
	}

	[TestMethod]
	public void State_ConnectedCount_ReturnsCorrectCount()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var adapter1 = CreateMockAdapter();
		var adapter2 = CreateMockAdapter();
		var adapter3 = CreateMockAdapter();

		state.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		state.SetAdapterState(adapter2, ConnectionStates.Connecting, null);
		state.SetAdapterState(adapter3, ConnectionStates.Connected, null);

		// Act & Assert
		state.ConnectedCount.AssertEqual(2);
		state.TotalCount.AssertEqual(3);
	}

	[TestMethod]
	public void State_Clear_RemovesAllAndResetsCurrent()
	{
		// Arrange
		var state = new AdapterConnectionState();
		state.SetAdapterState(CreateMockAdapter(), ConnectionStates.Connected, null);
		state.CurrentState = ConnectionStates.Connected;

		// Act
		state.Clear();

		// Assert
		state.TotalCount.AssertEqual(0);
		state.CurrentState.AssertEqual(ConnectionStates.Disconnected);
	}

	#endregion

	#region AdapterConnectionManager Tests

	[TestMethod]
	public void Manager_FirstAdapterConnected_BasketBecomesConnected()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var manager = new AdapterConnectionManager(state);
		var adapter = CreateMockAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter);

		// Act
		var messages = manager.ProcessConnect(adapter, null);

		// Assert
		manager.CurrentState.AssertEqual(ConnectionStates.Connected);
		messages.Length.AssertEqual(1);
		messages[0].AssertOfType<ConnectMessage>();
		((ConnectMessage)messages[0]).Error.AssertNull();
	}

	[TestMethod]
	public void Manager_SecondAdapterConnected_NoMessage()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var manager = new AdapterConnectionManager(state);
		var adapter1 = CreateMockAdapter();
		var adapter2 = CreateMockAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);
		manager.ProcessConnect(adapter1, null);

		// Act
		var messages = manager.ProcessConnect(adapter2, null);

		// Assert
		manager.CurrentState.AssertEqual(ConnectionStates.Connected);
		messages.Length.AssertEqual(0); // No message for second connection
	}

	[TestMethod]
	public void Manager_AllAdaptersDisconnected_BasketBecomesDisconnected()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var manager = new AdapterConnectionManager(state);
		var adapter1 = CreateMockAdapter();
		var adapter2 = CreateMockAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);
		manager.ProcessConnect(adapter1, null);
		manager.ProcessConnect(adapter2, null);

		// Disconnect first
		manager.ProcessDisconnect(adapter1, null);
		manager.CurrentState.AssertEqual(ConnectionStates.Connected); // Still connected

		// Act - disconnect last
		var messages = manager.ProcessDisconnect(adapter2, null);

		// Assert
		manager.CurrentState.AssertEqual(ConnectionStates.Disconnected);
		messages.Length.AssertEqual(1);
		messages[0].AssertOfType<DisconnectMessage>();
	}

	[TestMethod]
	public void Manager_OneAdapterError_BasketStaysConnectedIfOtherConnected()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var manager = new AdapterConnectionManager(state);
		var adapter1 = CreateMockAdapter();
		var adapter2 = CreateMockAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);
		manager.ProcessConnect(adapter1, null); // First connects successfully

		// Act - second fails
		var messages = manager.ProcessConnect(adapter2, new Exception("Connection failed"));

		// Assert
		manager.CurrentState.AssertEqual(ConnectionStates.Connected); // Stays connected
		messages.Length.AssertEqual(0); // No disconnect message
	}

	[TestMethod]
	public void Manager_AllAdaptersFailed_BasketFails()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var manager = new AdapterConnectionManager(state);
		var adapter1 = CreateMockAdapter();
		var adapter2 = CreateMockAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter1);
		manager.InitializeAdapter(adapter2);

		// Both fail
		manager.ProcessConnect(adapter1, new Exception("Error 1"));
		var messages = manager.ProcessConnect(adapter2, new Exception("Error 2"));

		// Assert
		manager.CurrentState.AssertEqual(ConnectionStates.Failed);
		messages.Length.AssertEqual(1);
		var connectMsg = (ConnectMessage)messages[0];
		connectMsg.Error.AssertNotNull();
		connectMsg.Error.AssertOfType<AggregateException>();
	}

	[TestMethod]
	public void Manager_Reset_ClearsAllStates()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var manager = new AdapterConnectionManager(state);
		var adapter = CreateMockAdapter();

		manager.BeginConnect();
		manager.InitializeAdapter(adapter);
		manager.ProcessConnect(adapter, null);

		// Act
		manager.Reset();

		// Assert
		manager.CurrentState.AssertEqual(ConnectionStates.Disconnected);
		state.TotalCount.AssertEqual(0);
	}

	[TestMethod]
	public void Manager_FullConnectionCycle_ThreeAdapters()
	{
		// Arrange
		var state = new AdapterConnectionState();
		var manager = new AdapterConnectionManager(state);
		var adapters = new[] { CreateMockAdapter(), CreateMockAdapter(), CreateMockAdapter() };

		manager.BeginConnect();
		foreach (var a in adapters)
			manager.InitializeAdapter(a);

		// Connect first - basket connected
		var r1 = manager.ProcessConnect(adapters[0], null);
		r1.Length.AssertEqual(1);
		manager.CurrentState.AssertEqual(ConnectionStates.Connected);

		// Connect second - no message (already connected)
		var r2 = manager.ProcessConnect(adapters[1], null);
		r2.Length.AssertEqual(0);

		// Connect third - no message
		var r3 = manager.ProcessConnect(adapters[2], null);
		r3.Length.AssertEqual(0);

		// Disconnect first - still connected
		var r4 = manager.ProcessDisconnect(adapters[0], null);
		r4.Length.AssertEqual(0);
		manager.CurrentState.AssertEqual(ConnectionStates.Connected);

		// Disconnect second - still connected
		var r5 = manager.ProcessDisconnect(adapters[1], null);
		r5.Length.AssertEqual(0);

		// Disconnect last - basket disconnected
		var r6 = manager.ProcessDisconnect(adapters[2], null);
		r6.Length.AssertEqual(1);
		manager.CurrentState.AssertEqual(ConnectionStates.Disconnected);
	}

	#endregion

	#region Mock State Tests

	[TestMethod]
	public void Manager_WithMockState_VerifiesStateCalls()
	{
		// Arrange
		var mockState = new Mock<IAdapterConnectionState>();
		mockState.Setup(s => s.CurrentState).Returns(ConnectionStates.Connecting);
		mockState.Setup(s => s.GetAllStates()).Returns([]);

		var manager = new AdapterConnectionManager(mockState.Object);
		var adapter = CreateMockAdapter();

		// Act
		manager.ProcessConnect(adapter, null);

		// Assert
		mockState.Verify(s => s.SetAdapterState(adapter, ConnectionStates.Connected, null), Times.Once);
		mockState.VerifySet(s => s.CurrentState = ConnectionStates.Connected, Times.Once);
	}

	#endregion

	private static IMessageAdapter CreateMockAdapter()
	{
		var mock = new Mock<IMessageAdapter>();
		mock.Setup(a => a.ToString()).Returns($"MockAdapter-{Guid.NewGuid():N}");
		return mock.Object;
	}
}
