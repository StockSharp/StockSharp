namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// The strategy realizing volume quoting algorithm by the limited price.
/// </summary>
[Obsolete("Use QuotingProcessor.")]
public class LimitQuotingStrategy : QuotingStrategy
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LimitQuotingStrategy"/>.
	/// </summary>
	public LimitQuotingStrategy()
	{
		_limitPrice = Param(nameof(LimitPrice), 1m);
	}

	private readonly StrategyParam<decimal> _limitPrice;

	/// <summary>
	/// The limited price for quoted orders.
	/// </summary>
	public decimal LimitPrice
	{
		get => _limitPrice.Value;
		set => _limitPrice.Value = value;
	}

	/// <inheritdoc/>
	protected override IQuotingBehavior CreateBehavior()
		=> new LimitQuotingBehavior(LimitPrice);
}