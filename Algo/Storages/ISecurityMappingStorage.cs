namespace StockSharp.Algo.Storages;

/// <summary>
/// Security identifier mappings storage.
/// </summary>
public interface ISecurityMappingStorage
{
	/// <summary>
	/// The new native security identifier added to storage.
	/// </summary>
	event Action<string, SecurityIdMapping> Changed;

	/// <summary>
	/// Initialize the storage.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
	ValueTask<Dictionary<string, Exception>> InitAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Get storage names.
	/// </summary>
	/// <returns>Storage names.</returns>
	IEnumerable<string> GetStorageNames();

	/// <summary>
	/// Get security identifier mappings for storage. 
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <returns>Security identifiers mapping.</returns>
	IEnumerable<SecurityIdMapping> Get(string storageName);

	/// <summary>
	/// Save security identifier mapping.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="mapping">Security identifier mapping.</param>
	/// <returns><see langword="true"/> if security mapping was added. If was changed, <see langword="false" />.</returns>
	bool Save(string storageName, SecurityIdMapping mapping);

	/// <summary>
	/// Remove security mapping.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="stockSharpId">StockSharp format.</param>
	/// <returns><see langword="true"/> if mapping was added. Otherwise, <see langword="false" />.</returns>
	bool Remove(string storageName, SecurityId stockSharpId);

	/// <summary>
	/// Try get <see cref="SecurityIdMapping.StockSharpId"/>.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="adapterId">Adapter format.</param>
	/// <returns><see cref="SecurityIdMapping.StockSharpId"/> if identifier exists. Otherwise, <see langword="null" />.</returns>
	SecurityId? TryGetStockSharpId(string storageName, SecurityId adapterId);

	/// <summary>
	/// Try get <see cref="SecurityIdMapping.AdapterId"/>.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="stockSharpId">StockSharp format.</param>
	/// <returns><see cref="SecurityIdMapping.AdapterId"/> if identifier exists. Otherwise, <see langword="null" />.</returns>
	SecurityId? TryGetAdapterId(string storageName, SecurityId stockSharpId);
}

/// <summary>
/// In memory security identifier mappings storage.
/// </summary>
public class InMemorySecurityMappingStorage : ISecurityMappingStorage
{
	private readonly SynchronizedDictionary<string, PairSet<SecurityId, SecurityId>> _mappings = new(StringComparer.InvariantCultureIgnoreCase);

	private Action<string, SecurityIdMapping> _changed;

	event Action<string, SecurityIdMapping> ISecurityMappingStorage.Changed
	{
		add => _changed += value;
		remove => _changed -= value;
	}

	ValueTask<Dictionary<string, Exception>> ISecurityMappingStorage.InitAsync(CancellationToken cancellationToken)
	{
		return new([]);
	}

	IEnumerable<string> ISecurityMappingStorage.GetStorageNames()
	{
		using (_mappings.EnterScope())
			return [.. _mappings.Keys];
	}

	IEnumerable<SecurityIdMapping> ISecurityMappingStorage.Get(string storageName)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		using (_mappings.EnterScope())
			return _mappings.TryGetValue(storageName)?.Select(p => (SecurityIdMapping)p).ToArray() ?? Enumerable.Empty<SecurityIdMapping>();
	}

	bool ISecurityMappingStorage.Save(string storageName, SecurityIdMapping mapping)
	{
		return Save(storageName, mapping, out _);
	}

	internal bool Save(string storageName, SecurityIdMapping mapping, out IEnumerable<SecurityIdMapping> all)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		if (mapping == default)
			throw new ArgumentNullException(nameof(mapping));

		var added = false;

		using (_mappings.EnterScope())
		{
			var mappings = _mappings.SafeAdd(storageName);

			var stockSharpId = mapping.StockSharpId;
			var adapterId = mapping.AdapterId;

			if (mappings.Remove(stockSharpId))
			{
			}
			else if (mappings.ContainsValue(adapterId))
			{
				mappings.RemoveByValue(adapterId);
			}
			else
				added = true;

			mappings.Add(stockSharpId, adapterId);

			all = added ? null : mappings.Select(p => (SecurityIdMapping)p).ToArray();
		}

		_changed?.Invoke(storageName, mapping);

		return added;
	}

	bool ISecurityMappingStorage.Remove(string storageName, SecurityId stockSharpId)
	{
		return Remove(storageName, stockSharpId, out _);
	}

	SecurityId? ISecurityMappingStorage.TryGetStockSharpId(string storageName, SecurityId adapterId)
	{
		using (_mappings.EnterScope())
		{
			if (!_mappings.TryGetValue(storageName, out var mappings))
				return null;

			if (!mappings.TryGetKey(adapterId, out var stockSharpId))
				return null;

			return stockSharpId;
		}
	}

	SecurityId? ISecurityMappingStorage.TryGetAdapterId(string storageName, SecurityId stockSharpId)
	{
		using (_mappings.EnterScope())
		{
			if (!_mappings.TryGetValue(storageName, out var mappings))
				return null;

			if (!mappings.TryGetValue(stockSharpId, out var adapterId))
				return null;

			return adapterId;
		}
	}

	internal bool Remove(string storageName, SecurityId stockSharpId, out IEnumerable<SecurityIdMapping> all)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		if (stockSharpId == default)
			throw new ArgumentNullException(nameof(storageName));

		all = null;

		using (_mappings.EnterScope())
		{
			var mappings = _mappings.TryGetValue(storageName);

			if (mappings == null)
				return false;

			var removed = mappings.Remove(stockSharpId);

			if (!removed)
				return false;

			all = [.. mappings.Select(p => (SecurityIdMapping)p)];
		}

		_changed?.Invoke(storageName, new SecurityIdMapping { StockSharpId = stockSharpId });

		return true;
	}

	internal void Load(string storageName, List<(SecurityId stockSharpId, SecurityId adapterId)> pairs)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		if (pairs == null)
			throw new ArgumentNullException(nameof(pairs));

		using (_mappings.EnterScope())
		{
			var mappings = _mappings.SafeAdd(storageName);

			foreach (var (stockSharpId, adapterId) in pairs)
				mappings.Add(stockSharpId, adapterId);
		}
	}
}

/// <summary>
/// CSV security identifier mappings storage.
/// </summary>
public sealed class CsvSecurityMappingStorage : ISecurityMappingStorage
{
	private readonly ISecurityMappingStorage _inMemory = new InMemorySecurityMappingStorage();

	private readonly string _path;
	private readonly ChannelExecutor _executor;
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc />
	public event Action<string, SecurityIdMapping> Changed;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvSecurityMappingStorage"/>.
	/// </summary>
	/// <param name="path">Path to storage.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	[Obsolete("Use IFileSystem overload.")]
	public CsvSecurityMappingStorage(string path, ChannelExecutor executor)
		: this(Paths.FileSystem, path, executor)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvSecurityMappingStorage"/>.
	/// </summary>
	/// <param name="fileSystem"><see cref="IFileSystem"/></param>
	/// <param name="path">Path to storage.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	public CsvSecurityMappingStorage(IFileSystem fileSystem, string path, ChannelExecutor executor)
	{
		_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

		if (path == null)
			throw new ArgumentNullException(nameof(path));

		_path = path.ToFullPath();
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));
	}

	/// <inheritdoc />
	public async ValueTask<Dictionary<string, Exception>> InitAsync(CancellationToken cancellationToken)
	{
		_fileSystem.CreateDirectory(_path);

		var errors = await _inMemory.InitAsync(cancellationToken);

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

	/// <inheritdoc />
	public IEnumerable<string> GetStorageNames() => _inMemory.GetStorageNames();

	/// <inheritdoc />
	public IEnumerable<SecurityIdMapping> Get(string storageName) => _inMemory.Get(storageName);

	/// <inheritdoc />
	public bool Save(string storageName, SecurityIdMapping mapping)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		if (mapping == default)
			throw new ArgumentNullException(nameof(mapping));

		var added = ((InMemorySecurityMappingStorage)_inMemory).Save(storageName, mapping, out var all);

		if (added)
			Save(storageName, false, [mapping]);
		else
			Save(storageName, true, all);

		Changed?.Invoke(storageName, mapping);

		return added;
	}

	/// <inheritdoc />
	public bool Remove(string storageName, SecurityId stockSharpId)
	{
		if (!((InMemorySecurityMappingStorage)_inMemory).Remove(storageName, stockSharpId, out var all))
			return false;

		Save(storageName, true, all);

		Changed?.Invoke(storageName, new SecurityIdMapping { StockSharpId = stockSharpId });

		return true;
	}

	/// <inheritdoc />
	public SecurityId? TryGetStockSharpId(string storageName, SecurityId adapterId)
	{
		return _inMemory.TryGetStockSharpId(storageName, adapterId);
	}

	/// <inheritdoc />
	public SecurityId? TryGetAdapterId(string storageName, SecurityId stockSharpId)
	{
		return _inMemory.TryGetAdapterId(storageName, stockSharpId);
	}

	private async ValueTask LoadFileAsync(string fileName, CancellationToken cancellationToken)
	{
		await Do.InvariantAsync(async () =>
		{
			if (!_fileSystem.FileExists(fileName))
				return;

			var pairs = new List<(SecurityId, SecurityId)>();

			using (var stream = _fileSystem.OpenRead(fileName))
			{
				var reader = stream.CreateCsvReader(Encoding.UTF8);

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

			((InMemorySecurityMappingStorage)_inMemory).Load(Path.GetFileNameWithoutExtension(fileName), pairs);
		});
	}

	private void Save(string name, bool overwrite, IEnumerable<SecurityIdMapping> mappings)
	{
		_executor.Add(() =>
		{
			var fileName = Path.Combine(_path, name + ".csv");

			var appendHeader = overwrite || !_fileSystem.FileExists(fileName) || _fileSystem.GetFileLength(fileName) == 0;
			var mode = overwrite ? FileMode.Create : FileMode.Append;

			using var stream = new TransactionFileStream(_fileSystem, fileName, mode);
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

			foreach (var mapping in mappings)
			{
				writer.WriteRow(
				[
					mapping.StockSharpId.SecurityCode,
					mapping.StockSharpId.BoardCode,
					mapping.AdapterId.SecurityCode,
					mapping.AdapterId.BoardCode,
				]);
			}

			writer.Flush();
			stream.Commit();
		});
	}
}