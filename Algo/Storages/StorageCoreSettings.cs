namespace StockSharp.Algo.Storages;

/// <summary>
/// Storage modes.
/// </summary>
[Flags]
public enum StorageModes
{
	/// <summary>
	/// None.
	/// </summary>
	None = 1,

	/// <summary>
	/// Incremental.
	/// </summary>
	Incremental = None << 1,

	/// <summary>
	/// Snapshot.
	/// </summary>
	Snapshot = Incremental << 1,
}

/// <summary>
/// Storage settings.
/// </summary>
public class StorageCoreSettings : IPersistable
{
	/// <summary>
	/// The storage of market data.
	/// </summary>
	public IStorageRegistry StorageRegistry { get; set; }

	/// <summary>
	/// The storage (database, file etc.).
	/// </summary>
	public IMarketDataDrive Drive { get; set; }

	/// <summary>
	/// Format.
	/// </summary>
	public StorageFormats Format { get; set; }

	/// <summary>
	/// Storage mode.
	/// </summary>
	/// <remarks>By default is <see cref="StorageModes.Incremental"/>.</remarks>
	public StorageModes Mode { get; set; } = StorageModes.Incremental;

	/// <summary>
	/// To get the market-data storage.
	/// </summary>
	/// <typeparam name="TMessage">Message type.</typeparam>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType"><see cref="DataType"/></param>
	/// <returns>Market-data storage.</returns>
	public IMarketDataStorage<TMessage> GetStorage<TMessage>(SecurityId securityId, DataType dataType)
		where TMessage : Message
	{
		return (IMarketDataStorage<TMessage>)GetStorage(securityId, dataType);
	}

	/// <summary>
	/// To get the market-data storage.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType"><see cref="DataType"/></param>
	/// <returns>Market-data storage.</returns>
	public IMarketDataStorage GetStorage(SecurityId securityId, DataType dataType)
	{
		return StorageRegistry.GetStorage(securityId, dataType, Drive, Format);
	}

	/// <summary>
	/// Check the specified mode turned on.
	/// </summary>
	/// <param name="mode">Storage mode.</param>
	/// <returns>Check result.</returns>
	public bool IsMode(StorageModes mode) => Mode.HasFlag(mode);

	void IPersistable.Load(SettingsStorage storage)
	{
		Mode = storage.GetValue(nameof(Mode), Mode);
		Format = storage.GetValue(nameof(Format), Format);
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Mode), Mode);
		storage.SetValue(nameof(Format), Format);
	}
}