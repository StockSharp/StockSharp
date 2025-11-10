namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface, describing the storage, associated with <see cref="IMarketDataStorage"/>.
/// </summary>
public interface IMarketDataStorageDrive
{
	/// <summary>
	/// The storage (database, file etc.).
	/// </summary>
	IMarketDataDrive Drive { get; }

	/// <summary>
	/// To get all the dates for which market data are recorded.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Available dates.</returns>
	ValueTask<IEnumerable<DateTime>> GetDatesAsync(CancellationToken cancellationToken);

	/// <summary>
	/// To delete cache-files, containing information on available time ranges.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask ClearDatesCacheAsync(CancellationToken cancellationToken);

	/// <summary>
	/// To remove market data on specified date from the storage.
	/// </summary>
	/// <param name="date">Date, for which all data shall be deleted.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask DeleteAsync(DateTime date, CancellationToken cancellationToken);

	/// <summary>
	/// To save data in the format of StockSharp storage.
	/// </summary>
	/// <param name="date">The date, for which data shall be saved.</param>
	/// <param name="stream">Data in the format of StockSharp storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask SaveStreamAsync(DateTime date, Stream stream, CancellationToken cancellationToken);

	/// <summary>
	/// To load data in the format of StockSharp storage.
	/// </summary>
	/// <param name="date">Date, for which data shall be loaded.</param>
	/// <param name="readOnly">Get stream in read mode only.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Data in the format of StockSharp storage. If no data exists, <see cref="Stream.Null"/> will be returned.</returns>
	ValueTask<Stream> LoadStreamAsync(DateTime date, bool readOnly = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// The interface, describing the storage (database, file etc.).
/// </summary>
public interface IMarketDataDrive : IPersistable, IDisposable
{
	/// <summary>
	/// Path to market data.
	/// </summary>
	string Path { get; }

	/// <summary>
	/// Get all available instruments.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Available instruments.</returns>
	IAsyncEnumerable<SecurityId> GetAvailableSecuritiesAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Get all available data types.
	/// </summary>
	/// <param name="securityId">Instrument identifier.</param>
	/// <param name="format">Format type.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Data types.</returns>
	ValueTask<IEnumerable<DataType>> GetAvailableDataTypesAsync(SecurityId securityId, StorageFormats format, CancellationToken cancellationToken);

	/// <summary>
	/// To get the storage for <see cref="IMarketDataStorage"/>.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="format">Format type.</param>
	/// <returns>Storage for <see cref="IMarketDataStorage"/>.</returns>
	IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, DataType dataType, StorageFormats format);

	/// <summary>
	/// Verify settings.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask VerifyAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Download securities by the specified criteria.
	/// </summary>
	/// <param name="criteria">Message security lookup for specified criteria.</param>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The sequence of found instruments.</returns>
	IAsyncEnumerable<SecurityMessage> LookupSecuritiesAsync(SecurityLookupMessage criteria, ISecurityProvider securityProvider, CancellationToken cancellationToken);
}

/// <summary>
/// The base implementation <see cref="IMarketDataDrive"/>.
/// </summary>
public abstract class BaseMarketDataDrive : Disposable, IMarketDataDrive
{
	/// <summary>
	/// Initialize <see cref="BaseMarketDataDrive"/>.
	/// </summary>
	protected BaseMarketDataDrive()
	{
	}

	/// <inheritdoc />
	public abstract string Path { get; set; }

	/// <inheritdoc />
	public abstract IAsyncEnumerable<SecurityId> GetAvailableSecuritiesAsync(CancellationToken cancellationToken);

	/// <inheritdoc />
	public abstract ValueTask<IEnumerable<DataType>> GetAvailableDataTypesAsync(SecurityId securityId, StorageFormats format, CancellationToken cancellationToken);

	/// <inheritdoc />
	public abstract IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, DataType dataType, StorageFormats format);

	/// <inheritdoc />
	public abstract ValueTask VerifyAsync(CancellationToken cancellationToken);

	/// <inheritdoc />
	public abstract IAsyncEnumerable<SecurityMessage> LookupSecuritiesAsync(SecurityLookupMessage criteria, ISecurityProvider securityProvider, CancellationToken cancellationToken);

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		Path = storage.GetValue<string>(nameof(Path));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Path), Path);
	}

	/// <inheritdoc />
	public override string ToString() => Path;
}