namespace StockSharp.Tests;

[TestClass]
public class HeartbeatManagerStateTests : BaseTestClass
{
	private static HeartbeatManagerState CreateState() => new();

	[TestMethod]
	public void DefaultValues_AreCorrect()
	{
		var state = CreateState();

		AreEqual(HeartbeatManagerState.None, state.CurrentState);
		AreEqual(HeartbeatManagerState.None, state.PreviousState);
		AreEqual(0, state.ConnectingAttemptCount);
		AreEqual(TimeSpan.Zero, state.ConnectionTimeOut);
		IsFalse(state.CanSendTime);
		IsTrue(state.IsFirstTimeConnect);
		IsFalse(state.SuppressDisconnectError);
	}

	[TestMethod]
	public void SetCurrentState_GetsCorrectValue()
	{
		var state = CreateState();

		state.CurrentState = ConnectionStates.Connected;

		AreEqual(ConnectionStates.Connected, state.CurrentState);
	}

	[TestMethod]
	public void SetPreviousState_GetsCorrectValue()
	{
		var state = CreateState();

		state.PreviousState = ConnectionStates.Disconnecting;

		AreEqual(ConnectionStates.Disconnecting, state.PreviousState);
	}

	[TestMethod]
	public void SetConnectingAttemptCount_GetsCorrectValue()
	{
		var state = CreateState();

		state.ConnectingAttemptCount = 5;

		AreEqual(5, state.ConnectingAttemptCount);
	}

	[TestMethod]
	public void SetConnectionTimeOut_GetsCorrectValue()
	{
		var state = CreateState();

		state.ConnectionTimeOut = TimeSpan.FromSeconds(30);

		AreEqual(TimeSpan.FromSeconds(30), state.ConnectionTimeOut);
	}

	[TestMethod]
	public void SetCanSendTime_GetsCorrectValue()
	{
		var state = CreateState();

		state.CanSendTime = true;

		IsTrue(state.CanSendTime);
	}

	[TestMethod]
	public void Reset_RestoresDefaults()
	{
		var state = CreateState();

		// Set all properties to non-default values
		state.CurrentState = ConnectionStates.Connected;
		state.PreviousState = ConnectionStates.Disconnecting;
		state.ConnectingAttemptCount = 5;
		state.ConnectionTimeOut = TimeSpan.FromSeconds(30);
		state.CanSendTime = true;
		state.IsFirstTimeConnect = false;
		state.SuppressDisconnectError = true;

		state.Reset();

		AreEqual(HeartbeatManagerState.None, state.CurrentState);
		AreEqual(HeartbeatManagerState.None, state.PreviousState);
		AreEqual(0, state.ConnectingAttemptCount);
		AreEqual(TimeSpan.Zero, state.ConnectionTimeOut);
		IsFalse(state.CanSendTime);
		IsTrue(state.IsFirstTimeConnect);
		IsFalse(state.SuppressDisconnectError);
	}
}
