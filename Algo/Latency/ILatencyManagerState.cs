namespace StockSharp.Algo.Latency;

/// <summary>
/// State storage for <see cref="LatencyManager"/>.
/// </summary>
public interface ILatencyManagerState
{
	/// <summary>
	/// Add pending registration.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="localTime">Local time.</param>
	void AddRegistration(long transactionId, DateTime localTime);

	/// <summary>
	/// Try get and remove pending registration.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="localTime">Local time if found.</param>
	/// <returns><see langword="true"/> if found and removed.</returns>
	bool TryGetAndRemoveRegistration(long transactionId, out DateTime localTime);

	/// <summary>
	/// Add pending cancellation.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="localTime">Local time.</param>
	void AddCancellation(long transactionId, DateTime localTime);

	/// <summary>
	/// Try get and remove pending cancellation.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="localTime">Local time if found.</param>
	/// <returns><see langword="true"/> if found and removed.</returns>
	bool TryGetAndRemoveCancellation(long transactionId, out DateTime localTime);

	/// <summary>
	/// Total registration latency.
	/// </summary>
	TimeSpan LatencyRegistration { get; }

	/// <summary>
	/// Total cancellation latency.
	/// </summary>
	TimeSpan LatencyCancellation { get; }

	/// <summary>
	/// Add to registration latency.
	/// </summary>
	/// <param name="latency">Latency to add.</param>
	void AddLatencyRegistration(TimeSpan latency);

	/// <summary>
	/// Add to cancellation latency.
	/// </summary>
	/// <param name="latency">Latency to add.</param>
	void AddLatencyCancellation(TimeSpan latency);

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}
