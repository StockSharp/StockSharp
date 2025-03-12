namespace StockSharp.Algo.Strategies.Quoting;

using StockSharp.Algo.Derivatives;

/// <summary>
/// Option volatility quoting.
/// </summary>
[Obsolete("Use QuotingProcessor.")]
public class VolatilityQuotingStrategy : BestByPriceQuotingStrategy
{
	/// <summary>
	/// Initializes a new instance of the <see cref="VolatilityQuotingStrategy"/>.
	/// </summary>
	public VolatilityQuotingStrategy()
	{
		_ivRange = Param(nameof(IVRange), new Range<decimal>());
		_model = Param<IBlackScholes>(nameof(Model));
	}

	private readonly StrategyParam<Range<decimal>> _ivRange;

	/// <summary>
	/// Volatility range.
	/// </summary>
	public Range<decimal> IVRange
	{
		get => _ivRange.Value;
		set => _ivRange.Value = value;
	}

	private readonly StrategyParam<IBlackScholes> _model;

	/// <summary>
	/// <see cref="IBlackScholes"/>
	/// </summary>
	public IBlackScholes Model
	{
		get => _model.Value;
		set => _model.Value = value;
	}

	/// <inheritdoc />
	protected override IQuotingBehavior CreateBehavior()
		=> new VolatilityQuotingBehavior(IVRange, Model);
}