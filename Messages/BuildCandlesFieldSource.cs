namespace StockSharp.Messages;

/// <summary>
/// Source for <see cref="MarketDataMessage.BuildField"/>.
/// </summary>
public class BuildCandlesFieldSource : ItemsSourceBase<Level1Fields>
{
	private static readonly IEnumerable<Level1Fields> _values =
	[
		Level1Fields.BestBidPrice,
		Level1Fields.BestAskPrice,
		Level1Fields.LastTradePrice,
		Level1Fields.SpreadMiddle
	];

	/// <summary>
	/// Get values.
	/// </summary>
	/// <returns>Values.</returns>
	protected override IEnumerable<Level1Fields> GetValues() => _values;
}