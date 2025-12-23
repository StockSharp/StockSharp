namespace StockSharp.Algo.Storages;

/// <summary>
/// Security native identifier storage.
/// </summary>
public interface INativeIdStorage
{
	/// <summary>
	/// The new native security identifier added to storage.
	/// </summary>
	event Func<string, SecurityId, object, CancellationToken, ValueTask> Added;

	/// <summary>
	/// Initialize the storage.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
	ValueTask<Dictionary<string, Exception>> InitAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Get native security identifiers for storage.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Security identifiers.</returns>
	ValueTask<(SecurityId secId, object nativeId)[]> GetAsync(string storageName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Try add native security identifier to storage.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="nativeId">Native (internal) trading system security id.</param>
	/// <param name="isPersistable">Save the identifier as a permanent.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see langword="true"/> if native identifier was added. Otherwise, <see langword="false" />.</returns>
	ValueTask<bool> TryAddAsync(string storageName, SecurityId securityId, object nativeId, bool isPersistable = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Try get security identifier by native identifier.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="nativeId">Native (internal) trading system security id.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Security identifier.</returns>
	ValueTask<SecurityId?> TryGetByNativeIdAsync(string storageName, object nativeId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Try get native security identifier by identifier.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Native (internal) trading system security id.</returns>
	ValueTask<object> TryGetBySecurityIdAsync(string storageName, SecurityId securityId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Clear storage.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	ValueTask ClearAsync(string storageName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Remove by security identifier.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="isPersistable">Save the identifier as a permanent.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Operation result.</returns>
	ValueTask<bool> RemoveBySecurityIdAsync(string storageName, SecurityId securityId, bool isPersistable = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Remove by native identifier.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="nativeId">Native (internal) trading system security id.</param>
	/// <param name="isPersistable">Save the identifier as a permanent.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Operation result.</returns>
	ValueTask<bool> RemoveByNativeIdAsync(string storageName, object nativeId, bool isPersistable = true, CancellationToken cancellationToken = default);
}

/// <summary>
/// CSV security native identifier storage.
/// </summary>
public sealed class CsvNativeIdStorage : INativeIdStorage
{
	private readonly INativeIdStorage _inMemory = new InMemoryNativeIdStorage();
	private readonly SynchronizedDictionary<SecurityId, object> _buffer = [];

	private readonly string _path;
	private readonly ChannelExecutor _executor;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvNativeIdStorage"/>.
	/// </summary>
	/// <param name="path">Path to storage.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	public CsvNativeIdStorage(string path, ChannelExecutor executor)
	{
		if (path == null)
			throw new ArgumentNullException(nameof(path));

		_path = path.ToFullPath();
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));
	}

	/// <inheritdoc />
	public event Func<string, SecurityId, object, CancellationToken, ValueTask> Added;

	/// <inheritdoc />
	public async ValueTask<Dictionary<string, Exception>> InitAsync(CancellationToken cancellationToken)
	{
		Directory.CreateDirectory(_path);

		var errors = await _inMemory.InitAsync(cancellationToken);

		var files = Directory.GetFiles(_path, "*.csv");

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
	public ValueTask<(SecurityId, object)[]> GetAsync(string storageName, CancellationToken cancellationToken)
		=> _inMemory.GetAsync(storageName, cancellationToken);

	/// <inheritdoc />
	public async ValueTask<bool> TryAddAsync(string storageName, SecurityId securityId, object nativeId, bool isPersistable, CancellationToken cancellationToken)
	{
		var added = await _inMemory.TryAddAsync(storageName, securityId, nativeId, isPersistable, cancellationToken);

		if (!added)
			return false;

		if (isPersistable)
			Save(storageName, securityId, nativeId);

		var evt = Added;
		if (evt != null)
			await evt.Invoke(storageName, securityId, nativeId, cancellationToken);

		return true;
	}

	/// <inheritdoc />
	public async ValueTask ClearAsync(string storageName, CancellationToken cancellationToken)
	{
		await _inMemory.ClearAsync(storageName, cancellationToken);
		_buffer.Clear();
		_executor.Add(() => File.Delete(GetFileName(storageName)));
	}

	/// <inheritdoc />
	public async ValueTask<bool> RemoveBySecurityIdAsync(string storageName, SecurityId securityId, bool isPersistable, CancellationToken cancellationToken)
	{
		var removed = await _inMemory.RemoveBySecurityIdAsync(storageName, securityId, isPersistable, cancellationToken);

		if (!removed)
			return false;

		if (isPersistable)
			await SaveAllAsync(storageName, cancellationToken);

		return true;
	}

	/// <inheritdoc />
	public async ValueTask<bool> RemoveByNativeIdAsync(string storageName, object nativeId, bool isPersistable, CancellationToken cancellationToken)
	{
		var removed = await _inMemory.RemoveByNativeIdAsync(storageName, nativeId, isPersistable, cancellationToken);

		if (!removed)
			return false;

		if (isPersistable)
			await SaveAllAsync(storageName, cancellationToken);

		return true;
	}

	private async ValueTask SaveAllAsync(string storageName, CancellationToken cancellationToken)
	{
		_buffer.Clear();

		_executor.Add(async () =>
		{
			var fileName = GetFileName(storageName);

			File.Delete(fileName);

			var items = await _inMemory.GetAsync(storageName, cancellationToken);

			if (items.Length == 0)
				return;

			using var writer = new TransactionFileStream(fileName, FileMode.Append).CreateCsvWriter();

			WriteHeader(writer, items.FirstOrDefault().nativeId);

			foreach (var (secId, nativeId) in items)
				WriteItem(writer, secId, nativeId);
		});
	}

	/// <inheritdoc />
	public ValueTask<SecurityId?> TryGetByNativeIdAsync(string storageName, object nativeId, CancellationToken cancellationToken)
		=> _inMemory.TryGetByNativeIdAsync(storageName, nativeId, cancellationToken);

	/// <inheritdoc />
	public ValueTask<object> TryGetBySecurityIdAsync(string storageName, SecurityId securityId, CancellationToken cancellationToken)
		=> _inMemory.TryGetBySecurityIdAsync(storageName, securityId, cancellationToken);

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

		_executor.Add(() =>
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

	private async ValueTask LoadFileAsync(string fileName, CancellationToken cancellationToken)
	{
		await Do.InvariantAsync(async () =>
		{
			if (!File.Exists(fileName))
				return;

			var name = Path.GetFileNameWithoutExtension(fileName);

			var pairs = new List<(SecurityId, object)>();

			using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				var reader = stream.CreateCsvReader(Encoding.UTF8);

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
	private readonly Lock _syncRoot = new();

	private Func<string, SecurityId, object, CancellationToken, ValueTask> _added;

	event Func<string, SecurityId, object, CancellationToken, ValueTask> INativeIdStorage.Added
	{
		add => _added += value;
		remove => _added -= value;
	}

	ValueTask<Dictionary<string, Exception>> INativeIdStorage.InitAsync(CancellationToken cancellationToken)
	{
		return new([]);
	}

	internal void Add(string storageName, IEnumerable<(SecurityId secId, object nativeId)> ids)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		if (ids == null)
			throw new ArgumentNullException(nameof(ids));

		using (_syncRoot.EnterScope())
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

	async ValueTask<bool> INativeIdStorage.TryAddAsync(string storageName, SecurityId securityId, object nativeId, bool isPersistable, CancellationToken cancellationToken)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		if (nativeId == null)
			throw new ArgumentNullException(nameof(nativeId));

		using (_syncRoot.EnterScope())
		{
			var added = _nativeIds.SafeAdd(storageName).TryAdd(securityId, nativeId);

			if (!added)
				return false;
		}

		var evt = _added;
		if (evt != null)
			await evt.Invoke(storageName, securityId, nativeId, cancellationToken);

		return true;
	}

	ValueTask<object> INativeIdStorage.TryGetBySecurityIdAsync(string storageName, SecurityId securityId, CancellationToken cancellationToken)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		using (_syncRoot.EnterScope())
			return new(_nativeIds.TryGetValue(storageName)?.TryGetValue(securityId));
	}

	ValueTask INativeIdStorage.ClearAsync(string storageName, CancellationToken cancellationToken)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		using (_syncRoot.EnterScope())
			_nativeIds.Remove(storageName);

		return default;
	}

	ValueTask<SecurityId?> INativeIdStorage.TryGetByNativeIdAsync(string storageName, object nativeId, CancellationToken cancellationToken)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		var securityId = default(SecurityId);

		using (_syncRoot.EnterScope())
		{
			if (_nativeIds.TryGetValue(storageName)?.TryGetKey(nativeId, out securityId) != true)
				return new((SecurityId?)null);
		}

		return new(securityId);
	}

	ValueTask<(SecurityId, object)[]> INativeIdStorage.GetAsync(string storageName, CancellationToken cancellationToken)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		using (_syncRoot.EnterScope())
			return new(_nativeIds.TryGetValue(storageName)?.Select(p => (p.Key, p.Value)).ToArray() ?? []);
	}

	ValueTask<bool> INativeIdStorage.RemoveBySecurityIdAsync(string storageName, SecurityId securityId, bool isPersistable, CancellationToken cancellationToken)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		using (_syncRoot.EnterScope())
		{
			var set = _nativeIds.TryGetValue(storageName);

			if (set == null)
				return new(false);

			return new(set.Remove(securityId));
		}
	}

	ValueTask<bool> INativeIdStorage.RemoveByNativeIdAsync(string storageName, object nativeId, bool isPersistable, CancellationToken cancellationToken)
	{
		if (storageName.IsEmpty())
			throw new ArgumentNullException(nameof(storageName));

		using (_syncRoot.EnterScope())
		{
			var set = _nativeIds.TryGetValue(storageName);

			if (set == null)
				return new(false);

			return new(set.RemoveByValue(nativeId));
		}
	}
}