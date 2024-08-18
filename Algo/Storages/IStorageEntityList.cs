namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface for presentation in the form of list of trade objects, received from the external storage.
/// </summary>
/// <typeparam name="T">The type of the trading object (for example, <see cref="Security"/> or <see cref="MyTrade"/>).</typeparam>
public interface IStorageEntityList<T> : INotifyList<T>, ISynchronizedCollection<T>
{
	/// <summary>
	/// Cached items.
	/// </summary>
	T[] Cache { get; }

	/// <summary>
	/// To load the trading object by identifier.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <returns>The trading object. If the object was not found by identifier, <see langword="null" /> will be returned.</returns>
	T ReadById(object id);

	/// <summary>
	/// To save the trading object.
	/// </summary>
	/// <param name="entity">The trading object.</param>
	void Save(T entity);

	/// <summary>
	/// The time delayed action.
	/// </summary>
	DelayAction DelayAction { get; }

	/// <summary>
	/// Wait flush.
	/// </summary>
	void WaitFlush();
}