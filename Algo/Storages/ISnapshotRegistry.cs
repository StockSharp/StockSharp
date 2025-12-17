namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface for snapshot storage registry.
/// </summary>
public interface ISnapshotRegistry
{
	/// <summary>
	/// To get the snapshot storage.
	/// </summary>
	/// <param name="dataType"><see cref="DataType"/></param>
	/// <returns>The snapshot storage.</returns>
	ISnapshotStorage GetSnapshotStorage(DataType dataType);

	/// <summary>
	/// Initialize the storage.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask InitAsync(CancellationToken cancellationToken);
}