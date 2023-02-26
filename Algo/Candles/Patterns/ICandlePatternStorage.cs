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
/// Storage <see cref="ICandlePattern"/>.
/// </summary>
public interface ICandlePatternStorage
{
	/// <summary>
	/// Initialize the storage.
	/// </summary>
	void Init();

	/// <summary>
	/// Get patterns from the storage.
	/// </summary>
	/// <returns>Patterns.</returns>
	IEnumerable<ICandlePattern> Load();

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
/// CSV <see cref="ICandlePattern"/> storage.
/// </summary>
public class CsvCandlePatternStorage : ICandlePatternStorage
{
	private readonly CachedSynchronizedSet<ICandlePattern> _cache = new();

	private readonly string _fileName;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvCandlePatternStorage"/>.
	/// </summary>
	/// <param name="fileName">CSV file.</param>
	public CsvCandlePatternStorage(string fileName)
    {
		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		_fileName = fileName;
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

	void ICandlePatternStorage.Init()
	{
		if (!File.Exists(_fileName))
			return;

		Do.Invariant(() => _cache.AddRange(Paths.Deserialize<SettingsStorage[]>(_fileName).Select(s => s.LoadEntire<ICandlePattern>())));
	}

	IEnumerable<ICandlePattern> ICandlePatternStorage.Load()
		=> _cache.Cache;

	bool ICandlePatternStorage.Remove(ICandlePattern pattern)
	{
		if (!_cache.Remove(pattern))
			return false;

		Save();
		return true;
	}

	void ICandlePatternStorage.Save(ICandlePattern pattern)
	{
		_cache.Add(pattern);

		Save();
	}

	private void Save()
	{
		DelayAction.DefaultGroup.Add(() => Paths.Serialize<SettingsStorage[]>(_cache.Cache.Select(i => i.SaveEntire(false)).ToArray(), _fileName));
	}
}