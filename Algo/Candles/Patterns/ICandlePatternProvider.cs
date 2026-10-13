namespace StockSharp.Algo.Candles.Patterns;

/// <summary>
/// Provider <see cref="ICandlePattern"/>.
/// </summary>
public interface ICandlePatternProvider
{
	/// <summary>
	/// <see cref="ICandlePattern"/> created event.
	/// </summary>
	event Action<ICandlePattern> PatternCreated;

	/// <summary>
	/// <see cref="ICandlePattern"/> replaced event.
	/// </summary>
	event Action<ICandlePattern, ICandlePattern> PatternReplaced;

	/// <summary>
	/// <see cref="ICandlePattern"/> deleted event.
	/// </summary>
	event Action<ICandlePattern> PatternDeleted;

	/// <summary>
	/// Initialize the storage.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	ValueTask InitAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Patterns.
	/// </summary>
	IEnumerable<ICandlePattern> Patterns { get; }

	/// <summary>
	/// Find pattern by name.
	/// </summary>
	/// <param name="name">Name.</param>
	/// <param name="pattern"><see cref="ICandlePattern"/> or <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if pattern was loaded otherwise <see langword="false"/>.</returns>
	bool TryFind(string name, out ICandlePattern pattern);

	/// <summary>
	/// Remove pattern from the storage.
	/// </summary>
	/// <param name="pattern">Pattern.</param>
	/// <returns>Operation result.</returns>
	bool Remove(ICandlePattern pattern);

	/// <summary>
	/// Save pattern to the storage. The pattern is prepared first (<see cref="ICandlePattern.PrepareAsync"/>):
	/// the indicators that use a pattern of the same name switch to the saved one at once.
	/// </summary>
	/// <param name="pattern">Pattern.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask SaveAsync(ICandlePattern pattern, CancellationToken cancellationToken);
}

/// <summary>
/// In memory <see cref="ICandlePattern"/> provider.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryCandlePatternProvider"/>.
/// </remarks>
public class InMemoryCandlePatternProvider : ICandlePatternProvider
{
	private readonly CachedSynchronizedDictionary<string, ICandlePattern> _cache = [];

	/// <inheritdoc/>
	public event Action<ICandlePattern> PatternCreated;

	/// <inheritdoc/>
	public event Action<ICandlePattern, ICandlePattern> PatternReplaced;

	/// <inheritdoc/>
	public event Action<ICandlePattern> PatternDeleted;

	ValueTask ICandlePatternProvider.InitAsync(CancellationToken cancellationToken)
	{
		// the built-in patterns are prepared by whoever uses them
		CandlePatternRegistry.All.ForEach(Store);
		return default;
	}

	IEnumerable<ICandlePattern> ICandlePatternProvider.Patterns => _cache.CachedValues;

	bool ICandlePatternProvider.Remove(ICandlePattern pattern)
	{
		if (pattern is null)
			throw new ArgumentNullException(nameof(pattern));

		if (!_cache.Remove(pattern.Name))
			return false;

		PatternDeleted?.Invoke(pattern);
		return true;
	}

	async ValueTask ICandlePatternProvider.SaveAsync(ICandlePattern pattern, CancellationToken cancellationToken)
	{
		if (pattern is null)
			throw new ArgumentNullException(nameof(pattern));

		await pattern.PrepareAsync(cancellationToken);

		Store(pattern);
	}

	private void Store(ICandlePattern pattern)
	{
		ICandlePattern oldPattern = null;

		_cache.SyncDo(_ =>
		{
			oldPattern = _cache.TryGetValue(pattern.Name);
			_cache[pattern.Name] = pattern;
		});

		if(oldPattern == null)
			PatternCreated?.Invoke(pattern);
		else
			PatternReplaced?.Invoke(oldPattern, pattern);
	}

	bool ICandlePatternProvider.TryFind(string name, out ICandlePattern pattern)
		=> _cache.TryGetValue(name, out pattern);
}

/// <summary>
/// CSV <see cref="ICandlePattern"/> storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CandlePatternFileStorage"/>.
/// </remarks>
/// <param name="fileSystem"><see cref="IFileSystem"/></param>
/// <param name="fileName">File name.</param>
/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
public class CandlePatternFileStorage(IFileSystem fileSystem, string fileName, ChannelExecutor executor) : ICandlePatternProvider
{
	private readonly ICandlePatternProvider _inMemory = new InMemoryCandlePatternProvider();
	private readonly CachedSynchronizedDictionary<string, ICandlePattern> _cache = [];
	private readonly string _fileName = fileName.ThrowIfEmpty(nameof(fileName));
	private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
	private readonly ChannelExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));

	/// <inheritdoc/>
	public event Action<ICandlePattern> PatternCreated;

	/// <inheritdoc/>
	public event Action<ICandlePattern, ICandlePattern> PatternReplaced;

	/// <inheritdoc/>
	public event Action<ICandlePattern> PatternDeleted;

	async ValueTask ICandlePatternProvider.InitAsync(CancellationToken cancellationToken)
	{
		await _inMemory.InitAsync(cancellationToken);

		var errors = new List<Exception>();

		if (_fileSystem.FileExists(_fileName))
		{
			await Do.InvariantAsync(async () =>
			{
				foreach (var s in await _fileName.DeserializeAsync<SettingsStorage[]>(_fileSystem, cancellationToken) ?? [])
				{
					ICandlePattern pattern;

					try
					{
						pattern = await s.LoadEntireAsync<ICandlePattern>(cancellationToken);
					}
					catch (Exception ex)
					{
						errors.Add(ex);
						continue;
					}

					Store(pattern);
				}
			});
		}

		if (errors.Count > 0)
			throw errors.SingleOrAggr();
	}

	IEnumerable<ICandlePattern> ICandlePatternProvider.Patterns
		=> _cache.CachedValues.Concat(_inMemory.Patterns.Where(p => !_cache.ContainsKey(p.Name)));

	bool ICandlePatternProvider.Remove(ICandlePattern pattern)
	{
		if (pattern is null)
			throw new ArgumentNullException(nameof(pattern));

		if (!_cache.Remove(pattern.Name))
			return false;

		PatternDeleted?.Invoke(pattern);
		Save();

		return true;
	}

	/// <inheritdoc />
	public async ValueTask SaveAsync(ICandlePattern pattern, CancellationToken cancellationToken)
	{
		if (pattern is null)
			throw new ArgumentNullException(nameof(pattern));

		await pattern.PrepareAsync(cancellationToken);

		Store(pattern);
	}

	private void Store(ICandlePattern pattern)
	{
		ICandlePattern oldPattern = null;

		_cache.SyncDo(_ =>
		{
			oldPattern = _cache.TryGetValue(pattern.Name);
			_cache[pattern.Name] = pattern;
		});

		if (oldPattern == null)
			PatternCreated?.Invoke(pattern);
		else
			PatternReplaced?.Invoke(oldPattern, pattern);

		Save();
	}

	private void Save()
	{
		_executor.Add(async cancellationToken =>
		{
			var patterns = new List<SettingsStorage>();

			foreach (var pattern in _cache.CachedValues)
				patterns.Add(await pattern.SaveEntireAsync(false, cancellationToken));

			await patterns.ToArray().SerializeAsync(_fileSystem, _fileName, true, cancellationToken);
		});
	}

	bool ICandlePatternProvider.TryFind(string name, out ICandlePattern pattern)
		=> _cache.TryGetValue(name, out pattern) || _inMemory.TryFind(name, out pattern);
}
