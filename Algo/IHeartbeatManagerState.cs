namespace StockSharp.Algo;

/// <summary>
/// State storage for <see cref="HeartbeatMessageAdapter"/> reconnection logic.
/// </summary>
public interface IHeartbeatManagerState
{
	/// <summary>
	/// Current connection state.
	/// </summary>
	ConnectionStates CurrentState { get; set; }

	/// <summary>
	/// Previous connection state.
	/// </summary>
	ConnectionStates PreviousState { get; set; }

	/// <summary>
	/// Number of remaining reconnection attempts.
	/// </summary>
	int ConnectingAttemptCount { get; set; }

	/// <summary>
	/// Current connection timeout remaining.
	/// </summary>
	TimeSpan ConnectionTimeOut { get; set; }

	/// <summary>
	/// Whether the adapter can send time messages.
	/// </summary>
	bool CanSendTime { get; set; }

	/// <summary>
	/// Whether this is the first time connecting.
	/// </summary>
	bool IsFirstTimeConnect { get; set; }

	/// <summary>
	/// Whether to suppress disconnect error messages.
	/// </summary>
	bool SuppressDisconnectError { get; set; }

	/// <summary>
	/// Reset all state to initial values.
	/// </summary>
	void Reset();
}
