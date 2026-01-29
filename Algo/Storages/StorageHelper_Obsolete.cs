namespace StockSharp.Algo.Storages;

using StockSharp.Algo.Candles.Compression;

static partial class StorageHelper
{
	/// <summary>
	/// Get all available data types.
	/// </summary>
	/// <param name="drive"><see cref="IMarketDataDrive"/></param>
	/// <param name="securityId">Instrument identifier.</param>
	/// <param name="format">Format type.</param>
	/// <returns>Data types.</returns>
	[Obsolete("Use GetAvailableDataTypesAsync method instead.")]
	public static IEnumerable<DataType> GetAvailableDataTypes(this IMarketDataDrive drive, SecurityId securityId, StorageFormats format)
		=> drive.GetAvailableDataTypesAsync(securityId, format).ToBlockingEnumerable();

	/// <summary>
	/// To create an iterative loader of market data for the time range.
	/// </summary>
	/// <typeparam name="TMessage">Data type.</typeparam>
	/// <param name="storage">Market-data storage.</param>
	/// <param name="from">The start time for data loading. If the value is not specified, data will be loaded from the starting time <see cref="IMarketDataStorageDrive.GetDatesAsync"/>.</param>
	/// <param name="to">The end time for data loading. If the value is not specified, data will be loaded up to the <see cref="IMarketDataStorageDrive.GetDatesAsync"/> date, inclusive.</param>
	/// <returns>The iterative loader of market data.</returns>
	[Obsolete("Use LoadAsync method instead.")]
	public static IEnumerable<TMessage> Load<TMessage>(this IMarketDataStorage<TMessage> storage, DateTime? from, DateTime? to)
		where TMessage : Message, IServerTimeMessage
		=> LoadAsync(storage, from, to).ToBlockingEnumerable();

	/// <summary>
	/// To delete market data from the storage for the specified time period.
	/// </summary>
	/// <param name="storage">Market-data storage.</param>
	/// <param name="from">The start time for data deleting. If the value is not specified, the data will be deleted starting from the date <see cref="IMarketDataStorageDrive.GetDatesAsync"/>.</param>
	/// <param name="to">The end time, up to which the data shall be deleted. If the value is not specified, data will be deleted up to the end date <see cref="IMarketDataStorageDrive.GetDatesAsync"/>, inclusive.</param>
	/// <returns><see langword="true"/> if data was deleted, <see langword="false"/> data not exist for the specified period.</returns>
	[Obsolete("Use DeleteAsync method instead.")]
	public static bool Delete(this IMarketDataStorage storage, DateTime? from, DateTime? to)
		=> AsyncHelper.Run(() => DeleteAsync(storage, from, to, default));

	/// <summary>
	/// Get available date range for the specified storage.
	/// </summary>
	/// <param name="storage">Storage.</param>
	/// <param name="from">The initial date from which you need to get data.</param>
	/// <param name="to">The final date by which you need to get data.</param>
	/// <returns>Date range</returns>
	[Obsolete("Use GetRangeAsync method instead.")]
	public static IRange<DateTime> GetRange(this IMarketDataStorage storage, DateTime? from, DateTime? to)
		=> AsyncHelper.Run(() => GetRangeAsync(storage, from, to, default));

	/// <summary>
	/// To get the start date for market data, stored in the storage.
	/// </summary>
	/// <param name="storage">Market-data storage.</param>
	/// <returns>The start date. If the value is not initialized, the storage is empty.</returns>
	[Obsolete("Use GetDatesAsync instead.")]
	public static DateTime? GetFromDate(this IMarketDataStorage storage)
		=> storage.GetDates().FirstOr();

	/// <summary>
	/// To get the end date for market data, stored in the storage.
	/// </summary>
	/// <param name="storage">Market-data storage.</param>
	/// <returns>The end date. If the value is not initialized, the storage is empty.</returns>
	[Obsolete("Use GetDatesAsync instead.")]
	public static DateTime? GetToDate(this IMarketDataStorage storage)
		=> storage.GetDates().LastOr();

	/// <summary>
	/// To get all dates for stored market data for the specified range.
	/// </summary>
	/// <param name="storage">Market-data storage.</param>
	/// <param name="from">The range start time. If the value is not specified, data will be loaded from the start date <see cref="IMarketDataStorageDrive.GetDatesAsync"/>.</param>
	/// <param name="to">The range end time. If the value is not specified, data will be loaded up to the end date <see cref="IMarketDataStorageDrive.GetDatesAsync"/>, inclusive.</param>
	/// <returns>All available data within the range.</returns>
	[Obsolete("Use GetDatesAsync method instead.")]
	public static IEnumerable<DateTime> GetDates(this IMarketDataStorage storage, DateTime? from, DateTime? to)
		=> GetDatesAsync(storage, from, to).ToBlockingEnumerable();

	/// <summary>
	/// Load messages.
	/// </summary>
	/// <param name="settings">Storage settings.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	/// <param name="subscription">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
	/// <param name="newOutMessage">New message event.</param>
	/// <returns>Last date.</returns>
	[Obsolete("Use LoadMessagesAsync method instead.")]
	public static (DateTime? lastDate, long? left)? LoadMessages(this StorageCoreSettings settings, CandleBuilderProvider candleBuilderProvider, MarketDataMessage subscription, Action<Message> newOutMessage)
	{
		if (newOutMessage is null)
			throw new ArgumentNullException(nameof(newOutMessage));

		return AsyncHelper.Run(async () =>
		{
			var context = new StorageLoadContext();

			await foreach (var msg in LoadMessagesAsync(settings, candleBuilderProvider, subscription, context, default))
				newOutMessage(msg);

			return (context.LastDate, context.Left);
		});
	}

	/// <summary>
	/// To get the <see cref="ExecutionMessage"/> storage for the specified instrument.
	/// </summary>
	/// <param name="registry"><see cref="IStorageRegistry"/></param>
	/// <param name="securityId">Security ID.</param>
	/// <param name="type">Data type, information about which is contained in the <see cref="ExecutionMessage"/>.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The <see cref="ExecutionMessage"/> storage.</returns>
	[Obsolete("Use DataType overload.")]
	public static IMarketDataStorage<ExecutionMessage> GetExecutionMessageStorage(this IStorageRegistry registry, SecurityId securityId, ExecutionTypes type, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		ArgumentNullException.ThrowIfNull(registry);
		return registry.GetExecutionMessageStorage(securityId, type.ToDataType(), drive, format);
	}

	/// <summary>
	/// To get the market-data storage.
	/// </summary>
	/// <param name="registry"><see cref="IStorageRegistry"/></param>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Market data type.</param>
	/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, candle arg.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>Market-data storage.</returns>
	[Obsolete("Use DataType overload.")]
	public static IMarketDataStorage GetStorage(this IStorageRegistry registry, SecurityId securityId, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		ArgumentNullException.ThrowIfNull(registry);
		return registry.GetStorage(securityId, DataType.Create(dataType, arg), drive, format);
	}

	/// <summary>
	/// To get the market-data storage.
	/// </summary>
	/// <param name="registry"><see cref="IStorageRegistry"/></param>
	/// <param name="security">Security.</param>
	/// <param name="dataType">Market data type.</param>
	/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, candle arg.</param>
	/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
	/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
	/// <returns>Market-data storage.</returns>
	[Obsolete("Use SecurityId overload.")]
	public static IMarketDataStorage GetStorage(this IStorageRegistry registry, Security security, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		ArgumentNullException.ThrowIfNull(registry);
		return registry.GetStorage(security.ToSecurityId(), dataType, arg, drive, format);
	}

	/// <summary>
	/// To get the market-data storage.
	/// </summary>
	/// <param name="registry">Market-data storage.</param>
	/// <param name="security">Security.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
	/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
	/// <returns>Market-data storage.</returns>
	[Obsolete("Use SecurityId overload.")]
	public static IMarketDataStorage GetStorage(this IStorageRegistry registry, Security security, DataType dataType, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		return registry.GetStorage(security.ToSecurityId(), dataType, drive, format);
	}

	/// <summary>
	/// To get the snapshot storage.
	/// </summary>
	/// <param name="registry">Snapshot storage registry.</param>
	/// <param name="dataType">Market data type.</param>
	/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, candle arg.</param>
	/// <returns>The snapshot storage.</returns>
	[Obsolete("Use DataType overload.")]
	public static ISnapshotStorage GetSnapshotStorage(this ISnapshotRegistry registry, Type dataType, object arg)
	{
		if (registry is null)
			throw new ArgumentNullException(nameof(registry));

		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		return registry.GetSnapshotStorage(DataType.Create(dataType, arg).Immutable());
	}

	/// <summary>
	/// To get the candles storage for the specified instrument.
	/// </summary>
	/// <param name="registry"><see cref="IStorageRegistry"/></param>
	/// <param name="candleMessageType">The type of candle message.</param>
	/// <param name="securityId">Security ID.</param>
	/// <param name="arg">Candle arg.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The candles storage.</returns>
	[Obsolete("Use DataType overload.")]
	public static IMarketDataStorage<CandleMessage> GetCandleMessageStorage(this IStorageRegistry registry, Type candleMessageType, SecurityId securityId, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		ArgumentNullException.ThrowIfNull(registry);
		return registry.GetCandleMessageStorage(securityId, DataType.Create(candleMessageType, arg), drive, format);
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="ISecurityStorage.SaveAsync(Security, bool, CancellationToken)"/>.
	/// </summary>
	/// <param name="storage">Security storage.</param>
	/// <param name="security">Security.</param>
	/// <param name="forced">Forced update.</param>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	//[Obsolete("Use SaveAsync method instead.")]
	public static void Save(this ISecurityStorage storage, Security security, bool forced)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		AsyncHelper.Run(() => storage.SaveAsync(security, forced, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="ISecurityStorage.DeleteAsync(Security, CancellationToken)"/>.
	/// </summary>
	/// <param name="storage">Security storage.</param>
	/// <param name="security">Security.</param>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use DeleteAsync method instead.")]
	public static void Delete(this ISecurityStorage storage, Security security)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		AsyncHelper.Run(() => storage.DeleteAsync(security, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="ISecurityStorage.DeleteRangeAsync(IEnumerable{Security}, CancellationToken)"/>.
	/// </summary>
	/// <param name="storage">Security storage.</param>
	/// <param name="securities">Securities.</param>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use DeleteRangeAsync method instead.")]
	public static void DeleteRange(this ISecurityStorage storage, IEnumerable<Security> securities)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		AsyncHelper.Run(() => storage.DeleteRangeAsync(securities, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="ISecurityStorage.DeleteByAsync(SecurityLookupMessage, CancellationToken)"/>.
	/// </summary>
	/// <param name="storage">Security storage.</param>
	/// <param name="criteria">The criterion.</param>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use DeleteByAsync method instead.")]
	public static void DeleteBy(this ISecurityStorage storage, SecurityLookupMessage criteria)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		AsyncHelper.Run(() => storage.DeleteByAsync(criteria, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorageDrive.GetDatesAsync"/>.
	/// </summary>
	/// <param name="drive">Market data storage drive.</param>
	/// <returns>Available dates.</returns>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use GetDatesAsync method instead.")]
	public static IEnumerable<DateTime> GetDates(this IMarketDataStorageDrive drive)
	{
		if (drive is null)
			throw new ArgumentNullException(nameof(drive));

		return drive.GetDatesAsync().ToBlockingEnumerable();
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorageDrive.ClearDatesCacheAsync(CancellationToken)"/>.
	/// </summary>
	/// <param name="drive">Market data storage drive.</param>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use ClearDatesCacheAsync method instead.")]
	public static void ClearDatesCache(this IMarketDataStorageDrive drive)
	{
		if (drive is null)
			throw new ArgumentNullException(nameof(drive));

		AsyncHelper.Run(() => drive.ClearDatesCacheAsync(default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorageDrive.DeleteAsync(DateTime, CancellationToken)"/>.
	/// </summary>
	/// <param name="drive">Market data storage drive.</param>
	/// <param name="date">Date, for which all data shall be deleted.</param>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use DeleteAsync method instead.")]
	public static void Delete(this IMarketDataStorageDrive drive, DateTime date)
	{
		if (drive is null)
			throw new ArgumentNullException(nameof(drive));

		AsyncHelper.Run(() => drive.DeleteAsync(date, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorageDrive.SaveStreamAsync(DateTime, Stream, CancellationToken)"/>.
	/// </summary>
	/// <param name="drive">Market data storage drive.</param>
	/// <param name="date">The date, for which data shall be saved.</param>
	/// <param name="stream">Data in the format of StockSharp storage.</param>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use SaveStreamAsync method instead.")]
	public static void SaveStream(this IMarketDataStorageDrive drive, DateTime date, Stream stream)
	{
		if (drive is null)
			throw new ArgumentNullException(nameof(drive));

		AsyncHelper.Run(() => drive.SaveStreamAsync(date, stream, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorageDrive.LoadStreamAsync(DateTime, bool, CancellationToken)"/>.
	/// </summary>
	/// <param name="drive">Market data storage drive.</param>
	/// <param name="date">Date, for which data shall be loaded.</param>
	/// <param name="readOnly">Get stream in read mode only.</param>
	/// <returns>Data in the format of StockSharp storage. If no data exists, <see cref="Stream.Null"/> will be returned.</returns>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use LoadStreamAsync method instead.")]
	public static Stream LoadStream(this IMarketDataStorageDrive drive, DateTime date, bool readOnly = false)
	{
		if (drive is null)
			throw new ArgumentNullException(nameof(drive));

		return AsyncHelper.Run(() => drive.LoadStreamAsync(date, readOnly, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorage.GetDatesAsync"/>.
	/// </summary>
	/// <param name="storage">Market data storage.</param>
	/// <returns>Available dates.</returns>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use GetDatesAsync method instead.")]
	public static IEnumerable<DateTime> GetDates(this IMarketDataStorage storage)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		return storage.GetDatesAsync().ToBlockingEnumerable();
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorage.SaveAsync(IEnumerable{Message}, CancellationToken)"/>.
	/// </summary>
	/// <param name="storage">Market data storage.</param>
	/// <param name="data">Market data.</param>
	/// <returns>Count of saved data.</returns>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use SaveAsync method instead.")]
	public static int Save(this IMarketDataStorage storage, IEnumerable<Message> data)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		return AsyncHelper.Run(() => storage.SaveAsync(data, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorage.DeleteAsync(IEnumerable{Message}, CancellationToken)"/>.
	/// </summary>
	/// <param name="storage">Market data storage.</param>
	/// <param name="data">Market data to be deleted.</param>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use DeleteAsync method instead.")]
	public static void Delete(this IMarketDataStorage storage, IEnumerable<Message> data)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		AsyncHelper.Run(() => storage.DeleteAsync(data, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorage.DeleteAsync(DateTime, CancellationToken)"/>.
	/// </summary>
	/// <param name="storage">Market data storage.</param>
	/// <param name="date">Date, for which all data shall be deleted.</param>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use DeleteAsync method instead.")]
	public static void Delete(this IMarketDataStorage storage, DateTime date)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		AsyncHelper.Run(() => storage.DeleteAsync(date, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorage.LoadAsync(DateTime)"/>.
	/// </summary>
	/// <param name="storage">Market data storage.</param>
	/// <param name="date">Date, for which data shall be loaded.</param>
	/// <returns>Data. If there is no data, the empty set will be returned.</returns>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use LoadAsync method instead.")]
	public static IEnumerable<Message> Load(this IMarketDataStorage storage, DateTime date)
		=> storage.CheckOnNull(nameof(storage)).LoadAsync(date).ToBlockingEnumerable();

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorage.GetMetaInfoAsync(DateTime, CancellationToken)"/>.
	/// </summary>
	/// <param name="storage">Market data storage.</param>
	/// <param name="date">Date, for which meta-information on data shall be received.</param>
	/// <returns>Meta-information on data. If there is no such date in history, <see langword="null" /> will be returned.</returns>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use GetMetaInfoAsync method instead.")]
	public static IMarketDataMetaInfo GetMetaInfo(this IMarketDataStorage storage, DateTime date)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		return AsyncHelper.Run(() => storage.GetMetaInfoAsync(date, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorage{TMessage}.SaveAsync(IEnumerable{TMessage}, CancellationToken)"/>.
	/// </summary>
	/// <typeparam name="TMessage">Market data type.</typeparam>
	/// <param name="storage">Market data storage.</param>
	/// <param name="data">Market data.</param>
	/// <returns>Count of saved data.</returns>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use SaveAsync method instead.")]
	public static int Save<TMessage>(this IMarketDataStorage<TMessage> storage, IEnumerable<TMessage> data)
		where TMessage : Message
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		return AsyncHelper.Run(() => storage.SaveAsync(data, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorage{TMessage}.DeleteAsync(IEnumerable{TMessage}, CancellationToken)"/>.
	/// </summary>
	/// <typeparam name="TMessage">Market data type.</typeparam>
	/// <param name="storage">Market data storage.</param>
	/// <param name="data">Market data to be deleted.</param>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use DeleteAsync method instead.")]
	public static void Delete<TMessage>(this IMarketDataStorage<TMessage> storage, IEnumerable<TMessage> data)
		where TMessage : Message
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		AsyncHelper.Run(() => storage.DeleteAsync(data, default));
	}

	/// <summary>
	/// Synchronous wrapper for <see cref="IMarketDataStorage{TMessage}.LoadAsync(DateTime)"/>.
	/// </summary>
	/// <typeparam name="TMessage">Market data type.</typeparam>
	/// <param name="storage">Market data storage.</param>
	/// <param name="date">Date, for which data shall be loaded.</param>
	/// <returns>Data. If there is no data, the empty set will be returned.</returns>
	/// <remarks>Calls async method via <see cref="AsyncHelper.Run{T}(Func{ValueTask{T}})"/> for backward compatibility.</remarks>
	[Obsolete("Use LoadAsync method instead.")]
	public static IEnumerable<TMessage> Load<TMessage>(this IMarketDataStorage<TMessage> storage, DateTime date)
		where TMessage : Message
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		return storage.LoadAsync(date).ToBlockingEnumerable();
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use InitAsync method instead.")]
	public static void Init(this IEntityRegistry registry)
		=> AsyncHelper.Run(() => registry.InitAsync(default));

	/// <summary>
	/// </summary>
	[Obsolete("Use InitAsync method instead.")]
	public static void Init(this ISnapshotRegistry registry)
		=> AsyncHelper.Run(() => registry.InitAsync(default));

	/// <summary>
	/// </summary>
	[Obsolete("Use InitAsync method instead.")]
	public static void Init(this IExchangeInfoProvider provider)
		=> AsyncHelper.Run(() => provider.InitAsync(default));
}
