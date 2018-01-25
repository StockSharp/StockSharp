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
		/// Remove snapshot for the specified security.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		void Clear(SecurityId securityId);

		/// <summary>
		/// Update snapshot.
		/// </summary>
		/// <param name="message">Message.</param>
		void Update(Message message);

		/// <summary>
		/// Get snapshot for the specified security.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Snapshot.</returns>
		Message Get(SecurityId securityId);
	}
}