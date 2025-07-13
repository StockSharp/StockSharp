namespace StockSharp.Algo.Storages;

/// <summary>
/// Security native identifier storage.
/// </summary>
public interface INativeIdStorage
{
	/// <summary>
	/// The new native security identifier added to storage.
	/// </summary>
	event Action<string, SecurityId, object> Added;

	/// <summary>
	/// Initialize the storage.
	/// </summary>
	/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
	IDictionary<string, Exception> Init();

	/// <summary>
	/// Get native security identifiers for storage.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <returns>Security identifiers.</returns>
	(SecurityId secId, object nativeId)[] Get(string storageName);

	/// <summary>
	/// Try add native security identifier to storage.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="nativeId">Native (internal) trading system security id.</param>
	/// <param name="isPersistable">Save the identifier as a permanent.</param>
	/// <returns><see langword="true"/> if native identifier was added. Otherwise, <see langword="false" />.</returns>
	bool TryAdd(string storageName, SecurityId securityId, object nativeId, bool isPersistable = true);

	/// <summary>
	/// Try get security identifier by native identifier.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="nativeId">Native (internal) trading system security id.</param>
	/// <returns>Security identifier.</returns>
	SecurityId? TryGetByNativeId(string storageName, object nativeId);

	/// <summary>
	/// Try get native security identifier by identifier.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="securityId">Security identifier.</param>
	/// <returns>Native (internal) trading system security id.</returns>
	object TryGetBySecurityId(string storageName, SecurityId securityId);

	/// <summary>
	/// Clear storage.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	void Clear(string storageName);

	/// <summary>
	///Remove by security identifier.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="isPersistable">Save the identifier as a permanent.</param>
	/// <returns>Operation result.</returns>
	bool RemoveBySecurityId(string storageName, SecurityId securityId, bool isPersistable = true);

	/// <summary>
	/// Remove by native identifier.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="nativeId">Native (internal) trading system security id.</param>
	/// <param name="isPersistable">Save the identifier as a permanent.</param>
	/// <returns>Operation result.</returns>
	bool RemoveByNativeId(string storageName, object nativeId, bool isPersistable = true);
}

/// <summary>
/// CSV security native identifier storage.
/// </summary>
public sealed class CsvNativeIdStorage : INativeIdStorage
{
	private readonly INativeIdStorage _inMemory = new InMemoryNativeIdStorage();
	private readonly SynchronizedDictionary<SecurityId, object> _buffer = [];

	private readonly string _path;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvNativeIdStorage"/>.
	/// </summary>
	/// <param name="path">Path to storage.</param>
	public CsvNativeIdStorage(string path)
	{
		if (path == null)
			throw new ArgumentNullException(nameof(path));

		_path = path.ToFullPath();
		_delayAction = new DelayAction(ex => ex.LogError());
	}

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
	public event Action<string, SecurityId, object> Added;

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
	public (SecurityId, object)[] Get(string storageName) => _inMemory.Get(storageName);

	/// <inheritdoc />
	public bool TryAdd(string storageName, SecurityId securityId, object nativeId, bool isPersistable)
	{
		var added = _inMemory.TryAdd(storageName, securityId, nativeId, isPersistable);

		if (!added)
			return false;

		if (isPersistable)
			Save(storageName, securityId, nativeId);

		Added?.Invoke(storageName, securityId, nativeId);

		return true;
	}

	/// <inheritdoc />
	public void Clear(string storageName)
	{
		_inMemory.Clear(storageName);
		_buffer.Clear();
		DelayAction.DefaultGroup.Add(() => File.Delete(GetFileName(storageName)));
	}

	/// <inheritdoc />
	public bool RemoveBySecurityId(string storageName, SecurityId securityId, bool isPersistable)
	{
		var added = _inMemory.RemoveBySecurityId(storageName, securityId, isPersistable);

		if (!added)
			return false;

		if (isPersistable)
			SaveAll(storageName);

		return true;
	}

	/// <inheritdoc />
	public bool RemoveByNativeId(string storageName, object nativeId, bool isPersistable)
	{
		var added = _inMemory.RemoveByNativeId(storageName, nativeId, isPersistable);

		if (!added)
			return false;

		if (isPersistable)
			SaveAll(storageName);

		return true;
	}

	private void SaveAll(string storageName)
	{
		_buffer.Clear();

		DelayAction.DefaultGroup.Add(() =>
		{
			var fileName = GetFileName(storageName);

			File.Delete(fileName);

			var items = _inMemory.Get(storageName);

			if (items.Length == 0)
				return;

			using var writer = new TransactionFileStream(fileName, FileMode.Append).CreateCsvWriter();

			WriteHeader(writer, items.FirstOrDefault().nativeId);

			foreach (var (secId, nativeId) in items)
				WriteItem(writer, secId, nativeId);
		});
	}

	/// <inheritdoc />
	public SecurityId? TryGetByNativeId(string storageName, object nativeId)
		=> _inMemory.TryGetByNativeId(storageName, nativeId);

	/// <inheritdoc />
	public object TryGetBySecurityId(string storageName, SecurityId securityId)
		=> _inMemory.TryGetBySecurityId(storageName, securityId);

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

	private void Save(string storageName, SecurityId securityId, object nativeId)
	{
		_buffer[securityId] = nativeId;

		DelayAction.DefaultGroup.Add(() =>
		{
			var items = _buffer.SyncGet(c => c.CopyAndClear());

			if (items.Length == 0)
				return;

			var fileName = GetFileName(storageName);

			var appendHeader = !File.Exists(fileName) || new FileInfo(fileName).Length == 0;

			using var writer = new TransactionFileStream(fileName, FileMode.Append).CreateCsvWriter();

			if (appendHeader)
				WriteHeader(writer, nativeId);

			foreach (var item in items)
				WriteItem(writer, item.Key, item.Value);
		});
	}

	private string GetFileName(string storageName) => Path.Combine(_path, storageName + ".csv");

	private static string GetTypeName(Type nativeIdType) => nativeIdType.TryGetCSharpAlias() ?? nativeIdType.GetTypeName(false);

	private void LoadFile(string fileName)
	{
		Do.Invariant(() =>
		{
			if (!File.Exists(fileName))
				return;

			var name = Path.GetFileNameWithoutExtension(fileName);

			var pairs = new List<(SecurityId, object)>();

			using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				var reader = stream.CreateCsvReader(Encoding.UTF8);

				reader.NextLine();
				reader.Skip(2);

				var types = new List<Type>();

				while ((reader.ColumnCurr + 1) < reader.ColumnCount)
					types.Add(reader.ReadString().To<Type>());

				var isTuple = types.Count > 1;

				while (reader.NextLine())
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

			((InMemoryNativeIdStorage)_inMemory).Add(name, pairs);
		});
        }
}

/// <summary>
/// In memory security native identifier storage.
/// </summary>
public class InMemoryNativeIdStorage : INativeIdStorage
{
	private readonly Dictionary<string, PairSet<SecurityId, object>> _nativeIds = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly SyncObject _syncRoot = new();

	private Action<string, SecurityId, object> _added;

	event Action<string, SecurityId, object> INativeIdStorage.Added
	{
		add => _added += value;
		remove => _added -= value;
	}

	IDictionary<string, Exception> INativeIdStorage.Init()
	{
		return new Dictionary<string, Exception>();
	}

	internal void Add(string storageName, IEnumerable<(SecurityId secId, object nativeId)> ids)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		if (ids == null)
			throw new ArgumentNullException(nameof(ids));

		lock (_syncRoot)
		{
			var dict = _nativeIds.SafeAdd(storageName);

			foreach (var (secId, nativeId) in ids)
			{
				// skip duplicates
				if (dict.ContainsKey(secId) || dict.ContainsValue(nativeId))
					continue;

				dict.Add(secId, nativeId);
			}
		}
	}

	bool INativeIdStorage.TryAdd(string storageName, SecurityId securityId, object nativeId, bool isPersistable)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		if (nativeId == null)
			throw new ArgumentNullException(nameof(nativeId));

		lock (_syncRoot)
		{
			var added = _nativeIds.SafeAdd(storageName).TryAdd(securityId, nativeId);

			if (!added)
				return false;
		}

		_added?.Invoke(storageName, securityId, nativeId);

		return true;
	}

	object INativeIdStorage.TryGetBySecurityId(string storageName, SecurityId securityId)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		lock (_syncRoot)
			return _nativeIds.TryGetValue(storageName)?.TryGetValue(securityId);
	}

	void INativeIdStorage.Clear(string storageName)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		lock (_syncRoot)
			_nativeIds.Remove(storageName);
	}

	SecurityId? INativeIdStorage.TryGetByNativeId(string storageName, object nativeId)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		var securityId = default(SecurityId);

		lock (_syncRoot)
		{
			if (_nativeIds.TryGetValue(storageName)?.TryGetKey(nativeId, out securityId) != true)
				return null;
		}

		return securityId;
	}

	(SecurityId, object)[] INativeIdStorage.Get(string storageName)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		lock (_syncRoot)
			return _nativeIds.TryGetValue(storageName)?.Select(p => (p.Key, p.Value)).ToArray() ?? [];
	}

	bool INativeIdStorage.RemoveBySecurityId(string storageName, SecurityId securityId, bool isPersistable)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		lock (_syncRoot)
		{
			var set = _nativeIds.TryGetValue(storageName);

			if (set == null)
				return false;

			return set.Remove(securityId);
		}
	}

	bool INativeIdStorage.RemoveByNativeId(string storageName, object nativeId, bool isPersistable)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		lock (_syncRoot)
		{
			var set = _nativeIds.TryGetValue(storageName);

			if (set == null)
				return false;

			return set.RemoveByValue(nativeId);
		}
	}
}