namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// The quoting by specified level in the order book.
/// </summary>
[Obsolete("Use QuotingProcessor.")]
public class LevelQuotingStrategy : QuotingStrategy
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LevelQuotingStrategy"/>.
	/// </summary>
	public LevelQuotingStrategy()
	{
		_level = Param(nameof(Level), new Range<int>()).SetRequired();
		_ownLevel = Param<bool>(nameof(OwnLevel));
	}

	private readonly StrategyParam<Range<int>> _level;

	/// <summary>
	/// The level in the order book. It specifies the number of quotes to the deep from the best one. By default, it is equal to {0:0} which means quoting by the best quote.
	/// </summary>
	public Range<int> Level
	{
		get => _level.Value;
		set => _level.Value = value;
	}

	private readonly StrategyParam<bool> _ownLevel;

	/// <summary>
	/// To create your own price level in the order book, if there is no quote with necessary price yet. The default is disabled.
	/// </summary>
	public bool OwnLevel
	{
		get => _ownLevel.Value;
		set => _ownLevel.Value = value;
	}

	/// <inheritdoc/>
	protected override IQuotingBehavior CreateBehavior()
		=> new LevelQuotingBehavior(Level, OwnLevel);
}