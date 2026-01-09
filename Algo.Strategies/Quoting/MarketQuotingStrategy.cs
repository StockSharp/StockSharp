namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// The quoting by the market price.
/// </summary>
[Obsolete("Use QuotingProcessor.")]
public class MarketQuotingStrategy : BestByPriceQuotingStrategy
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MarketQuotingStrategy"/>.
	/// </summary>
	public MarketQuotingStrategy()
	{
		_priceType = Param(nameof(PriceType), MarketPriceTypes.Following);
		_priceOffset = Param(nameof(PriceOffset), new Unit());

		UseLastTradePrice = false;
	}

	private readonly StrategyParam<MarketPriceTypes> _priceType;

	/// <summary>
	/// The market price type. The default value is <see cref="MarketPriceTypes.Following"/>.
	/// </summary>
	public MarketPriceTypes PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}

	private readonly StrategyParam<Unit> _priceOffset;

	/// <summary>
	/// The price shift for the registering order. It determines the amount of shift from the best quote (for the buy it is added to the price, for the sell it is subtracted).
	/// </summary>
	public Unit PriceOffset
	{
		get => _priceOffset.Value;
		set => _priceOffset.Value = value;
	}

	/// <inheritdoc/>
	protected override IQuotingBehavior CreateBehavior()
		=> new MarketQuotingBehavior(PriceOffset, BestPriceOffset, PriceType);
}
