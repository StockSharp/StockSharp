namespace StockSharp.Algo.Storages
{
	using StockSharp.Messages;

	/// <summary>
	/// The interface for access to the storage of snapshot prices.
	/// </summary>
	public interface ISnapshotStorage
	{
		/// <summary>
		/// Initialize the storage.
		/// </summary>
		void Init();

		/// <summary>
		/// Clear storage.
		/// </summary>
		void ClearAll();

		/// <summary>
		/// Remove snapshot for the specified key.
		/// </summary>
		/// <param name="key">Key.</param>
		void Clear(object key);

		/// <summary>
		/// Update snapshot.
		/// </summary>
		/// <param name="message">Message.</param>
		void Update(Message message);

		/// <summary>
		/// Get snapshot for the specified key.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns>Snapshot.</returns>
		Message Get(object key);
	}

	/// <summary>
	/// The interface for access to the storage of snapshot prices.
	/// </summary>
	/// <typeparam name="TKey">Type of key value.</typeparam>
	public interface ISnapshotStorage<TKey> : ISnapshotStorage
	{
		/// <summary>
		/// Remove snapshot for the specified key.
		/// </summary>
		/// <param name="key">Key.</param>
		void Clear(TKey key);

		/// <summary>
		/// Get snapshot for the specified key.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns>Snapshot.</returns>
		Message Get(TKey key);
	}
}