namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// The quoting by the best price. For this quoting the shift from the best price <see cref="BestPriceOffset"/> is specified, on which quoted order can be changed.
/// </summary>
[Obsolete("Use QuotingProcessor.")]
public class BestByPriceQuotingStrategy : QuotingStrategy
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BestByPriceQuotingStrategy"/>.
	/// </summary>
	public BestByPriceQuotingStrategy()
	{
		_bestPriceOffset = Param(nameof(BestPriceOffset), new Unit());
	}

	private readonly StrategyParam<Unit> _bestPriceOffset;

	/// <summary>
	/// The shift from the best price, on which quoted order can be changed.
	/// </summary>
	public Unit BestPriceOffset
	{
		get => _bestPriceOffset.Value;
		set => _bestPriceOffset.Value = value;
	}

	/// <inheritdoc/>
	protected override IQuotingBehavior CreateBehavior()
		=> new BestByPriceQuotingBehavior(BestPriceOffset);
}