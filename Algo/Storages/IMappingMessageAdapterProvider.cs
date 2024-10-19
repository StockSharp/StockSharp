namespace StockSharp.Algo.Storages;

/// <summary>
/// The mapping message adapter's provider interface.
/// </summary>
/// <typeparam name="TKey">Type of key.</typeparam>
public interface IMappingMessageAdapterProvider<TKey>
{
	/// <summary>
	/// All available adapters.
	/// </summary>
	IEnumerable<KeyValuePair<TKey, Guid>> Adapters { get; }

	/// <summary>
	/// Initialize the storage.
	/// </summary>
	void Init();

	/// <summary>
	/// Association changed.
	/// </summary>
	event Action<TKey, Guid, bool> Changed;

	/// <summary>
	/// Get adapter by the specified key.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <returns>Found adapter identifier or <see langword="null"/>.</returns>
	Guid? TryGetAdapter(TKey key);

	/// <summary>
	/// Make association with adapter.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <param name="adapterId">Adapter identifier.</param>
	/// <returns><see langword="true"/> if the association is successfully changed, otherwise, <see langword="false"/>.</returns>
	bool SetAdapter(TKey key, Guid adapterId);

	/// <summary>
	/// Remove association with adapter.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <returns><see langword="true"/> if the association is successfully removed, otherwise, <see langword="false"/>.</returns>
	bool RemoveAssociation(TKey key);
}