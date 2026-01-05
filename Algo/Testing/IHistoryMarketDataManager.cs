namespace StockSharp.Algo.Testing;

using StockSharp.Algo.Testing.Generation;

/// <summary>
/// Interface for history market data manager.
/// </summary>
public interface IHistoryMarketDataManager : IDisposable
{
	/// <summary>
	/// Date in history for starting.
	/// </summary>
	DateTime StartDate { get; set; }

	/// <summary>
	/// Date in history to stop (date is included).
	/// </summary>
	DateTime StopDate { get; set; }

	/// <summary>
	/// The interval of message <see cref="TimeMessage"/> generation.
	/// </summary>
	TimeSpan MarketTimeChangedInterval { get; set; }

	/// <summary>
	/// The number of the event calls after end of trading.
	/// </summary>
	int PostTradeMarketTimeChangedCount { get; set; }

	/// <summary>
	/// Check loading dates are they tradable.
	/// </summary>
	bool CheckTradableDates { get; set; }

	/// <summary>
	/// Market data storage.
	/// </summary>
	IStorageRegistry StorageRegistry { get; set; }

	/// <summary>
	/// The storage which is used by default.
	/// </summary>
	IMarketDataDrive Drive { get; set; }

	/// <summary>
	/// The format of market data.
	/// </summary>
	StorageFormats StorageFormat { get; set; }

	/// <summary>
	/// <see cref="BasketMarketDataStorage{T}.Cache"/>.
	/// </summary>
	MarketDataStorageCache StorageCache { get; set; }

	/// <summary>
	/// <see cref="MarketDataStorageCache"/>.
	/// </summary>
	MarketDataStorageCache AdapterCache { get; set; }

	/// <summary>
	/// The number of loaded events.
	/// </summary>
	int LoadedMessageCount { get; }

	/// <summary>
	/// Current time.
	/// </summary>
	DateTime CurrentTime { get; }

	/// <summary>
	/// Is started.
	/// </summary>
	bool IsStarted { get; }

	/// <summary>
	/// Subscribe to market data.
	/// </summary>
	/// <param name="message">Subscription message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Error if subscription failed, null otherwise.</returns>
	ValueTask<Exception> SubscribeAsync(MarketDataMessage message, CancellationToken cancellationToken);

	/// <summary>
	/// Unsubscribe from market data.
	/// </summary>
	/// <param name="originalTransactionId">Original subscription transaction id.</param>
	void Unsubscribe(long originalTransactionId);

	/// <summary>
	/// Register generator.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type.</param>
	/// <param name="generator">Generator.</param>
	/// <param name="transactionId">Transaction ID.</param>
	void RegisterGenerator(SecurityId securityId, DataType dataType, MarketDataGenerator generator, long transactionId);

	/// <summary>
	/// Unregister generator.
	/// </summary>
	/// <param name="originalTransactionId">Original transaction id.</param>
	/// <returns><see langword="true"/> if generator was found and removed.</returns>
	bool UnregisterGenerator(long originalTransactionId);

	/// <summary>
	/// Check if generator exists.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type.</param>
	/// <returns><see langword="true"/> if generator exists.</returns>
	bool HasGenerator(SecurityId securityId, DataType dataType);

	/// <summary>
	/// Get supported data types for security.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <returns>Supported data types.</returns>
	IEnumerable<DataType> GetSupportedDataTypes(SecurityId securityId);

	/// <summary>
	/// Start market data generation.
	/// </summary>
	/// <param name="boards">Exchange boards.</param>
	/// <returns>Async enumerable of messages.</returns>
	IAsyncEnumerable<Message> StartAsync(IEnumerable<BoardMessage> boards);

	/// <summary>
	/// Stop market data generation.
	/// </summary>
	void Stop();

	/// <summary>
	/// Reset state.
	/// </summary>
	void Reset();
}
