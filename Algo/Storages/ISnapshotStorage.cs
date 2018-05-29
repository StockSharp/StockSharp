namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

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

		/// <summary>
		/// Get all snapshots.
		/// </summary>
		/// <param name="from">Start date, from which data needs to be retrieved.</param>
		/// <param name="to">End date, until which data needs to be retrieved.</param>
		/// <returns>All snapshots.</returns>
		IEnumerable<Message> GetAll(DateTimeOffset? from = null, DateTimeOffset? to = null);
	}

	/// <summary>
	/// The interface for access to the storage of snapshot prices.
	/// </summary>
	/// <typeparam name="TKey">Type of key value.</typeparam>
	/// <typeparam name="TMessage">Message type.</typeparam>
	public interface ISnapshotStorage<TKey, TMessage> : ISnapshotStorage
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
		TMessage Get(TKey key);

		/// <summary>
		/// Get all snapshots.
		/// </summary>
		/// <param name="from">Start date, from which data needs to be retrieved.</param>
		/// <param name="to">End date, until which data needs to be retrieved.</param>
		/// <returns>All snapshots.</returns>
		new IEnumerable<TMessage> GetAll(DateTimeOffset? from = null, DateTimeOffset? to = null);
	}
}