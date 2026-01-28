namespace StockSharp.Algo.Basket;

/// <summary>
/// Interface for managing message routing in basket adapter.
/// Extracts routing logic from BasketMessageAdapter for testability.
/// </summary>
public interface IBasketRoutingManager
{
	/// <summary>
	/// Process an incoming message and determine routing decisions.
	/// </summary>
	/// <param name="message">The message to process.</param>
	/// <param name="adapterLookup">Function to resolve wrapper adapter from underlying adapter.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Routing result with decisions and any output messages.</returns>
	ValueTask<RoutingInResult> ProcessInMessageAsync(
		Message message,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken);

	/// <summary>
	/// Process an outgoing message from an inner adapter.
	/// </summary>
	/// <param name="innerAdapter">The adapter that sent the message.</param>
	/// <param name="message">The message from the adapter.</param>
	/// <param name="adapterLookup">Function to resolve wrapper adapter from underlying adapter.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Routing result with transformed message and any extra messages.</returns>
	ValueTask<RoutingOutResult> ProcessOutMessageAsync(
		IMessageAdapter innerAdapter,
		Message message,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken);

	/// <summary>
	/// Register adapter's supported message types after connection.
	/// </summary>
	/// <param name="adapter">The connected adapter (wrapper).</param>
	/// <param name="supportedTypes">The message types supported by the adapter.</param>
	void RegisterAdapterMessageTypes(IMessageAdapter adapter, IEnumerable<MessageTypes> supportedTypes);

	/// <summary>
	/// Unregister adapter's message types on disconnection.
	/// </summary>
	/// <param name="adapter">The disconnected adapter (wrapper).</param>
	/// <param name="supportedTypes">The message types to unregister.</param>
	void UnregisterAdapterMessageTypes(IMessageAdapter adapter, IEnumerable<MessageTypes> supportedTypes);

	/// <summary>
	/// Add order transaction to adapter mapping.
	/// </summary>
	/// <param name="transactionId">The order transaction ID.</param>
	/// <param name="adapter">The adapter that handles the order.</param>
	void AddOrderAdapter(long transactionId, IMessageAdapter adapter);

	/// <summary>
	/// Try get adapter for an order by its transaction ID.
	/// </summary>
	/// <param name="transactionId">The order transaction ID.</param>
	/// <param name="adapter">The adapter if found.</param>
	/// <returns>True if found.</returns>
	bool TryGetOrderAdapter(long transactionId, out IMessageAdapter adapter);

	/// <summary>
	/// Get subscriber IDs for a data type.
	/// </summary>
	/// <param name="dataType">The data type.</param>
	/// <returns>Array of subscription IDs.</returns>
	long[] GetSubscribers(DataType dataType);

	/// <summary>
	/// Reset all routing state.
	/// </summary>
	/// <param name="clearPending">Whether to clear pending messages.</param>
	void Reset(bool clearPending);

	/// <summary>
	/// Gets whether there are adapters still connecting.
	/// </summary>
	bool HasPendingAdapters { get; }

	/// <summary>
	/// Gets whether all adapters are disconnected or failed.
	/// </summary>
	bool AllDisconnectedOrFailed { get; }

	/// <summary>
	/// Gets the count of connected adapters.
	/// </summary>
	int ConnectedCount { get; }

	/// <summary>
	/// Gets the total count of adapters.
	/// </summary>
	int TotalCount { get; }

	/// <summary>
	/// Gets the underlying adapter router.
	/// </summary>
	IAdapterRouter Router { get; }

	/// <summary>
	/// Gets the subscription routing state.
	/// </summary>
	ISubscriptionRoutingState SubscriptionRouting { get; }

	/// <summary>
	/// Gets the parent-child mapping.
	/// </summary>
	IParentChildMap ParentChildMap { get; }

	/// <summary>
	/// Gets the pending message state.
	/// </summary>
	IPendingMessageState PendingState { get; }

	/// <summary>
	/// Gets the connection state.
	/// </summary>
	IAdapterConnectionState ConnectionState { get; }

	/// <summary>
	/// Gets the connection manager.
	/// </summary>
	IAdapterConnectionManager ConnectionManager { get; }
}
