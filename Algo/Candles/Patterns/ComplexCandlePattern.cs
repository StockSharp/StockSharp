using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Ecng.Serialization;

using StockSharp.Localization;
using StockSharp.Messages;

namespace StockSharp.Algo.Candles.Patterns;

/// <summary>
/// Base complex implementation of <see cref="ICandlePattern"/>.
/// </summary>
public class ComplexCandlePattern : ICandlePattern
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ComplexCandlePattern"/>.
	/// </summary>
	public ComplexCandlePattern() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="ComplexCandlePattern"/>.
	/// </summary>
	public ComplexCandlePattern(IEnumerable<ICandlePattern> inner)
	{
		_inner.AddRange(inner);
		UpdateCount();
	}

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NameKey,
		Description = LocalizedStrings.NameKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public string Name { get; set; }

	private readonly List<ICandlePattern> _inner = new();

	/// <summary>
	/// Inner patterns.
	/// </summary>
	public IEnumerable<ICandlePattern> Inner => _inner;

	/// <inheritdoc />
	public int CandlesCount { get; private set; }

	private void UpdateCount() => CandlesCount = Inner.Sum(c => c.CandlesCount);

	bool ICandlePattern.Recognize(ReadOnlySpan<ICandleMessage> candles)
	{
		if(candles.Length != CandlesCount)
			throw new ArgumentException($"unexpected candles count. expected {CandlesCount}, got {candles.Length}");

		var start = 0;
		foreach (var inner in _inner)
		{
			var subCandles = candles.Slice(start, inner.CandlesCount);

			if(!inner.Recognize(subCandles))
				return false;

			start += inner.CandlesCount;
		}

		return true;
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Name), Name)
			.Set(nameof(Inner), Inner.Select(i => i.SaveEntire(false)).ToArray())
		;
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		Name = storage.GetValue<string>(nameof(Name));

		_inner.Clear();
		_inner.AddRange(storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Inner)).Select(i => i.LoadEntire<ICandlePattern>()));
		UpdateCount();

		if(!(_inner?.Count > 0))
			throw new ArgumentException("no inner patterns");
	}

	/// <inheritdoc />
	public override string ToString() => Name;
}
