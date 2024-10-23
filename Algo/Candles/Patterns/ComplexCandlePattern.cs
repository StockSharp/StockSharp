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
	public ComplexCandlePattern(string name, IEnumerable<ICandlePattern> inner)
	{
		Name = name;

		_inner.AddRange(inner);
		UpdateCount();
	}

	/// <inheritdoc />
	public string Name { get; private set; }

	private readonly List<ICandlePattern> _inner = [];

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

	private void EnsureEmpty()
	{
		if(Name != null || _inner.Count > 0)
			throw new InvalidOperationException($"cannot change initialized pattern (name='{Name}', {_inner.Count} inner patterns)");
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
		EnsureEmpty();

		Name = storage.GetValue<string>(nameof(Name));

		_inner.Clear();
		_inner.AddRange(storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Inner)).Select(i => i.LoadEntire<ICandlePattern>()));
		UpdateCount();
	}

	/// <inheritdoc />
	public override string ToString() => Name;
}
