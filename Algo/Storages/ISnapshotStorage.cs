namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface for access to the storage of snapshot prices.
/// </summary>
public interface ISnapshotStorage
{
	/// <summary>
	/// To get all the dates for which market data are recorded.
	/// </summary>
	IEnumerable<DateTime> Dates { get; }

	/// <summary>
	/// Clear storage.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask ClearAllAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Remove snapshot for the specified key.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask ClearAsync(object key, CancellationToken cancellationToken);

	/// <summary>
	/// Update snapshot.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask UpdateAsync(Message message, CancellationToken cancellationToken);

	/// <summary>
	/// Get snapshot for the specified key.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Snapshot.</returns>
	ValueTask<Message> GetAsync(object key, CancellationToken cancellationToken);

	/// <summary>
	/// Get all snapshots.
	/// </summary>
	/// <param name="from">Start date, from which data needs to be retrieved.</param>
	/// <param name="to">End date, until which data needs to be retrieved.</param>
	/// <returns>All snapshots.</returns>
	IAsyncEnumerable<Message> GetAllAsync(DateTime? from = null, DateTime? to = null);
}

/// <summary>
/// The interface for access to the storage of snapshot prices.
/// </summary>
/// <typeparam name="TKey">Type of key value.</typeparam>
/// <typeparam name="TMessage">Message type.</typeparam>
public interface ISnapshotStorage<TKey, TMessage> : ISnapshotStorage
	where TMessage : Message
{
	/// <summary>
	/// Remove snapshot for the specified key.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask ClearAsync(TKey key, CancellationToken cancellationToken);

	/// <summary>
	/// Get snapshot for the specified key.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Snapshot.</returns>
	ValueTask<TMessage> GetAsync(TKey key, CancellationToken cancellationToken);

	/// <summary>
	/// Get all snapshots.
	/// </summary>
	/// <param name="from">Start date, from which data needs to be retrieved.</param>
	/// <param name="to">End date, until which data needs to be retrieved.</param>
	/// <returns>All snapshots.</returns>
	new IAsyncEnumerable<TMessage> GetAllAsync(DateTime? from = null, DateTime? to = null);
}
