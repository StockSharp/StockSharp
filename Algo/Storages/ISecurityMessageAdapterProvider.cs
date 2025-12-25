namespace StockSharp.Algo.Storages;

using Key = ValueTuple<SecurityId, DataType>;

/// <summary>
/// The security based message adapter's provider interface.
/// </summary>
public interface ISecurityMessageAdapterProvider : IMappingMessageAdapterProvider<(SecurityId secId, DataType dt)>
{
	/// <summary>
	/// Get adapter by the specified security id.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type.</param>
	/// <returns>Found adapter identifier or <see langword="null"/>.</returns>
	Guid? TryGetAdapter(SecurityId securityId, DataType dataType);

	/// <summary>
	/// Make association with adapter.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type.</param>
	/// <param name="adapterId">Adapter identifier.</param>
	/// <returns><see langword="true"/> if the association is successfully changed, otherwise, <see langword="false"/>.</returns>
	bool SetAdapter(SecurityId securityId, DataType dataType, Guid adapterId);
}

/// <summary>
/// In memory implementation of <see cref="ISecurityMessageAdapterProvider"/>.
/// </summary>
public class InMemorySecurityMessageAdapterProvider : ISecurityMessageAdapterProvider
{
	/// <summary>
	/// Initializes a new instance of the <see cref="InMemorySecurityMessageAdapterProvider"/>.
	/// </summary>
	public InMemorySecurityMessageAdapterProvider()
	{
	}

	private readonly CachedSynchronizedDictionary<Key, Guid> _adapters = [];

	/// <inheritdoc />
	public IEnumerable<KeyValuePair<Key, Guid>> Adapters => _adapters.CachedPairs;

	/// <inheritdoc />
	public ValueTask InitAsync(CancellationToken cancellationToken)
	{
		return default;
	}

	/// <inheritdoc />
	public event Action<Key, Guid, bool> Changed;

	/// <inheritdoc />
	public Guid? TryGetAdapter(Key key)
	{
		return _adapters.TryGetValue2(key);
	}

	/// <inheritdoc />
	public bool SetAdapter(Key key, Guid adapterId)
	{
		if (key == default)
			throw new ArgumentNullException(nameof(key));

		if (adapterId == default)
			throw new ArgumentNullException(nameof(adapterId));

		using (_adapters.EnterScope())
		{
			var prev = TryGetAdapter(key);

			if (prev == adapterId)
				return false;

			_adapters[key] = adapterId;
		}

		Changed?.Invoke(key, adapterId, true);
		return true;
	}

	/// <inheritdoc />
	public bool RemoveAssociation(Key key)
	{
		if (!_adapters.Remove(key))
			return false;

		Changed?.Invoke(key, Guid.Empty, false);
		return true;
	}

	/// <inheritdoc />
	public Guid? TryGetAdapter(SecurityId securityId, DataType dataType)
	{
		return TryGetAdapter((securityId, dataType));
	}

	/// <inheritdoc />
	public bool SetAdapter(SecurityId securityId, DataType dataType, Guid adapterId)
	{
		return SetAdapter((securityId, dataType), adapterId);
	}
}

/// <summary>
/// CSV implementation of <see cref="ISecurityMessageAdapterProvider"/>.
/// </summary>
public class CsvSecurityMessageAdapterProvider : ISecurityMessageAdapterProvider
{
	private readonly InMemorySecurityMessageAdapterProvider _inMemory = new();

	private readonly string _fileName;
	private readonly ChannelExecutor _executor;
	private readonly IFileSystem _fileSystem;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvSecurityMessageAdapterProvider"/>.
	/// </summary>
	/// <param name="fileName">File name.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	[Obsolete("Use IFileSystem overload.")]
	public CsvSecurityMessageAdapterProvider(string fileName, ChannelExecutor executor)
		: this(Paths.FileSystem, fileName, executor)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvSecurityMessageAdapterProvider"/>.
	/// </summary>
	/// <param name="fileSystem"><see cref="IFileSystem"/></param>
	/// <param name="fileName">File name.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	public CsvSecurityMessageAdapterProvider(IFileSystem fileSystem, string fileName, ChannelExecutor executor)
	{
		_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		_fileName = fileName;
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));

		_inMemory.Changed += InMemoryOnChanged;
	}

	private void InMemoryOnChanged(Key key, Guid adapterId, bool changeType)
	{
		Changed?.Invoke(key, adapterId, changeType);
	}

	/// <inheritdoc />
	public IEnumerable<KeyValuePair<Key, Guid>> Adapters => _inMemory.Adapters;

	/// <inheritdoc />
	public async ValueTask InitAsync(CancellationToken cancellationToken)
	{
		await _inMemory.InitAsync(cancellationToken);

		if (_fileSystem.FileExists(_fileName))
			await LoadAsync(cancellationToken);
	}

	/// <inheritdoc />
	public event Action<Key, Guid, bool> Changed;

	/// <inheritdoc />
	public Guid? TryGetAdapter(Key key) => _inMemory.TryGetAdapter(key);

	/// <inheritdoc />
	public bool SetAdapter(Key key, Guid adapterId) => SetAdapter(key.Item1, key.Item2, adapterId);

	/// <inheritdoc />
	public bool RemoveAssociation(Key key)
	{
		if (!_inMemory.RemoveAssociation(key))
			return false;

		Save(true, _inMemory.Adapters);
		return true;
	}

	/// <inheritdoc />
	public Guid? TryGetAdapter(SecurityId securityId, DataType dataType)
		=> _inMemory.TryGetAdapter(securityId, dataType);

	/// <inheritdoc />
	public bool SetAdapter(SecurityId securityId, DataType dataType, Guid adapterId)
	{
		var has = _inMemory.TryGetAdapter(securityId, dataType) != null;

		if (!_inMemory.SetAdapter(securityId, dataType, adapterId))
			return false;

		Save(has, has ? _inMemory.Adapters : [new KeyValuePair<Key, Guid>((securityId, dataType), adapterId)]);
		return true;
	}

	private async ValueTask LoadAsync(CancellationToken cancellationToken)
	{
		using var stream = _fileSystem.OpenRead(_fileName);

		var reader = stream.CreateCsvReader(Encoding.UTF8);

		await reader.NextLineAsync(cancellationToken);

		while (await reader.NextLineAsync(cancellationToken))
		{
			var securityId = new SecurityId
			{
				SecurityCode = reader.ReadString(),
				BoardCode = reader.ReadString()
			};

			DataType dataType;

			var typeStr = reader.ReadString();

			var argStr = reader.ReadString();
			dataType = typeStr.IsEmpty() ? null : typeStr.ToDataType(argStr);

			var adapterId = reader.ReadString().To<Guid>();

			_inMemory.SetAdapter(securityId, dataType, adapterId);
		}
	}

	private void Save(bool overwrite, IEnumerable<KeyValuePair<Key, Guid>> adapters)
	{
		_executor.Add(() =>
		{
			var appendHeader = overwrite || !_fileSystem.FileExists(_fileName) || _fileSystem.GetFileLength(_fileName) == 0;
			var mode = overwrite ? FileMode.Create : FileMode.Append;

			using var stream = new TransactionFileStream(_fileSystem, _fileName, mode);
			using var writer = stream.CreateCsvWriter();

			if (appendHeader)
			{
				writer.WriteRow(
				[
					"Symbol",
					"Board",
					"MessageType",
					"Arg",
					"Adapter",
				]);
			}

			foreach (var pair in adapters)
			{
				var dataType = pair.Key.Item2?.FormatToString();

				writer.WriteRow(
				[
					pair.Key.Item1.SecurityCode,
					pair.Key.Item1.BoardCode,
					dataType?.type,
					dataType?.arg,
					pair.Value.To<string>()
				]);
			}

			writer.Flush();
			stream.Commit();
		});
	}
}