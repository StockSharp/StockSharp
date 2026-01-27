namespace StockSharp.Algo.Basket;

/// <summary>
/// Interface for managing pending messages before connection is established.
/// </summary>
public interface IPendingMessageManager
{
	/// <summary>
	/// Enqueues a cloned message if basket is not connected.
	/// </summary>
	/// <param name="message">The message to enqueue (will be cloned).</param>
	/// <param name="currentState">Current basket connection state.</param>
	/// <param name="hasPendingAdapters">Whether there are adapters still connecting.</param>
	/// <param name="totalAdapters">Total adapter count.</param>
	/// <returns><see langword="true"/> if message was enqueued.</returns>
	bool TryEnqueue(Message message, ConnectionStates currentState, bool hasPendingAdapters, int totalAdapters);

	/// <summary>
	/// Gets all pending messages and clears the queue.
	/// </summary>
	Message[] DequeueAll();

	/// <summary>
	/// Tries to find and remove a pending MarketData subscription by transaction ID.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <returns>The removed message, or null.</returns>
	MarketDataMessage TryRemoveMarketData(long transactionId);

	/// <summary>
	/// Gets whether there are pending messages.
	/// </summary>
	bool HasPending { get; }

	/// <summary>
	/// Gets the underlying state storage.
	/// </summary>
	IPendingMessageState State { get; }
}
