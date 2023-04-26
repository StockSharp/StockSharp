namespace StockSharp.Algo.Candles.Patterns;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Configuration;

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
	void Init();

	/// <summary>
	/// Patterns.
	/// </summary>
	IEnumerable<ICandlePattern> Patterns { get; }

	/// <summary>
	/// Find pattern by name.
	/// </summary>
	/// <param name="name">Name.</param>
	/// <returns><see cref="ICandlePattern"/> or <see langword="null"/>.</returns>
	ICandlePattern TryFind(string name);

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
public class InMemoryCandlePatternProvider : ICandlePatternProvider
{
	private readonly CachedSynchronizedDictionary<string, ICandlePattern> _cache = new();
	private readonly ICandlePattern[] _appendOnInit;

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryCandlePatternProvider"/>.
	/// </summary>
	public InMemoryCandlePatternProvider(IEnumerable<ICandlePattern> patterns = null)
		=> _appendOnInit = patterns?.ToArray();

	/// <inheritdoc/>
	public event Action<ICandlePattern> PatternCreated;

	/// <inheritdoc/>
	public event Action<ICandlePattern, ICandlePattern> PatternReplaced;

	/// <inheritdoc/>
	public event Action<ICandlePattern> PatternDeleted;

	void ICandlePatternProvider.Init()
	{
		_cache.CachedValues.ForEach(p => ((ICandlePatternProvider)this).Remove(p));
		_appendOnInit?.ForEach(p => ((ICandlePatternProvider)this).Save(p));
	}

	IEnumerable<ICandlePattern> ICandlePatternProvider.Patterns => _cache.CachedValues;

	bool ICandlePatternProvider.Remove(ICandlePattern pattern)
	{
		if (!_cache.Remove(pattern.Name))
			return false;

		PatternDeleted?.Invoke(pattern);
		return true;
	}

	void ICandlePatternProvider.Save(ICandlePattern pattern)
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

	ICandlePattern ICandlePatternProvider.TryFind(string name)
		=> _cache.TryGetValue(name);
}

/// <summary>
/// CSV <see cref="ICandlePattern"/> storage.
/// </summary>
public class CandlePatternFileStorage : ICandlePatternProvider
{
	private readonly ICandlePatternProvider _inMemory;

	private readonly string _fileName;

	/// <summary>
	/// Initializes a new instance of the <see cref="CandlePatternFileStorage"/>.
	/// </summary>
	/// <param name="fileName">File name.</param>
	/// <param name="patterns">Patterns.</param>
	public CandlePatternFileStorage(string fileName, IEnumerable<ICandlePattern> patterns = null)
    {
		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		_inMemory = new InMemoryCandlePatternProvider(patterns);
		_fileName = fileName;

		_inMemory.PatternCreated  += p        => PatternCreated?.Invoke(p);
		_inMemory.PatternReplaced += (op, np) => PatternReplaced?.Invoke(op, np);
		_inMemory.PatternDeleted  += p        => PatternDeleted?.Invoke(p);
	}

	private DelayAction _delayAction;

	/// <inheritdoc/>
	public event Action<ICandlePattern> PatternCreated;

	/// <inheritdoc/>
	public event Action<ICandlePattern, ICandlePattern> PatternReplaced;

	/// <inheritdoc/>
	public event Action<ICandlePattern> PatternDeleted;

	/// <summary>
	/// The time delayed action.
	/// </summary>
	public DelayAction DelayAction
	{
		get => _delayAction;
		set => _delayAction = value ?? throw new ArgumentNullException(nameof(value));
	}

	void ICandlePatternProvider.Init()
	{
		_inMemory.Init();

		if (File.Exists(_fileName))
			Do.Invariant(() => _fileName.Deserialize<SettingsStorage[]>().Select(s => s.LoadEntire<ICandlePattern>()).ForEach(p =>
			{
				var toReplace = _inMemory.TryFind(p.Name);
				if(toReplace != null)
					_inMemory.Remove(toReplace);

				_inMemory.Save(p);
			}));
	}

	IEnumerable<ICandlePattern> ICandlePatternProvider.Patterns => _inMemory.Patterns;

	bool ICandlePatternProvider.Remove(ICandlePattern pattern)
	{
		if (!_inMemory.Remove(pattern))
			return false;

		Save();
		return true;
	}

	void ICandlePatternProvider.Save(ICandlePattern pattern)
	{
		_inMemory.Save(pattern);
		Save();
	}

	private void Save()
	{
		void dosave()
		{
			var serialized = _inMemory.Patterns.Where(p => (p as ExpressionCandlePattern)?.IsRegistry != true).Select(i => i.SaveEntire(false)).ToArray();

			if(serialized.Length > 0)
				serialized.Serialize(_fileName);
			else if(File.Exists(_fileName))
				File.Delete(_fileName);
		}

		if (DelayAction is null)
			dosave();
		else
			DelayAction.DefaultGroup.Add(dosave);
	}

	ICandlePattern ICandlePatternProvider.TryFind(string name) => _inMemory.TryFind(name);
}
