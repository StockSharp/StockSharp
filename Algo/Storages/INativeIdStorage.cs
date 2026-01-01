namespace StockSharp.Algo.Storages;

/// <summary>
/// Single storage for security native identifiers.
/// </summary>
public interface INativeIdStorage
{
	/// <summary>
	/// The new native security identifier added to storage.
	/// </summary>
	event Func<SecurityId, object, CancellationToken, ValueTask> Added;

	/// <summary>
	/// Get all native security identifiers.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Security identifiers.</returns>
	ValueTask<(SecurityId secId, object nativeId)[]> GetAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Try add native security identifier.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="nativeId">Native (internal) trading system security id.</param>
	/// <param name="isPersistable">Save the identifier as a permanent.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see langword="true"/> if native identifier was added. Otherwise, <see langword="false" />.</returns>
	ValueTask<bool> TryAddAsync(SecurityId securityId, object nativeId, bool isPersistable = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Try get security identifier by native identifier.
	/// </summary>
	/// <param name="nativeId">Native (internal) trading system security id.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Security identifier.</returns>
	ValueTask<SecurityId?> TryGetByNativeIdAsync(object nativeId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Try get native security identifier by identifier.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Native (internal) trading system security id.</returns>
	ValueTask<object> TryGetBySecurityIdAsync(SecurityId securityId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Clear storage.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	ValueTask ClearAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Remove by security identifier.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="isPersistable">Save the identifier as a permanent.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Operation result.</returns>
	ValueTask<bool> RemoveBySecurityIdAsync(SecurityId securityId, bool isPersistable = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Remove by native identifier.
	/// </summary>
	/// <param name="nativeId">Native (internal) trading system security id.</param>
	/// <param name="isPersistable">Save the identifier as a permanent.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Operation result.</returns>
	ValueTask<bool> RemoveByNativeIdAsync(object nativeId, bool isPersistable = true, CancellationToken cancellationToken = default);
}

/// <summary>
/// Security native identifier storage provider.
/// </summary>
public interface INativeIdStorageProvider : IAsyncDisposable
{
	/// <summary>
	/// Initialize the storage provider.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
	ValueTask<Dictionary<string, Exception>> InitAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Get storage for a specific storage name.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <returns>Storage instance.</returns>
	INativeIdStorage GetStorage(string storageName);
}

/// <summary>
/// CSV security native identifier storage provider.
/// </summary>
public class CsvNativeIdStorageProvider : INativeIdStorageProvider
{
	private class CsvNativeIdStorage(CsvNativeIdStorageProvider provider, string storageName, InMemoryNativeIdStorage inMemory) : Disposable, INativeIdStorage
	{
		private readonly CsvNativeIdStorageProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
		private readonly string _storageName = storageName ?? throw new ArgumentNullException(nameof(storageName));
		private readonly InMemoryNativeIdStorage _inMemory = inMemory ?? throw new ArgumentNullException(nameof(inMemory));

		private readonly SynchronizedDictionary<SecurityId, object> _buffer = [];
		private TransactionFileStream _stream;
		private CsvFileWriter _writer;
		private Func<SecurityId, object, CancellationToken, ValueTask> _added;

		public event Func<SecurityId, object, CancellationToken, ValueTask> Added
		{
			add => _added += value;
			remove => _added -= value;
		}

		public ValueTask<(SecurityId secId, object nativeId)[]> GetAsync(CancellationToken cancellationToken)
			=> _inMemory.GetAsync(cancellationToken);

		public async ValueTask<bool> TryAddAsync(SecurityId securityId, object nativeId, bool isPersistable, CancellationToken cancellationToken)
		{
			var added = await _inMemory.TryAddAsync(securityId, nativeId, isPersistable, cancellationToken);

			if (!added)
				return false;

			if (isPersistable)
				Save(securityId, nativeId);

			var evt = _added;
			if (evt != null)
				await evt.Invoke(securityId, nativeId, cancellationToken);

			return true;
		}

		public async ValueTask ClearAsync(CancellationToken cancellationToken)
		{
			await _inMemory.ClearAsync(cancellationToken);

			_buffer.Clear();

			_provider._executor.Add(() =>
			{
				ResetStream();
				_provider._fileSystem.DeleteFile(GetFileName());
			});
		}

		public async ValueTask<bool> RemoveBySecurityIdAsync(SecurityId securityId, bool isPersistable, CancellationToken cancellationToken)
		{
			var removed = await _inMemory.RemoveBySecurityIdAsync(securityId, isPersistable, cancellationToken);

			if (!removed)
				return false;

			if (isPersistable)
				await SaveAllAsync(cancellationToken);

			return true;
		}

		public async ValueTask<bool> RemoveByNativeIdAsync(object nativeId, bool isPersistable, CancellationToken cancellationToken)
		{
			var removed = await _inMemory.RemoveByNativeIdAsync(nativeId, isPersistable, cancellationToken);

			if (!removed)
				return false;

			if (isPersistable)
				await SaveAllAsync(cancellationToken);

			return true;
		}

		public ValueTask<SecurityId?> TryGetByNativeIdAsync(object nativeId, CancellationToken cancellationToken)
			=> _inMemory.TryGetByNativeIdAsync(nativeId, cancellationToken);

		public ValueTask<object> TryGetBySecurityIdAsync(SecurityId securityId, CancellationToken cancellationToken)
			=> _inMemory.TryGetBySecurityIdAsync(securityId, cancellationToken);

		private void Save(SecurityId securityId, object nativeId)
		{
			_buffer[securityId] = nativeId;

			_provider._executor.Add(() =>
			{
				var items = _buffer.SyncGet(c => c.CopyAndClear()).Select(t => (t.Key, t.Value)).ToArray();
				WriteItemsToFile(items, rewriteAll: false);
			});
		}

		private async ValueTask SaveAllAsync(CancellationToken cancellationToken)
		{
			_buffer.Clear();

			var items = await _inMemory.GetAsync(cancellationToken);

			_provider._executor.Add(() => WriteItemsToFile(items, rewriteAll: true));
		}

		private void WriteItemsToFile((SecurityId secId, object nativeId)[] items, bool rewriteAll)
		{
			var fileName = GetFileName();
			var fs = _provider._fileSystem;

			if (rewriteAll)
			{
				ResetStream();

				if (fs.FileExists(fileName))
					fs.DeleteFile(fileName);

				if (items.Length == 0)
					return;
			}
			else if (items.Length == 0)
			{
				return;
			}

			var appendHeader = !fs.FileExists(fileName) || fs.GetFileLength(fileName) == 0;

			EnsureStream();

			if (appendHeader)
				WriteHeader(_writer, items[0].nativeId);

			foreach (var (secId, nativeId) in items)
				WriteItem(_writer, secId, nativeId);

			_writer.Flush();
			_stream.Commit();
		}

		private void EnsureStream()
		{
			if (_stream != null)
				return;

			var fileName = GetFileName();
			_stream = new TransactionFileStream(_provider._fileSystem, fileName, FileMode.Append);
			_writer = _stream.CreateCsvWriter(leaveOpen: false);
		}

		private void ResetStream()
		{
			_writer?.Dispose();
			_writer = null;
		}

		private string GetFileName() => Path.Combine(_provider._path, _storageName + ".csv");

		protected override void DisposeManaged()
		{
			ResetStream();
			base.DisposeManaged();
		}
	}

	private readonly SynchronizedDictionary<string, CsvNativeIdStorage> _storages = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly InMemoryNativeIdStorageProvider _inMemoryProvider = new();

	private readonly string _path;
	private readonly ChannelExecutor _executor;
	private readonly IFileSystem _fileSystem;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvNativeIdStorageProvider"/>.
	/// </summary>
	/// <param name="path">Path to storage.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	[Obsolete("Use IFileSystem overload.")]
	public CsvNativeIdStorageProvider(string path, ChannelExecutor executor)
		: this(Paths.FileSystem, path, executor)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvNativeIdStorageProvider"/>.
	/// </summary>
	/// <param name="fileSystem"><see cref="IFileSystem"/></param>
	/// <param name="path">Path to storage.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	public CsvNativeIdStorageProvider(IFileSystem fileSystem, string path, ChannelExecutor executor)
	{
		_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

		if (path == null)
			throw new ArgumentNullException(nameof(path));

		_path = path.ToFullPath();
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await _executor.AddAndWaitAsync(() =>
		{
			foreach (var storage in _storages.Values)
				storage.Dispose();

			_storages.Clear();
		});

		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	public INativeIdStorage GetStorage(string storageName)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		return _storages.SafeAdd(storageName, key =>
		{
			var inMemory = (InMemoryNativeIdStorage)_inMemoryProvider.GetStorage(key);
			return new CsvNativeIdStorage(this, key, inMemory);
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

	private async ValueTask LoadFileAsync(string fileName, CancellationToken cancellationToken)
	{
		await Do.InvariantAsync(async () =>
		{
			if (!_fileSystem.FileExists(fileName))
				return;

			var name = Path.GetFileNameWithoutExtension(fileName);

			var pairs = new List<(SecurityId, object)>();

			using (var reader = _fileSystem.OpenRead(fileName).CreateCsvReader(Encoding.UTF8, false))
			{
				await reader.NextLineAsync(cancellationToken);
				reader.Skip(2);

				var types = new List<Type>();

				while ((reader.ColumnCurr + 1) < reader.ColumnCount)
					types.Add(reader.ReadString().To<Type>());

				var isTuple = types.Count > 1;

				while (await reader.NextLineAsync(cancellationToken))
				{
					var securityId = new SecurityId
					{
						SecurityCode = reader.ReadString(),
						BoardCode = reader.ReadString()
					};

					object nativeId;

					if (isTuple)
					{
						var args = new List<object>();

						for (var i = 0; i < types.Count; i++)
							args.Add(reader.ReadString().To(types[i]));

						nativeId = args.ToTuple(true);
					}
					else
						nativeId = reader.ReadString().To(types[0]);

					pairs.Add((securityId, nativeId));
				}
			}

			var inMemory = (InMemoryNativeIdStorage)_inMemoryProvider.GetStorage(name);
			inMemory.Add(pairs);
		});
	}

	private static object[] TryTupleToValues(object nativeId)
	{
		if (nativeId is null)
			throw new ArgumentNullException(nameof(nativeId));

		if (!nativeId.GetType().IsTuple())
			return null;

		var tupleValues = nativeId.ToValues().ToArray();

		if (tupleValues.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(nativeId), nativeId, LocalizedStrings.InvalidValue);

		return tupleValues;
	}

	private static void WriteHeader(CsvFileWriter writer, object nativeId)
	{
		var tupleValues = TryTupleToValues(nativeId);

		const string symbol = "Symbol";
		const string board = "Board";

		if (tupleValues is not null)
		{
			writer.WriteRow(new[]
			{
				symbol,
				board,
			}.Concat(tupleValues.Select(v => GetTypeName(v.GetType()))));
		}
		else
		{
			writer.WriteRow(
			[
				symbol,
				board,
				GetTypeName(nativeId.GetType()),
			]);
		}
	}

	private static void WriteItem(CsvFileWriter writer, SecurityId securityId, object nativeId)
	{
		var tupleValues = TryTupleToValues(nativeId);

		if (tupleValues is not null)
		{
			writer.WriteRow(new[]
			{
				securityId.SecurityCode,
				securityId.BoardCode
			}.Concat(tupleValues.Select(v => v.To<string>())));
		}
		else
		{
			writer.WriteRow(
			[
				securityId.SecurityCode,
				securityId.BoardCode,
				nativeId.ToString()
			]);
		}
	}

	private static string GetTypeName(Type nativeIdType) => nativeIdType.TryGetCSharpAlias() ?? nativeIdType.GetTypeName(false);
}

/// <summary>
/// In memory security native identifier storage.
/// </summary>
public class InMemoryNativeIdStorage : INativeIdStorage
{
	private readonly PairSet<SecurityId, object> _nativeIds = [];
	private readonly Lock _syncRoot = new();

	private Func<SecurityId, object, CancellationToken, ValueTask> _added;

	/// <inheritdoc />
	public event Func<SecurityId, object, CancellationToken, ValueTask> Added
	{
		add => _added += value;
		remove => _added -= value;
	}

	internal void Add(IEnumerable<(SecurityId secId, object nativeId)> ids)
	{
		if (ids == null)
			throw new ArgumentNullException(nameof(ids));

		using (_syncRoot.EnterScope())
		{
			foreach (var (secId, nativeId) in ids)
			{
				// skip duplicates
				if (_nativeIds.ContainsKey(secId) || _nativeIds.ContainsValue(nativeId))
					continue;

				_nativeIds.Add(secId, nativeId);
			}
		}
	}

	/// <inheritdoc />
	public async ValueTask<bool> TryAddAsync(SecurityId securityId, object nativeId, bool isPersistable, CancellationToken cancellationToken)
	{
		if (nativeId == null)
			throw new ArgumentNullException(nameof(nativeId));

		using (_syncRoot.EnterScope())
		{
			var added = _nativeIds.TryAdd(securityId, nativeId);

			if (!added)
				return false;
		}

		var evt = _added;
		if (evt != null)
			await evt.Invoke(securityId, nativeId, cancellationToken);

		return true;
	}

	/// <inheritdoc />
	public ValueTask<object> TryGetBySecurityIdAsync(SecurityId securityId, CancellationToken cancellationToken)
	{
		using (_syncRoot.EnterScope())
			return new(_nativeIds.TryGetValue(securityId));
	}

	/// <inheritdoc />
	public ValueTask ClearAsync(CancellationToken cancellationToken)
	{
		using (_syncRoot.EnterScope())
			_nativeIds.Clear();

		return default;
	}

	/// <inheritdoc />
	public ValueTask<SecurityId?> TryGetByNativeIdAsync(object nativeId, CancellationToken cancellationToken)
	{
		var securityId = default(SecurityId);

		using (_syncRoot.EnterScope())
		{
			if (!_nativeIds.TryGetKey(nativeId, out securityId))
				return new((SecurityId?)null);
		}

		return new(securityId);
	}

	/// <inheritdoc />
	public ValueTask<(SecurityId secId, object nativeId)[]> GetAsync(CancellationToken cancellationToken)
	{
		using (_syncRoot.EnterScope())
			return new([.. _nativeIds.Select(p => (p.Key, p.Value))]);
	}

	/// <inheritdoc />
	public ValueTask<bool> RemoveBySecurityIdAsync(SecurityId securityId, bool isPersistable, CancellationToken cancellationToken)
	{
		using (_syncRoot.EnterScope())
			return new(_nativeIds.Remove(securityId));
	}

	/// <inheritdoc />
	public ValueTask<bool> RemoveByNativeIdAsync(object nativeId, bool isPersistable, CancellationToken cancellationToken)
	{
		using (_syncRoot.EnterScope())
			return new(_nativeIds.RemoveByValue(nativeId));
	}
}

/// <summary>
/// In memory security native identifier storage provider.
/// </summary>
public class InMemoryNativeIdStorageProvider : INativeIdStorageProvider
{
	private readonly SynchronizedDictionary<string, InMemoryNativeIdStorage> _storages = new(StringComparer.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	public ValueTask<Dictionary<string, Exception>> InitAsync(CancellationToken cancellationToken)
		=> new([]);

	/// <inheritdoc />
	public INativeIdStorage GetStorage(string storageName)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		return _storages.SafeAdd(storageName, _ => new InMemoryNativeIdStorage());
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		_storages.Clear();
		GC.SuppressFinalize(this);
		return default;
	}
}