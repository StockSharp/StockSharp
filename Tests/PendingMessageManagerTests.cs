namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

/// <summary>
/// Tests for <see cref="PendingMessageState"/>.
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
}
