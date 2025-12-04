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
	public void Init()
	{
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
	private ChannelExecutorGroup<CsvFileWriter> _writerGroup;

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

		// Create a writer group with reusable stream/writer
		_writerGroup = new ChannelExecutorGroup<CsvFileWriter>(_executor, () =>
		{
			var stream = new TransactionFileStream(_fileName, FileMode.Append);
			return stream.CreateCsvWriter();
		});

		_inMemory.Changed += InMemoryOnChanged;
	}

	private void InMemoryOnChanged(string key, Guid adapterId, bool changeType)
	{
		Changed?.Invoke(key, adapterId, changeType);
	}

	/// <inheritdoc />
	public IEnumerable<KeyValuePair<string, Guid>> Adapters => _inMemory.Adapters;

	/// <inheritdoc />
	public void Init()
	{
		_inMemory.Init();

		if (File.Exists(_fileName))
			Load();
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

	private void Load()
	{
		using var stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);

		var reader = stream.CreateCsvReader(Encoding.UTF8);

		reader.NextLine();

		while (reader.NextLine())
		{
			var portfolioName = reader.ReadString();
			var adapterId = reader.ReadString().To<Guid>();

			_inMemory.SetAdapter(portfolioName, adapterId);
		}
	}

	private void Save(bool overwrite, IEnumerable<KeyValuePair<string, Guid>> adapters)
	{
		var adaptersCopy = adapters.ToArray();

		if (overwrite)
		{
			// For overwrite mode, recreate the group to switch to Create mode
			_writerGroup.RecreateResource();

			_executor.Add(() =>
			{
				using var writer = new TransactionFileStream(_fileName, FileMode.Create).CreateCsvWriter();

				writer.WriteRow(["Portfolio", "Adapter"]);

				foreach (var pair in adaptersCopy)
				{
					writer.WriteRow([pair.Key, pair.Value.To<string>()]);
				}
			});

			// Recreate the group back to Append mode
			_writerGroup.RecreateResource();
		}
		else
		{
			// Use the reusable writer group for append operations
			var needHeader = !File.Exists(_fileName) || new FileInfo(_fileName).Length == 0;

			_writerGroup.Add((writer, state) =>
			{
				if (state.needHeader)
					writer.WriteRow(["Portfolio", "Adapter"]);

				foreach (var pair in state.adapters)
				{
					writer.WriteRow([pair.Key, pair.Value.To<string>()]);
				}
			}, (needHeader, adapters: adaptersCopy));
		}
	}
}