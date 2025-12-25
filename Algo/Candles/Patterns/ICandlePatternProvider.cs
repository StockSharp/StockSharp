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
	/// Save pattern to the storage.
	/// </summary>
	/// <param name="pattern">Pattern.</param>
	void Save(ICandlePattern pattern);
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
		CandlePatternRegistry.All.ForEach(p => ((ICandlePatternProvider)this).Save(p));
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

	void ICandlePatternProvider.Save(ICandlePattern pattern)
	{
		if (pattern is null)
			throw new ArgumentNullException(nameof(pattern));

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
/// <param name="fileName">File name.</param>
/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
public class CandlePatternFileStorage(string fileName, ChannelExecutor executor) : ICandlePatternProvider
{
	private readonly ICandlePatternProvider _inMemory = new InMemoryCandlePatternProvider();
	private readonly CachedSynchronizedDictionary<string, ICandlePattern> _cache = [];
	private readonly string _fileName = fileName.ThrowIfEmpty(nameof(fileName));
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

		if (File.Exists(_fileName))
		{
			await Do.InvariantAsync(async () => (await _fileName.DeserializeAsync<SettingsStorage[]>(cancellationToken))?.Select(s =>
			{
				try
				{
					return s.LoadEntire<ICandlePattern>();
				}
				catch (Exception ex)
				{
					errors.Add(ex);
					return null;
				}
			}).WhereNotNull().ForEach(Save));
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
	public void Save(ICandlePattern pattern)
	{
		if (pattern is null)
			throw new ArgumentNullException(nameof(pattern));

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
		_executor.Add(() =>
			_cache
				.CachedValues
				.Select(i => i.SaveEntire(false))
				.Serialize(_fileName));
	}

	bool ICandlePatternProvider.TryFind(string name, out ICandlePattern pattern)
		=> _cache.TryGetValue(name, out pattern) || _inMemory.TryFind(name, out pattern);
}
