namespace StockSharp.Algo;

/// <summary>
/// The index, built of instruments. For example, to specify spread at arbitrage or pair trading.
/// </summary>
public abstract class IndexSecurity : BasketSecurity
{
	/// <summary>
	/// Ignore calculation errors (like arithmetic overflows).
	/// </summary>
	public bool IgnoreErrors { get; set; }

	/// <summary>
	/// Calculate extended information.
	/// </summary>
	public bool CalculateExtended { get; set; }

	/// <summary>
	/// Fill market-data gaps by zero values.
	/// </summary>
	public bool FillGapsByZeros { get; set; }

	/// <summary>
	/// Initialize <see cref="IndexSecurity"/>.
	/// </summary>
	protected IndexSecurity()
	{
		Type = SecurityTypes.Index;
		//Board = ExchangeBoard.Associated;
	}
}

/// <summary>
/// The instruments basket, based on weigh-scales <see cref="Weights"/>.
/// </summary>
[BasketCode("WI")]
public class WeightedIndexSecurity : IndexSecurity
{
	/// <summary>
	/// Initializes a new instance of the <see cref="WeightedIndexSecurity"/>.
	/// </summary>
	public WeightedIndexSecurity()
	{
		Weights = [];
	}

	/// <summary>
	/// Instruments and their weighting coefficients in the basket.
	/// </summary>
	public CachedSynchronizedDictionary<SecurityId, decimal> Weights { get; }

	/// <inheritdoc />
	public override IEnumerable<SecurityId> InnerSecurityIds => Weights.CachedKeys;

	/// <inheritdoc />
	public override Security Clone()
	{
		var clone = new WeightedIndexSecurity();
		clone.Weights.AddRange(Weights.CachedPairs);
		CopyTo(clone);
		return clone;
	}

	/// <inheritdoc />
	protected override void FromSerializedString(string text)
	{
		lock (Weights.SyncRoot)
		{
			Weights.Clear();
			Weights.AddRange(text.SplitByComma().Select(p =>
			{
				var parts = p.SplitByEqual();
				return new KeyValuePair<SecurityId, decimal>(parts[0].ToSecurityId(), parts[1].To<decimal>());
			}));
		}
	}

	/// <inheritdoc />
	protected override string ToSerializedString()
	{
		return Weights.CachedPairs.Select(p => $"{p.Key.ToStringId()}={p.Value}").JoinComma();
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return Weights.CachedPairs.Select(p => $"{p.Value} * {p.Key.ToStringId()}").JoinCommaSpace();
	}
}