namespace StockSharp.Algo.Storages;

/// <summary>
/// Extended info storage.
/// </summary>
public interface IExtendedInfoStorageItem
{
	/// <summary>
	/// Extended fields (names and types).
	/// </summary>
	IEnumerable<(string name, Type type)> Fields { get; }

	/// <summary>
	/// Get all security identifiers.
	/// </summary>
	IEnumerable<SecurityId> Securities { get; }

	/// <summary>
	/// Storage name.
	/// </summary>
	string StorageName { get; }

	/// <summary>
	/// Initialize the storage.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	ValueTask InitAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Add extended info.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="extensionInfo">Extended information.</param>
	void Add(SecurityId securityId, IDictionary<string, object> extensionInfo);

	/// <summary>
	/// Load extended info.
	/// </summary>
	/// <returns>Extended information.</returns>
	IEnumerable<(SecurityId secId, IDictionary<string, object> fields)> Load();

	/// <summary>
	/// Load extended info.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <returns>Extended information.</returns>
	IDictionary<string, object> Load(SecurityId securityId);

	/// <summary>
	/// Delete extended info.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	void Delete(SecurityId securityId);
}

/// <summary>
/// Extended info storage.
/// </summary>
public interface IExtendedInfoStorage
{
	/// <summary>
	/// Get all extended storages.
	/// </summary>
	IEnumerable<IExtendedInfoStorageItem> Storages { get; }

	/// <summary>
	/// Initialize the storage.
	/// </summary>
	/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
	ValueTask<Dictionary<IExtendedInfoStorageItem, Exception>> InitAsync(CancellationToken cancellationToken);

	/// <summary>
	/// To get storage for the specified name.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Storage.</returns>
	ValueTask<IExtendedInfoStorageItem> GetAsync(string storageName, CancellationToken cancellationToken);

	/// <summary>
	/// To create storage.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="fields">Extended fields (names and types).</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Storage.</returns>
	ValueTask<IExtendedInfoStorageItem> CreateAsync(string storageName, IEnumerable<(string name, Type type)> fields, CancellationToken cancellationToken);

	/// <summary>
	/// Delete storage.
	/// </summary>
	/// <param name="storage">Storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	ValueTask DeleteAsync(IExtendedInfoStorageItem storage, CancellationToken cancellationToken);

	/// <summary>
	/// The storage was created.
	/// </summary>
	event Action<IExtendedInfoStorageItem> Created;

	/// <summary>
	/// The storage was deleted.
	/// </summary>
	event Action<IExtendedInfoStorageItem> Deleted;
}

/// <summary>
/// Extended info storage, used csv files.
/// </summary>
public class CsvExtendedInfoStorage : IExtendedInfoStorage
{
	private class CsvExtendedInfoStorageItem : IExtendedInfoStorageItem
	{
		private readonly CsvExtendedInfoStorage _storage;
		private readonly string _fileName;
		private (string name, Type type)[] _fields;
		private readonly Lock _lock = new();
		//private readonly Dictionary<string, Type> _fieldTypes = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<SecurityId, Dictionary<string, object>> _cache = [];
		private readonly ChannelExecutor _executor;

		private IFileSystem FileSystem => _storage._fileSystem;

		public CsvExtendedInfoStorageItem(CsvExtendedInfoStorage storage, string fileName, ChannelExecutor executor)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException(nameof(fileName));

			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
			_fileName = fileName;
			_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		}

		public CsvExtendedInfoStorageItem(CsvExtendedInfoStorage storage, string fileName, IEnumerable<(string, Type)> fields, ChannelExecutor executor)
			: this(storage, fileName, executor)
		{
			if (fields == null)
				throw new ArgumentNullException(nameof(fields));

			_fields = [.. fields];

			if (_fields.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(fields));
		}

		public string StorageName => Path.GetFileNameWithoutExtension(_fileName);

		public async ValueTask InitAsync(CancellationToken cancellationToken)
		{
			if (FileSystem.FileExists(_fileName))
			{
				await Do.InvariantAsync(async () =>
				{
					using var reader = FileSystem.OpenRead(_fileName).CreateCsvReader(Encoding.UTF8, false);

					await reader.NextLineAsync(cancellationToken);
					reader.Skip();

					var fields = new string[reader.ColumnCount - 1];

					for (var i = 0; i < fields.Length; i++)
						fields[i] = reader.ReadString();

					await reader.NextLineAsync(cancellationToken);
					reader.Skip();

					var types = new Type[reader.ColumnCount - 1];

					for (var i = 0; i < types.Length; i++)
					{
						types[i] = reader.ReadString().To<Type>();
						//_fieldTypes.Add(fields[i], types[i]);
					}

					if (_fields == null)
					{
						if (fields.Length != types.Length)
							throw new InvalidOperationException($"{fields.Length} != {types.Length}");

						_fields = [.. fields.Select((f, i) => (f, types[i]))];
					}

					while (await reader.NextLineAsync(cancellationToken))
					{
						var secId = reader.ReadString().ToSecurityId();

						var values = new Dictionary<string, object>();

						for (var i = 0; i < fields.Length; i++)
						{
							values[fields[i]] = reader.ReadString().To(types[i]);
						}

						_cache.Add(secId, values);
					}
				});
			}
			else
			{
				if (_fields == null)
					throw new InvalidOperationException();

				Write([]);
			}
		}

		private void Flush()
		{
			_executor.Add(() => Write(((IExtendedInfoStorageItem)this).Load()));
		}

		private void Write(IEnumerable<(SecurityId secId, IDictionary<string, object> fields)> values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			using var stream = new TransactionFileStream(FileSystem, _fileName, FileMode.Create);
			using var writer = stream.CreateCsvWriter();

			writer.WriteRow(new[] { nameof(SecurityId) }.Concat(_fields.Select(f => f.name)));
			writer.WriteRow(new[] { typeof(string) }.Concat(_fields.Select(f => f.type)).Select(t => t.TryGetCSharpAlias() ?? t.GetTypeName(false)));

			foreach (var (secId, fields) in values)
			{
				writer.WriteRow(new[] { secId.ToStringId() }.Concat(_fields.Select(f => fields.TryGetValue(f.name)?.To<string>())));
			}

			writer.Commit();
		}

		public void Delete()
		{
			_executor.Add(() =>
			{
				FileSystem.DeleteFile(_fileName);
			});

			_storage._deleted?.Invoke(this);
		}

		IEnumerable<(string, Type)> IExtendedInfoStorageItem.Fields => _fields;

		void IExtendedInfoStorageItem.Add(SecurityId securityId, IDictionary<string, object> extensionInfo)
		{
			using (_lock.EnterScope())
			{
				var dict = _cache.SafeAdd(securityId);

				foreach (var (name, _) in _fields)
				{
					var value = extensionInfo.TryGetValue(name);

					if (value == null)
						continue;

					dict[name] = value;

					//_fieldTypes.TryAdd(field, value.GetType());
				}
			}

			Flush();
		}

		IEnumerable<(SecurityId secId, IDictionary<string, object> fields)> IExtendedInfoStorageItem.Load()
		{
			using (_lock.EnterScope())
			{
				var retVal = new (SecurityId, IDictionary<string, object>)[_cache.Count];

				var i = 0;
				foreach (var pair in _cache)
				{
					retVal[i] = (pair.Key, pair.Value.ToDictionary());
					i++;
				}

				return retVal;
			}
		}

		IDictionary<string, object> IExtendedInfoStorageItem.Load(SecurityId securityId)
		{
			using (_lock.EnterScope())
				return _cache.TryGetValue(securityId)?.ToDictionary();
		}

		void IExtendedInfoStorageItem.Delete(SecurityId securityId)
		{
			using (_lock.EnterScope())
				_cache.Remove(securityId);

			Flush();
		}

		IEnumerable<SecurityId> IExtendedInfoStorageItem.Securities
		{
			get
			{
				using (_lock.EnterScope())
					return [.. _cache.Keys];
			}
		}
	}

	private readonly AsyncReaderWriterLock _itemsLock = new();
	private readonly Dictionary<string, CsvExtendedInfoStorageItem> _items = new(StringComparer.InvariantCultureIgnoreCase);
	private CsvExtendedInfoStorageItem[] _itemsCache;

	private readonly string _path;
	private readonly ChannelExecutor _executor;
	private readonly IFileSystem _fileSystem;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvExtendedInfoStorage"/>.
	/// </summary>
	/// <param name="path">Path to storage.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	[Obsolete("Use IFileSystem overload.")]
	public CsvExtendedInfoStorage(string path, ChannelExecutor executor)
		: this(Paths.FileSystem, path, executor)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvExtendedInfoStorage"/>.
	/// </summary>
	/// <param name="fileSystem"><see cref="IFileSystem"/></param>
	/// <param name="path">Path to storage.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	public CsvExtendedInfoStorage(IFileSystem fileSystem, string path, ChannelExecutor executor)
	{
		_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

		if (path == null)
			throw new ArgumentNullException(nameof(path));

		_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		_path = path.ToFullPath();
		_fileSystem.CreateDirectory(path);
	}

	async ValueTask<IExtendedInfoStorageItem> IExtendedInfoStorage.CreateAsync(string storageName, IEnumerable<(string, Type)> fields, CancellationToken cancellationToken)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		CsvExtendedInfoStorageItem item;

		using (await _itemsLock.WriterLockAsync(cancellationToken))
		{
			if (_items.TryGetValue(storageName, out item))
				return item;

			item = new(this, Path.Combine(_path, storageName + ".csv"), fields, _executor);
			await item.InitAsync(cancellationToken);

			_items.Add(storageName, item);
			_itemsCache = [.. _items.Values];
		}

		_created?.Invoke(item);

		return item;
	}

	async ValueTask IExtendedInfoStorage.DeleteAsync(IExtendedInfoStorageItem storage, CancellationToken cancellationToken)
	{
		if (storage == null)
			throw new ArgumentNullException(nameof(storage));

		bool isRemoved;
		using (await _itemsLock.WriterLockAsync(cancellationToken))
		{
			isRemoved = _items.Remove(storage.StorageName);

			if (isRemoved)
				_itemsCache = [.. _items.Values];
		}

		if (isRemoved)
			((CsvExtendedInfoStorageItem)storage).Delete();
	}

	private Action<IExtendedInfoStorageItem> _created;

	event Action<IExtendedInfoStorageItem> IExtendedInfoStorage.Created
	{
		add => _created += value;
		remove => _created -= value;
	}

	private Action<IExtendedInfoStorageItem> _deleted;

	event Action<IExtendedInfoStorageItem> IExtendedInfoStorage.Deleted
	{
		add => _deleted += value;
		remove => _deleted -= value;
	}

	async ValueTask<IExtendedInfoStorageItem> IExtendedInfoStorage.GetAsync(string storageName, CancellationToken cancellationToken)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		using (await _itemsLock.ReaderLockAsync(cancellationToken))
			return _items.TryGetValue(storageName);
	}

	IEnumerable<IExtendedInfoStorageItem> IExtendedInfoStorage.Storages => _itemsCache;

	/// <inheritdoc />
	public async ValueTask<Dictionary<IExtendedInfoStorageItem, Exception>> InitAsync(CancellationToken cancellationToken)
	{
		var errors = new Dictionary<IExtendedInfoStorageItem, Exception>();

		foreach (var fileName in _fileSystem.EnumerateFiles(_path, "*.csv"))
		{
			var item = new CsvExtendedInfoStorageItem(this, fileName, _executor);

			using (await _itemsLock.WriterLockAsync(cancellationToken))
			{
				_items.Add(Path.GetFileNameWithoutExtension(fileName), item);
				_itemsCache = [.. _items.Values];
			}

			try
			{
				await item.InitAsync(cancellationToken);
			}
			catch (Exception ex)
			{
				errors.Add(item, ex);
			}
		}

		return errors;
	}
}
