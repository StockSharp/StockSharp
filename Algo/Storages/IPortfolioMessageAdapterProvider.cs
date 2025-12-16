namespace StockSharp.Algo.Storages;

/// <summary>
/// The portfolio based message adapter's provider interface.
/// </summary>
public interface IPortfolioMessageAdapterProvider : IMappingMessageAdapterProvider<string>
{
}

/// <summary>
/// In memory implementation of <see cref="IPortfolioMessageAdapterProvider"/>.
/// </summary>
public class InMemoryPortfolioMessageAdapterProvider : IPortfolioMessageAdapterProvider
{
	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryPortfolioMessageAdapterProvider"/>.
	/// </summary>
	public InMemoryPortfolioMessageAdapterProvider()
	{
	}

	private readonly CachedSynchronizedDictionary<string, Guid> _adapters = new(StringComparer.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	public IEnumerable<KeyValuePair<string, Guid>> Adapters => _adapters.CachedPairs;

	/// <inheritdoc />
	public virtual ValueTask InitAsync(CancellationToken cancellationToken)
	{
		return default;
	}

	/// <inheritdoc />
	public event Action<string, Guid, bool> Changed;

	/// <inheritdoc />
	public Guid? TryGetAdapter(string key)
	{
		if (key.IsEmpty())
			throw new ArgumentNullException(nameof(key));

		return _adapters.TryGetValue2(key);
	}

	/// <inheritdoc />
	public bool SetAdapter(string key, Guid adapterId)
	{
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
	public bool RemoveAssociation(string key)
	{
		if (key.IsEmpty())
			throw new ArgumentNullException(nameof(key));

		if (!_adapters.Remove(key))
			return false;

		Changed?.Invoke(key, Guid.Empty, false);
		return true;
	}
}

/// <summary>
/// CSV implementation of <see cref="IPortfolioMessageAdapterProvider"/>.
/// </summary>
public class CsvPortfolioMessageAdapterProvider : IPortfolioMessageAdapterProvider
{
	private readonly InMemoryPortfolioMessageAdapterProvider _inMemory = new();

	private readonly string _fileName;
	private readonly ChannelExecutor _executor;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvPortfolioMessageAdapterProvider"/>.
	/// </summary>
	/// <param name="fileName">File name.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	public CsvPortfolioMessageAdapterProvider(string fileName, ChannelExecutor executor)
	{
		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		_fileName = fileName;
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));

		_inMemory.Changed += InMemoryOnChanged;
	}

	private void InMemoryOnChanged(string key, Guid adapterId, bool changeType)
	{
		Changed?.Invoke(key, adapterId, changeType);
	}

	/// <inheritdoc />
	public IEnumerable<KeyValuePair<string, Guid>> Adapters => _inMemory.Adapters;

	/// <inheritdoc />
	public async ValueTask InitAsync(CancellationToken cancellationToken)
	{
		await _inMemory.InitAsync(cancellationToken);

		if (File.Exists(_fileName))
			await LoadAsync(cancellationToken);
	}

	/// <inheritdoc />
	public event Action<string, Guid, bool> Changed;

	/// <inheritdoc />
	public Guid? TryGetAdapter(string key) => _inMemory.TryGetAdapter(key);

	/// <inheritdoc />
	public bool SetAdapter(string key, Guid adapterId)
	{
		var has = _inMemory.TryGetAdapter(key) != null;

		if (!_inMemory.SetAdapter(key, adapterId))
			return false;

		Save(has, has ? _inMemory.Adapters : [new KeyValuePair<string, Guid>(key, adapterId)]);
		return true;
	}

	/// <inheritdoc />
	public bool RemoveAssociation(string key)
	{
		if (!_inMemory.RemoveAssociation(key))
			return false;

		Save(true, _inMemory.Adapters);
		return true;
	}

	private async ValueTask LoadAsync(CancellationToken cancellationToken)
	{
		using var stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);

		var reader = stream.CreateCsvReader(Encoding.UTF8);

		await reader.NextLineAsync(cancellationToken);

		while (await reader.NextLineAsync(cancellationToken))
		{
			var portfolioName = reader.ReadString();
			var adapterId = reader.ReadString().To<Guid>();

			_inMemory.SetAdapter(portfolioName, adapterId);
		}
	}

	private void Save(bool overwrite, IEnumerable<KeyValuePair<string, Guid>> adapters)
	{
		_executor.Add(() =>
		{
			var appendHeader = overwrite || !File.Exists(_fileName) || new FileInfo(_fileName).Length == 0;
			var mode = overwrite ? FileMode.Create : FileMode.Append;

			using var writer = new TransactionFileStream(_fileName, mode).CreateCsvWriter();

			if (appendHeader)
			{
				writer.WriteRow(
				[
					"Portfolio",
					"Adapter",
				]);
			}

			foreach (var pair in adapters)
			{
				writer.WriteRow(
				[
					pair.Key,
					pair.Value.To<string>(),
				]);
			}
		});
	}
}