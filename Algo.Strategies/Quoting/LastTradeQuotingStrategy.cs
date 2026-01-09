namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// The quoting by the last trade price.
/// </summary>
[Obsolete("Use QuotingProcessor.")]
public class LastTradeQuotingStrategy : BestByPriceQuotingStrategy
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LastTradeQuotingStrategy"/>.
	/// </summary>
	public LastTradeQuotingStrategy()
	{
	}

	/// <inheritdoc/>
	protected override IQuotingBehavior CreateBehavior()
		=> new LastTradeQuotingBehavior(BestPriceOffset);
}