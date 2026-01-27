namespace StockSharp.Algo;

/// <summary>
/// Default implementation of <see cref="IHeartbeatManagerState"/>.
/// </summary>
public class HeartbeatManagerState : IHeartbeatManagerState
{
	/// <summary>
	/// Sentinel value indicating no connection state.
	/// </summary>
	public const ConnectionStates None = (ConnectionStates)(-1);

	/// <inheritdoc />
	public ConnectionStates CurrentState { get; set; } = None;

	/// <inheritdoc />
	public ConnectionStates PreviousState { get; set; } = None;

	/// <inheritdoc />
	public int ConnectingAttemptCount { get; set; }

	/// <inheritdoc />
	public TimeSpan ConnectionTimeOut { get; set; }

	/// <inheritdoc />
	public bool CanSendTime { get; set; }

	/// <inheritdoc />
	public bool IsFirstTimeConnect { get; set; } = true;

	/// <inheritdoc />
	public bool SuppressDisconnectError { get; set; }

	/// <inheritdoc />
	public void Reset()
	{
		CurrentState = None;
		PreviousState = None;
		ConnectingAttemptCount = 0;
		ConnectionTimeOut = default;
		CanSendTime = false;
		IsFirstTimeConnect = true;
		SuppressDisconnectError = false;
	}
}
