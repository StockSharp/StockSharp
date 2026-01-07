namespace StockSharp.Algo.Storages;

/// <summary>
/// Single storage for security identifier mappings.
/// </summary>
public interface ISecurityMappingStorage
{
	/// <summary>
	/// The mapping changed.
	/// </summary>
	event Action<SecurityIdMapping> Changed;

	/// <summary>
	/// Get all security identifier mappings.
	/// </summary>
	IEnumerable<SecurityIdMapping> Mappings { get; }

	/// <summary>
	/// Save security identifier mapping.
	/// </summary>
	/// <param name="mapping">Security identifier mapping.</param>
	/// <returns><see langword="true"/> if security mapping was added. If was changed, <see langword="false" />.</returns>
	bool Save(SecurityIdMapping mapping);

	/// <summary>
	/// Remove security mapping.
	/// </summary>
	/// <param name="stockSharpId">StockSharp format.</param>
	/// <returns><see langword="true"/> if mapping was removed. Otherwise, <see langword="false" />.</returns>
	bool Remove(SecurityId stockSharpId);

	/// <summary>
	/// Try get <see cref="SecurityIdMapping.StockSharpId"/>.
	/// </summary>
	/// <param name="adapterId">Adapter format.</param>
	/// <returns><see cref="SecurityIdMapping.StockSharpId"/> if identifier exists. Otherwise, <see langword="null" />.</returns>
	SecurityId? TryGetStockSharpId(SecurityId adapterId);

	/// <summary>
	/// Try get <see cref="SecurityIdMapping.AdapterId"/>.
	/// </summary>
	/// <param name="stockSharpId">StockSharp format.</param>
	/// <returns><see cref="SecurityIdMapping.AdapterId"/> if identifier exists. Otherwise, <see langword="null" />.</returns>
	SecurityId? TryGetAdapterId(SecurityId stockSharpId);
}

/// <summary>
/// Security identifier mappings storage provider.
/// </summary>
public interface ISecurityMappingStorageProvider : IDisposable
{
	/// <summary>
	/// Initialize the storage provider.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
	ValueTask<Dictionary<string, Exception>> InitAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Get storage names.
	/// </summary>
	IEnumerable<string> StorageNames { get; }

	/// <summary>
	/// Get storage for a specific storage name.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <returns>Storage instance.</returns>
	ISecurityMappingStorage GetStorage(string storageName);
}

/// <summary>
/// In memory security identifier mappings storage.
/// </summary>
public class InMemorySecurityMappingStorage : ISecurityMappingStorage
{
	private readonly PairSet<SecurityId, SecurityId> _mappings = [];
	private readonly Lock _syncRoot = new();

	private Action<SecurityIdMapping> _changed;

	/// <inheritdoc />
	public event Action<SecurityIdMapping> Changed
	{
		add => _changed += value;
		remove => _changed -= value;
	}

	/// <inheritdoc />
	public IEnumerable<SecurityIdMapping> Mappings
	{
		get
		{
			using (_syncRoot.EnterScope())
				return [.. _mappings.Select(p => (SecurityIdMapping)p)];
		}
	}

	/// <inheritdoc />
	public bool Save(SecurityIdMapping mapping)
	{
		return Save(mapping, out _);
	}

	internal bool Save(SecurityIdMapping mapping, out IEnumerable<SecurityIdMapping> all)
	{
		if (mapping == default)
			throw new ArgumentNullException(nameof(mapping));

		var added = false;

		using (_syncRoot.EnterScope())
		{
			var stockSharpId = mapping.StockSharpId;
			var adapterId = mapping.AdapterId;

			if (_mappings.Remove(stockSharpId))
			{
			}
			else if (_mappings.ContainsValue(adapterId))
			{
				_mappings.RemoveByValue(adapterId);
			}
			else
				added = true;

			_mappings.Add(stockSharpId, adapterId);

			all = added ? null : [.. _mappings.Select(p => (SecurityIdMapping)p)];
		}

		_changed?.Invoke(mapping);

		return added;
	}

	/// <inheritdoc />
	public bool Remove(SecurityId stockSharpId)
	{
		return Remove(stockSharpId, out _);
	}

	internal bool Remove(SecurityId stockSharpId, out IEnumerable<SecurityIdMapping> all)
	{
		if (stockSharpId == default)
			throw new ArgumentNullException(nameof(stockSharpId));

		all = null;

		using (_syncRoot.EnterScope())
		{
			var removed = _mappings.Remove(stockSharpId);

			if (!removed)
				return false;

			all = [.. _mappings.Select(p => (SecurityIdMapping)p)];
		}

		_changed?.Invoke(new SecurityIdMapping { StockSharpId = stockSharpId });

		return true;
	}

	/// <inheritdoc />
	public SecurityId? TryGetStockSharpId(SecurityId adapterId)
	{
		using (_syncRoot.EnterScope())
		{
			if (!_mappings.TryGetKey(adapterId, out var stockSharpId))
				return null;

			return stockSharpId;
		}
	}

	/// <inheritdoc />
	public SecurityId? TryGetAdapterId(SecurityId stockSharpId)
	{
		using (_syncRoot.EnterScope())
		{
			if (!_mappings.TryGetValue(stockSharpId, out var adapterId))
				return null;

			return adapterId;
		}
	}

	internal void Load(IEnumerable<(SecurityId stockSharpId, SecurityId adapterId)> pairs)
	{
		if (pairs == null)
			throw new ArgumentNullException(nameof(pairs));

		using (_syncRoot.EnterScope())
		{
			foreach (var (stockSharpId, adapterId) in pairs)
				_mappings.Add(stockSharpId, adapterId);
		}
	}
}

/// <summary>
/// In memory security identifier mappings storage provider.
/// </summary>
public class InMemorySecurityMappingStorageProvider : ISecurityMappingStorageProvider
{
	private readonly SynchronizedDictionary<string, InMemorySecurityMappingStorage> _storages = new(StringComparer.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	public ValueTask<Dictionary<string, Exception>> InitAsync(CancellationToken cancellationToken) => new([]);

	/// <inheritdoc />
	public IEnumerable<string> StorageNames
	{
		get
		{
			using (_storages.EnterScope())
				return [.. _storages.Keys];
		}
	}

	/// <inheritdoc />
	public ISecurityMappingStorage GetStorage(string storageName)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		return _storages.SafeAdd(storageName, _ => new InMemorySecurityMappingStorage());
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_storages.Clear();
		GC.SuppressFinalize(this);
	}
}

/// <summary>
/// CSV security identifier mappings storage provider.
/// </summary>
public sealed class CsvSecurityMappingStorageProvider : Disposable, ISecurityMappingStorageProvider
{
	private class CsvSecurityMappingStorage(CsvSecurityMappingStorageProvider provider, string storageName, InMemorySecurityMappingStorage inMemory) : ISecurityMappingStorage
	{
		private readonly CsvSecurityMappingStorageProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
		private readonly string _storageName = storageName ?? throw new ArgumentNullException(nameof(storageName));
		private readonly InMemorySecurityMappingStorage _inMemory = inMemory ?? throw new ArgumentNullException(nameof(inMemory));
		private Action<SecurityIdMapping> _changed;

		/// <inheritdoc />
		public event Action<SecurityIdMapping> Changed
		{
			add => _changed += value;
			remove => _changed -= value;
		}

		/// <inheritdoc />
		public IEnumerable<SecurityIdMapping> Mappings => _inMemory.Mappings;

		/// <inheritdoc />
		public bool Save(SecurityIdMapping mapping)
		{
			if (mapping == default)
				throw new ArgumentNullException(nameof(mapping));

			var added = _inMemory.Save(mapping, out var all);

			if (added)
				SaveToFile(false, [mapping]);
			else
				SaveToFile(true, all);

			_changed?.Invoke(mapping);

			return added;
		}

		/// <inheritdoc />
		public bool Remove(SecurityId stockSharpId)
		{
			if (!_inMemory.Remove(stockSharpId, out var all))
				return false;

			SaveToFile(true, all);

			_changed?.Invoke(new SecurityIdMapping { StockSharpId = stockSharpId });

			return true;
		}

		/// <inheritdoc />
		public SecurityId? TryGetStockSharpId(SecurityId adapterId) => _inMemory.TryGetStockSharpId(adapterId);

		/// <inheritdoc />
		public SecurityId? TryGetAdapterId(SecurityId stockSharpId) => _inMemory.TryGetAdapterId(stockSharpId);

		private void SaveToFile(bool overwrite, IEnumerable<SecurityIdMapping> mappings)
		{
			var arr = mappings.ToArray();

			_provider._executor.Add(() =>
			{
				var fileName = Path.Combine(_provider._path, _storageName + ".csv");

				var appendHeader = overwrite || !_provider._fileSystem.FileExists(fileName) || _provider._fileSystem.GetFileLength(fileName) == 0;

				if (arr.Length == 0)
				{
					if (appendHeader)
						_provider._fileSystem.DeleteFile(fileName);

					return;
				}

				var mode = overwrite ? FileMode.Create : FileMode.Append;

				using var stream = new TransactionFileStream(_provider._fileSystem, fileName, mode);
				using var writer = stream.CreateCsvWriter();

				if (appendHeader)
				{
					writer.WriteRow(
					[
						"SecurityCode",
						"BoardCode",
						"AdapterCode",
						"AdapterBoard",
					]);
				}

				foreach (var mapping in arr)
				{
					writer.WriteRow(
					[
						mapping.StockSharpId.SecurityCode,
						mapping.StockSharpId.BoardCode,
						mapping.AdapterId.SecurityCode,
						mapping.AdapterId.BoardCode,
					]);
				}

				writer.Commit();
			});
		}
	}

	private readonly SynchronizedDictionary<string, CsvSecurityMappingStorage> _storages = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly InMemorySecurityMappingStorageProvider _inMemoryProvider = new();

	private readonly string _path;
	private readonly ChannelExecutor _executor;
	private readonly IFileSystem _fileSystem;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvSecurityMappingStorageProvider"/>.
	/// </summary>
	/// <param name="path">Path to storage.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	[Obsolete("Use IFileSystem overload.")]
	public CsvSecurityMappingStorageProvider(string path, ChannelExecutor executor)
		: this(Paths.FileSystem, path, executor)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvSecurityMappingStorageProvider"/>.
	/// </summary>
	/// <param name="fileSystem"><see cref="IFileSystem"/></param>
	/// <param name="path">Path to storage.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	public CsvSecurityMappingStorageProvider(IFileSystem fileSystem, string path, ChannelExecutor executor)
	{
		_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

		if (path == null)
			throw new ArgumentNullException(nameof(path));

		_path = path.ToFullPath();
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_storages.Clear();
		_inMemoryProvider.Dispose();

		base.DisposeManaged();
	}

	/// <inheritdoc />
	public IEnumerable<string> StorageNames => _inMemoryProvider.StorageNames;

	/// <inheritdoc />
	public ISecurityMappingStorage GetStorage(string storageName)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		return _storages.SafeAdd(storageName, key =>
		{
			var inMemory = (InMemorySecurityMappingStorage)_inMemoryProvider.GetStorage(key);
			return new CsvSecurityMappingStorage(this, key, inMemory);
		});
	}

	/// <inheritdoc />
	public async ValueTask<Dictionary<string, Exception>> InitAsync(CancellationToken cancellationToken)
	{
		_fileSystem.CreateDirectory(_path);

		var errors = await _inMemoryProvider.InitAsync(cancellationToken);

		var files = _fileSystem.EnumerateFiles(_path, "*.csv");

		foreach (var fileName in files)
		{
			try
			{
				await LoadFileAsync(fileName, cancellationToken);
			}
			catch (Exception ex)
			{
				errors.Add(fileName, ex);
			}
		}

		return errors;
	}

	private Task LoadFileAsync(string fileName, CancellationToken cancellationToken)
	{
		return Do.InvariantAsync(async () =>
		{
			if (!_fileSystem.FileExists(fileName))
				return;

			var pairs = new List<(SecurityId, SecurityId)>();

			using (var reader = _fileSystem.OpenRead(fileName).CreateCsvReader(Encoding.UTF8, false))
			{
				await reader.NextLineAsync(cancellationToken);

				while (await reader.NextLineAsync(cancellationToken))
				{
					var stockSharpId = new SecurityId
					{
						SecurityCode = reader.ReadString(),
						BoardCode = reader.ReadString()
					};
					var adapterId = new SecurityId
					{
						SecurityCode = reader.ReadString(),
						BoardCode = reader.ReadString()
					};

					pairs.Add((stockSharpId, adapterId));
				}
			}

			((InMemorySecurityMappingStorage)_inMemoryProvider.GetStorage(Path.GetFileNameWithoutExtension(fileName))).Load(pairs);
		});
	}
}
