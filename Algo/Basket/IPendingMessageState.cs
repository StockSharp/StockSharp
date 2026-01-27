namespace StockSharp.Algo.Basket;

/// <summary>
/// Interface for pending messages storage.
/// </summary>
public interface IPendingMessageState
{
	/// <summary>
	/// Adds a message to pending queue.
	/// </summary>
	void Add(Message message);

	/// <summary>
	/// Gets all pending messages and clears the queue.
	/// </summary>
	Message[] GetAndClear();

	/// <summary>
	/// Tries to find and remove a pending MarketDataMessage by its transaction ID.
	/// </summary>
	/// <param name="transactionId">Transaction ID to find.</param>
	/// <returns>The removed message, or null if not found.</returns>
	MarketDataMessage TryRemoveMarketData(long transactionId);

	/// <summary>
	/// Gets the count of pending messages.
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Clears all pending messages.
	/// </summary>
	void Clear();
}
