namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface, describing the storage of market data (ticks, order books etc.).
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
/// <typeparam name="TMessage">>Message type.</typeparam>
[Obsolete("Use message based storages.")]
public interface IEntityMarketDataStorage<TEntity, TMessage> : IMarketDataStorage<TMessage>
	where TMessage : Message
{
	/// <summary>
	/// To save market data in storage.
	/// </summary>
	/// <param name="data">Market data.</param>
	/// <returns>Count of saved data.</returns>
	int Save(IEnumerable<TEntity> data);

	/// <summary>
	/// To delete market data from storage.
	/// </summary>
	/// <param name="data">Market data to be deleted.</param>
	void Delete(IEnumerable<TEntity> data);

	/// <summary>
	/// To load data.
	/// </summary>
	/// <param name="date">Date, for which data shall be loaded.</param>
	/// <returns>Data. If there is no data, the empty set will be returned.</returns>
	new IEnumerable<TEntity> Load(DateTime date);
}