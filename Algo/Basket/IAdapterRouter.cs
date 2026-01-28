namespace StockSharp.Algo.Basket;

/// <summary>
/// Interface for routing messages to appropriate adapters.
/// </summary>
public interface IAdapterRouter
{
	/// <summary>
	/// Get adapters for a message based on message type, security, and filtering.
	/// </summary>
	/// <param name="message">Message to route.</param>
	/// <param name="getWrapper">Function to resolve wrapper adapter from underlying adapter.</param>
	/// <returns>Matched adapters array and whether supported-message filtering was skipped.</returns>
	(IMessageAdapter[] adapters, bool skipSupportedMessages) GetAdapters(Message message, Func<IMessageAdapter, IMessageAdapter> getWrapper);

	/// <summary>
	/// Get adapters for market data subscription with additional filtering (candles, build mode, etc.).
	/// </summary>
	/// <param name="mdMsg">Market data message.</param>
	/// <param name="adapters">Pre-filtered adapters from <see cref="GetAdapters"/>.</param>
	/// <param name="skipSupportedMessages">Whether supported-message filtering was skipped.</param>
	/// <returns>Filtered adapters.</returns>
	IMessageAdapter[] GetSubscriptionAdapters(MarketDataMessage mdMsg, IMessageAdapter[] adapters, bool skipSupportedMessages);

	/// <summary>
	/// Get adapter for portfolio-based routing.
	/// </summary>
	/// <param name="portfolioName">Portfolio name.</param>
	/// <param name="getWrapper">Function to resolve wrapper adapter from underlying adapter.</param>
	/// <returns>Found adapter or null.</returns>
	IMessageAdapter GetPortfolioAdapter(string portfolioName, Func<IMessageAdapter, IMessageAdapter> getWrapper);

	/// <summary>
	/// Try get adapter for order by transaction id.
	/// </summary>
	bool TryGetOrderAdapter(long transactionId, out IMessageAdapter adapter);

	/// <summary>
	/// Add order transaction to adapter mapping.
	/// </summary>
	void AddOrderAdapter(long transactionId, IMessageAdapter adapter);

	/// <summary>
	/// Add adapter to non-supported set for a transaction (for retry logic).
	/// </summary>
	void AddNotSupported(long transactionId, IMessageAdapter adapter);

	/// <summary>
	/// Add adapter for specified message type.
	/// </summary>
	void AddMessageTypeAdapter(MessageTypes type, IMessageAdapter adapter);

	/// <summary>
	/// Remove adapter for specified message type.
	/// </summary>
	void RemoveMessageTypeAdapter(MessageTypes type, IMessageAdapter adapter);

	/// <summary>
	/// Set adapter for specific security and data type.
	/// </summary>
	void SetSecurityAdapter(SecurityId secId, DataType dataType, IMessageAdapter adapter);

	/// <summary>
	/// Remove adapter for specific security and data type.
	/// </summary>
	void RemoveSecurityAdapter(SecurityId secId, DataType dataType);

	/// <summary>
	/// Set adapter for portfolio.
	/// </summary>
	void SetPortfolioAdapter(string portfolio, IMessageAdapter adapter);

	/// <summary>
	/// Remove adapter for portfolio.
	/// </summary>
	void RemovePortfolioAdapter(string portfolio);

	/// <summary>
	/// Clear all routing state (message type adapters, non-supported adapters, security/portfolio caches).
	/// </summary>
	void Clear();
}
