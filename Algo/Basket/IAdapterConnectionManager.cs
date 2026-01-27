namespace StockSharp.Algo.Basket;

/// <summary>
/// Interface for managing adapter connections in basket adapter.
/// </summary>
public interface IAdapterConnectionManager
{
	/// <summary>
	/// Gets the current aggregated connection state.
	/// </summary>
	ConnectionStates CurrentState { get; }

	/// <summary>
	/// To call the ConnectMessage event when the first adapter connects.
	/// If <see langword="false"/>, waits for all adapters to connect.
	/// </summary>
	bool ConnectDisconnectEventOnFirstAdapter { get; set; }

	/// <summary>
	/// Processes adapter connect response.
	/// </summary>
	/// <param name="adapter">The adapter that connected.</param>
	/// <param name="error">Optional connection error.</param>
	/// <returns>Messages to send to outer handler (Connect/Disconnect if basket state changed).</returns>
	Message[] ProcessConnect(IMessageAdapter adapter, Exception error);

	/// <summary>
	/// Processes adapter disconnect response.
	/// </summary>
	/// <param name="adapter">The adapter that disconnected.</param>
	/// <param name="error">Optional disconnection error.</param>
	/// <returns>Messages to send to outer handler.</returns>
	Message[] ProcessDisconnect(IMessageAdapter adapter, Exception error);

	/// <summary>
	/// Initializes adapter state before connecting.
	/// </summary>
	void InitializeAdapter(IMessageAdapter adapter);

	/// <summary>
	/// Sets basket state to connecting.
	/// </summary>
	void BeginConnect();

	/// <summary>
	/// Sets basket state to disconnecting.
	/// </summary>
	void BeginDisconnect();

	/// <summary>
	/// Whether any adapter is still connecting.
	/// </summary>
	bool HasPendingAdapters { get; }

	/// <summary>
	/// Resets all adapter states.
	/// </summary>
	void Reset();

	/// <summary>
	/// Gets the underlying state storage.
	/// </summary>
	IAdapterConnectionState State { get; }
}
