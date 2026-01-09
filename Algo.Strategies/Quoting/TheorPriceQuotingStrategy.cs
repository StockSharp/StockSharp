namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// Option theoretical price quoting.
/// </summary>
[Obsolete("Use QuotingProcessor.")]
public class TheorPriceQuotingStrategy : BestByPriceQuotingStrategy
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TheorPriceQuotingStrategy"/>.
	/// </summary>
	public TheorPriceQuotingStrategy()
	{
		_theorPriceOffset = Param(nameof(TheorPriceOffset), new Range<Unit>());
	}

	private readonly StrategyParam<Range<Unit>> _theorPriceOffset;

	/// <summary>
	/// Theoretical price offset.
	/// </summary>
	public Range<Unit> TheorPriceOffset
	{
		get => _theorPriceOffset.Value;
		set => _theorPriceOffset.Value = value;
	}

	/// <inheritdoc />
	protected override IQuotingBehavior CreateBehavior()
		=> new TheorPriceQuotingBehavior(TheorPriceOffset);
}