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
	/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
	IDictionary<string, Exception> Init();

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

	IDictionary<string, Exception> ISecurityMappingStorage.Init()
	{
		return new Dictionary<string, Exception>();
	}

	IEnumerable<string> ISecurityMappingStorage.GetStorageNames()
	{
		lock (_mappings.SyncRoot)
			return [.. _mappings.Keys];
	}

	IEnumerable<SecurityIdMapping> ISecurityMappingStorage.Get(string storageName)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		lock (_mappings.SyncRoot)
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

		lock (_mappings.SyncRoot)
		{
			var mappings = _mappings.SafeAdd(storageName);

			var stockSharpId = mapping.StockSharpId;
			var adapterId = mapping.AdapterId;

			if (mappings.ContainsKey(stockSharpId))
			{
				mappings.Remove(stockSharpId);
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
		lock (_mappings.SyncRoot)
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
		lock (_mappings.SyncRoot)
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

		lock (_mappings.SyncRoot)
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

	internal void Load(string storageName, List<Tuple<SecurityId, SecurityId>> pairs)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		if (pairs == null)
			throw new ArgumentNullException(nameof(pairs));

		lock (_mappings.SyncRoot)
		{
			var mappings = _mappings.SafeAdd(storageName);

			foreach (var tuple in pairs)
				mappings.Add(tuple.Item1, tuple.Item2);
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

	private DelayAction _delayAction;

	/// <summary>
	/// The time delayed action.
	/// </summary>
	public DelayAction DelayAction
	{
		get => _delayAction;
		set => _delayAction = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	public event Action<string, SecurityIdMapping> Changed;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvSecurityMappingStorage"/>.
	/// </summary>
	/// <param name="path">Path to storage.</param>
	public CsvSecurityMappingStorage(string path)
	{
		if (path == null)
			throw new ArgumentNullException(nameof(path));

		_path = path.ToFullPath();
		_delayAction = new DelayAction(ex => ex.LogError());
	}

	/// <inheritdoc />
	public IDictionary<string, Exception> Init()
	{
		Directory.CreateDirectory(_path);

		var errors = _inMemory.Init();

		var files = Directory.GetFiles(_path, "*.csv");

		foreach (var fileName in files)
		{
			try
			{
				LoadFile(fileName);
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

	private void LoadFile(string fileName)
	{
		Do.Invariant(() =>
		{
			if (!File.Exists(fileName))
				return;

			var pairs = new List<Tuple<SecurityId, SecurityId>>();

			using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				var reader = stream.CreateCsvReader(Encoding.UTF8);

				reader.NextLine();

				while (reader.NextLine())
				{
					var securityId = new SecurityId
					{
						SecurityCode = reader.ReadString(),
						BoardCode = reader.ReadString()
					};
					var adapterId = new SecurityId
					{
						SecurityCode = reader.ReadString(),
						BoardCode = reader.ReadString()
					};

					pairs.Add(Tuple.Create(securityId, adapterId));
				}
			}

			((InMemorySecurityMappingStorage)_inMemory).Load(Path.GetFileNameWithoutExtension(fileName), pairs);
		});
	}

	private void Save(string name, bool overwrite, IEnumerable<SecurityIdMapping> mappings)
	{
		DelayAction.DefaultGroup.Add(() =>
		{
			var fileName = Path.Combine(_path, name + ".csv");

			var appendHeader = overwrite || !File.Exists(fileName) || new FileInfo(fileName).Length == 0;
			var mode = overwrite ? FileMode.Create : FileMode.Append;

			using var writer = new TransactionFileStream(fileName, mode).CreateCsvWriter();

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
		});
	}
}