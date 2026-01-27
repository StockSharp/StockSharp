namespace StockSharp.Algo.Basket;

/// <summary>
/// Interface for managing adapter connection states.
/// </summary>
public interface IAdapterConnectionState
{
	/// <summary>
	/// Sets the state for the specified adapter.
	/// </summary>
	void SetAdapterState(IMessageAdapter adapter, ConnectionStates state, Exception error);

	/// <summary>
	/// Tries to get the state for the specified adapter.
	/// </summary>
	bool TryGetAdapterState(IMessageAdapter adapter, out ConnectionStates state, out Exception error);

	/// <summary>
	/// Removes the state for the specified adapter.
	/// </summary>
	bool RemoveAdapter(IMessageAdapter adapter);

	/// <summary>
	/// Gets or sets the aggregated basket connection state.
	/// </summary>
	ConnectionStates CurrentState { get; set; }

	/// <summary>
	/// Gets all adapter states.
	/// </summary>
	IEnumerable<(IMessageAdapter adapter, ConnectionStates state, Exception error)> GetAllStates();

	/// <summary>
	/// Gets the count of connected adapters.
	/// </summary>
	int ConnectedCount { get; }

	/// <summary>
	/// Gets the total count of adapters.
	/// </summary>
	int TotalCount { get; }

	/// <summary>
	/// Whether any adapter is still in Connecting state.
	/// </summary>
	bool HasPendingAdapters { get; }

	/// <summary>
	/// Whether all adapters are in Failed state.
	/// </summary>
	bool AllFailed { get; }

	/// <summary>
	/// Whether all adapters are Disconnected or Failed.
	/// </summary>
	bool AllDisconnectedOrFailed { get; }

	/// <summary>
	/// Get all non-null errors from adapter states.
	/// </summary>
	Exception[] GetErrors();

	/// <summary>
	/// Clears all adapter states and resets current state to Disconnected.
	/// </summary>
	void Clear();
}
