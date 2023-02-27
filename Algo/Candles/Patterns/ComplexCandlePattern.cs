namespace StockSharp.Algo.Candles.Patterns;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Ecng.Serialization;
using Ecng.Collections;
using Ecng.Common;

using StockSharp.Localization;
using StockSharp.Messages;

/// <summary>
/// Base complex implementation of <see cref="ICandlePattern"/>.
/// </summary>
public class ComplexCandlePattern : ICandlePattern
{
	private int _last;

	/// <summary>
	/// Initializes a new instance of the <see cref="ComplexCandlePattern"/>.
	/// </summary>
	public ComplexCandlePattern()
	{
	}

	private string _name = nameof(ComplexCandlePattern);

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NameKey,
		Description = LocalizedStrings.NameKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public string Name
	{
		get => _name;
		set => _name = value.ThrowIfEmpty(nameof(value));
	}

	/// <summary>
	/// Inner patterns.
	/// </summary>
	public IList<ICandlePattern> Inner { get; } = new List<ICandlePattern>();

	void ICandlePattern.Reset()
	{
		_last = 0;

		foreach (var i in Inner)
			i.Reset();
	}

	bool ICandlePattern.Recognize(ICandleMessage candle)
	{
		var res = Inner[_last].Recognize(candle);

		if (res)
		{
			if (++_last < Inner.Count)
				return false;

			_last = 0;
			return true;
		}

		_last = 0;
		return false;
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

		Inner.Clear();
		Inner.AddRange(storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Inner)).Select(i => i.LoadEntire<ICandlePattern>()));
	}

	/// <inheritdoc />
	public override string ToString() => Name;
}