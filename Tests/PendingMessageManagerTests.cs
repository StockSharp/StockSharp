namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

/// <summary>
/// Tests for <see cref="PendingMessageManager"/> and <see cref="PendingMessageState"/>.
/// </summary>
[TestClass]
public class PendingMessageManagerTests
{
	#region PendingMessageState Tests

	[TestMethod]
	public void State_AddAndGet_ReturnsMessages()
	{
		// Arrange
		var state = new PendingMessageState();
		var msg1 = new SecurityMessage();
		var msg2 = new SecurityMessage();

		// Act
		state.Add(msg1);
		state.Add(msg2);
		var messages = state.GetAndClear();

		// Assert
		messages.Length.AssertEqual(2);
	}

	[TestMethod]
	public void State_GetAndClear_ClearsState()
	{
		// Arrange
		var state = new PendingMessageState();
		state.Add(new SecurityMessage());
		state.Add(new SecurityMessage());

		// Act
		state.GetAndClear();
		var secondCall = state.GetAndClear();

		// Assert
		secondCall.Length.AssertEqual(0);
		state.Count.AssertEqual(0);
	}

	[TestMethod]
	public void State_Count_ReturnsCorrectCount()
	{
		// Arrange
		var state = new PendingMessageState();

		// Act & Assert
		state.Count.AssertEqual(0);

		state.Add(new SecurityMessage());
		state.Count.AssertEqual(1);

		state.Add(new SecurityMessage());
		state.Count.AssertEqual(2);

		state.Clear();
		state.Count.AssertEqual(0);
	}

	#endregion

	#region PendingMessageManager Tests

	[TestMethod]
	public void Manager_HasPendingAdapters_EnqueuesMessage()
	{
		// Arrange
		var state = new PendingMessageState();
		var manager = new PendingMessageManager(state);
		var message = new SecurityMessage();

		// Act - hasPendingAdapters=true means adapters are still connecting
		var enqueued = manager.TryEnqueue(message, ConnectionStates.Connecting, hasPendingAdapters: true, totalAdapters: 1);

		// Assert
		enqueued.AssertTrue();
		manager.HasPending.AssertTrue();
		state.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Manager_AllConnected_DoesNotEnqueue()
	{
		// Arrange
		var state = new PendingMessageState();
		var manager = new PendingMessageManager(state);
		var message = new SecurityMessage();

		// Act - no pending adapters, 1 total adapter = all connected
		var enqueued = manager.TryEnqueue(message, ConnectionStates.Connected, hasPendingAdapters: false, totalAdapters: 1);

		// Assert
		enqueued.AssertFalse();
		manager.HasPending.AssertFalse();
	}

	[TestMethod]
	public void Manager_NoAdapters_EnqueuesMessage()
	{
		// Arrange
		var state = new PendingMessageState();
		var manager = new PendingMessageManager(state);
		var message = new SecurityMessage();

		// Act - totalAdapters=0 means no adapters at all, should enqueue
		var enqueued = manager.TryEnqueue(message, ConnectionStates.Disconnected, hasPendingAdapters: false, totalAdapters: 0);

		// Assert
		enqueued.AssertTrue();
	}

	[TestMethod]
	public void Manager_DequeueAll_ReturnsAllAndClears()
	{
		// Arrange
		var state = new PendingMessageState();
		var manager = new PendingMessageManager(state);
		var msg1 = new SecurityMessage();
		var msg2 = new PortfolioMessage();

		manager.TryEnqueue(msg1, ConnectionStates.Connecting, hasPendingAdapters: true, totalAdapters: 1);
		manager.TryEnqueue(msg2, ConnectionStates.Connecting, hasPendingAdapters: true, totalAdapters: 1);

		// Act
		var messages = manager.DequeueAll();

		// Assert
		messages.Length.AssertEqual(2);
		manager.HasPending.AssertFalse();
	}

	[TestMethod]
	public void Manager_FullCycle_BuffersUntilConnected()
	{
		// Arrange
		var state = new PendingMessageState();
		var manager = new PendingMessageManager(state);

		// Enqueue while pending adapters exist
		var msg1 = new SecurityMessage { SecurityId = new SecurityId { SecurityCode = "AAPL" } };
		var msg2 = new MarketDataMessage { SecurityId = new SecurityId { SecurityCode = "GOOG" }, DataType2 = DataType.Ticks };

		manager.TryEnqueue(msg1, ConnectionStates.Disconnected, hasPendingAdapters: true, totalAdapters: 1).AssertTrue();
		manager.TryEnqueue(msg2, ConnectionStates.Connecting, hasPendingAdapters: true, totalAdapters: 1).AssertTrue();

		manager.HasPending.AssertTrue();

		// Simulate connected - messages should be dequeued
		var pending = manager.DequeueAll();

		// Assert
		pending.Length.AssertEqual(2);
		manager.HasPending.AssertFalse();
	}

	#endregion

	#region Mock State Tests

	[TestMethod]
	public void Manager_WithMockState_VerifiesAddCall()
	{
		// Arrange
		var mockState = new Mock<IPendingMessageState>();
		mockState.Setup(s => s.Count).Returns(0);

		var manager = new PendingMessageManager(mockState.Object);
		var message = new SecurityMessage();

		// Act
		manager.TryEnqueue(message, ConnectionStates.Connecting, hasPendingAdapters: true, totalAdapters: 1);

		// Assert
		mockState.Verify(s => s.Add(It.IsAny<Message>()), Times.Once);
	}

	#endregion
}
