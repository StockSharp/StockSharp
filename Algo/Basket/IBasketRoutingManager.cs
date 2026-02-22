namespace StockSharp.Algo.Basket;

/// <summary>
/// Interface for managing message routing in basket adapter.
/// Encapsulates all routing, connection, and subscription logic.
/// </summary>
public interface IBasketRoutingManager
{
	#region Message Processing

	/// <summary>
	/// Process an incoming message and determine routing decisions.
	/// </summary>
	ValueTask<RoutingInResult> ProcessInMessageAsync(
		Message message,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken);

	/// <summary>
	/// Process an outgoing message from an inner adapter.
	/// </summary>
	ValueTask<RoutingOutResult> ProcessOutMessageAsync(
		IMessageAdapter innerAdapter,
		Message message,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken);

	/// <summary>
	/// Process a back message with subscription tracking.
	/// </summary>
	void ProcessBackMessage(IMessageAdapter adapter, ISubscriptionMessage subscrMsg, Func<IMessageAdapter, IMessageAdapter> adapterLookup);

	#endregion

	#region Connection Management

	/// <summary>
	/// Gets or sets whether to fire connect/disconnect event on first adapter.
	/// </summary>
	bool ConnectDisconnectEventOnFirstAdapter { get; set; }

	/// <summary>
	/// Gets whether there are adapters still connecting.
	/// </summary>
	bool HasPendingAdapters { get; }

	/// <summary>
	/// Gets the count of connected adapters.
	/// </summary>
	int ConnectedCount { get; }

	/// <summary>
	/// Begin connection process.
	/// </summary>
	void BeginConnect();

	/// <summary>
	/// Initialize adapter for connection.
	/// </summary>
	void InitializeAdapter(IMessageAdapter adapter);

	/// <summary>
	/// Process successful or failed connection.
	/// </summary>
	/// <param name="adapter">The adapter that connected.</param>
	/// <param name="wrapper">The wrapper for the adapter.</param>
	/// <param name="supportedMessages">Supported message types.</param>
	/// <param name="error">Connection error if any.</param>
	/// <returns>Extra messages to send out and pending messages to loop back.</returns>
	(IEnumerable<Message> outMessages, Message[] pendingToLoopback, Message[] notSupportedMsgs) ProcessConnect(
		IMessageAdapter adapter,
		IMessageAdapter wrapper,
		IEnumerable<MessageTypes> supportedMessages,
		Exception error);

	/// <summary>
	/// Begin disconnection process.
	/// </summary>
	void BeginDisconnect();

	/// <summary>
	/// Get adapters that need to be disconnected.
	/// </summary>
	/// <param name="adapterLookup">Function to get wrapper from underlying adapter.</param>
	/// <returns>Dictionary of wrapper to underlying adapter.</returns>
	IDictionary<IMessageAdapter, IMessageAdapter> GetAdaptersToDisconnect(Func<IMessageAdapter, IMessageAdapter> adapterLookup);

	/// <summary>
	/// Process disconnection.
	/// </summary>
	/// <param name="adapter">The adapter that disconnected.</param>
	/// <param name="wrapper">The wrapper for the adapter.</param>
	/// <param name="supportedMessages">Supported message types.</param>
	/// <param name="error">Disconnection error if any.</param>
	/// <returns>Extra messages to send out.</returns>
	IEnumerable<Message> ProcessDisconnect(
		IMessageAdapter adapter,
		IMessageAdapter wrapper,
		IEnumerable<MessageTypes> supportedMessages,
		Exception error);

	#endregion

	#region Adapter List Management

	/// <summary>
	/// Called when an adapter is removed from the inner adapters list.
	/// </summary>
	void OnAdapterRemoved(IMessageAdapter adapter);

	/// <summary>
	/// Called when all adapters are cleared from the inner adapters list.
	/// </summary>
	void OnAdaptersCleared();

	#endregion

	#region Order Routing

	/// <summary>
	/// Add order transaction to adapter mapping.
	/// </summary>
	void AddOrderAdapter(long transactionId, IMessageAdapter adapter);

	/// <summary>
	/// Try get adapter for an order by its transaction ID.
	/// </summary>
	bool TryGetOrderAdapter(long transactionId, out IMessageAdapter adapter);

	/// <summary>
	/// Get adapter for portfolio-based routing.
	/// </summary>
	IMessageAdapter GetPortfolioAdapter(string portfolioName, Func<IMessageAdapter, IMessageAdapter> adapterLookup);

	#endregion

	#region Subscriptions

	/// <summary>
	/// Get subscriber IDs for a data type.
	/// </summary>
	long[] GetSubscribers(DataType dataType);

	/// <summary>
	/// Apply parent lookup ID remapping to subscription message.
	/// </summary>
	/// <returns><see langword="false"/> if no valid parent found and message should be dropped.</returns>
	bool ApplyParentLookupId(ISubscriptionIdMessage msg);

	#endregion

	#region Provider Change Handlers

	/// <summary>
	/// Handle security adapter provider change.
	/// </summary>
	void OnSecurityAdapterProviderChanged(
		(SecurityId, DataType) key,
		Guid adapterId,
		bool isAdd,
		Func<Guid, IMessageAdapter> findAdapter);

	/// <summary>
	/// Handle portfolio adapter provider change.
	/// </summary>
	void OnPortfolioAdapterProviderChanged(
		string portfolioName,
		Guid adapterId,
		bool isAdd,
		Func<Guid, IMessageAdapter> findAdapter);

	#endregion

	#region Load/Save

	/// <summary>
	/// Clear and load security adapter mappings.
	/// </summary>
	void LoadSecurityAdapters(IEnumerable<((SecurityId, DataType) key, IMessageAdapter adapter)> mappings);

	/// <summary>
	/// Clear and load portfolio adapter mappings.
	/// </summary>
	void LoadPortfolioAdapters(IEnumerable<(string portfolio, IMessageAdapter adapter)> mappings);

	#endregion

	#region Reset

	/// <summary>
	/// Reset all routing state.
	/// </summary>
	/// <param name="clearPending">Whether to clear pending messages.</param>
	void Reset(bool clearPending);

	#endregion

	/// <summary>
	/// Transaction id generator used for internal child subscription routing.
	/// Must be kept in sync with the parent adapter's generator.
	/// </summary>
	IdGenerator TransactionIdGenerator { get; set; }
}
