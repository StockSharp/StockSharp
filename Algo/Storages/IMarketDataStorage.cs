namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface, describing the storage of market data (ticks, order books etc.).
/// </summary>
public interface IMarketDataStorage
{
	/// <summary>
	/// All data, for which market data are recorded.
	/// </summary>
	IEnumerable<DateTime> Dates { get; }

	/// <summary>
	/// The type of market-data, operated by given storage.
	/// </summary>
	DataType DataType { get; }

	/// <summary>
	/// The instrument, operated by the external storage.
	/// </summary>
	SecurityId SecurityId { get; }

	/// <summary>
	/// The storage (database, file etc.).
	/// </summary>
	IMarketDataStorageDrive Drive { get; }

	/// <summary>
	/// Whether to add new data or attempt to record all data without filter.
	/// </summary>
	bool AppendOnlyNew { get; set; }

	/// <summary>
	/// To save market data in storage.
	/// </summary>
	/// <param name="data">Market data.</param>
	/// <returns>Count of saved data.</returns>
	int Save(IEnumerable<Message> data);

	/// <summary>
	/// To delete market data from storage.
	/// </summary>
	/// <param name="data">Market data to be deleted.</param>
	void Delete(IEnumerable<Message> data);

	/// <summary>
	/// To remove market data on specified date from the storage.
	/// </summary>
	/// <param name="date">Date, for which all data shall be deleted.</param>
	void Delete(DateTime date);

	/// <summary>
	/// To load data.
	/// </summary>
	/// <param name="date">Date, for which data shall be loaded.</param>
	/// <returns>Data. If there is no data, the empty set will be returned.</returns>
	IEnumerable<Message> Load(DateTime date);

	/// <summary>
	/// To get meta-information on data.
	/// </summary>
	/// <param name="date">Date, for which meta-information on data shall be received.</param>
	/// <returns>Meta-information on data. If there is no such date in history, <see langword="null" /> will be returned.</returns>
	IMarketDataMetaInfo GetMetaInfo(DateTime date);

	/// <summary>
	/// The serializer.
	/// </summary>
	IMarketDataSerializer Serializer { get; }
}

/// <summary>
/// The interface, describing the storage of market data (ticks, order books etc.).
/// </summary>
/// <typeparam name="TMessage">Market data type.</typeparam>
public interface IMarketDataStorage<TMessage> : IMarketDataStorage
	where TMessage : Message
{
	/// <summary>
	/// To save market data in storage.
	/// </summary>
	/// <param name="data">Market data.</param>
	/// <returns>Count of saved data.</returns>
	int Save(IEnumerable<TMessage> data);

	/// <summary>
	/// To delete market data from storage.
	/// </summary>
	/// <param name="data">Market data to be deleted.</param>
	void Delete(IEnumerable<TMessage> data);

	/// <summary>
	/// To load data.
	/// </summary>
	/// <param name="date">Date, for which data shall be loaded.</param>
	/// <returns>Data. If there is no data, the empty set will be returned.</returns>
	new IEnumerable<TMessage> Load(DateTime date);

	/// <summary>
	/// The serializer.
	/// </summary>
	new IMarketDataSerializer<TMessage> Serializer { get; }
}