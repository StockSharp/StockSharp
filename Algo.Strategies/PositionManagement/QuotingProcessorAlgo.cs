namespace StockSharp.Algo.PositionManagement;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// Quoting-based position modify algorithm that delegates to <see cref="QuotingProcessor"/>.
/// </summary>
public class QuotingProcessorAlgo : IPositionModifyAlgo
{
	private readonly QuotingProcessor _processor;
	private bool _started;
	private bool _finished;
	private decimal _remainingVolume;

	/// <summary>
	/// Initializes a new instance of the <see cref="QuotingProcessorAlgo"/>.
	/// </summary>
	/// <param name="behavior">Quoting behavior for price calculation.</param>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="side">Order side.</param>
	/// <param name="volume">Total volume to execute.</param>
	/// <param name="maxOrderVolume">Maximum volume per order.</param>
	/// <param name="timeOut">Timeout for quoting.</param>
	/// <param name="subProvider">Subscription provider.</param>
	/// <param name="container">Market rule container.</param>
	/// <param name="transProvider">Transaction provider.</param>
	/// <param name="timeProvider">Time provider.</param>
	/// <param name="mdProvider">Market data provider.</param>
	/// <param name="isAllowed">Trading mode check function.</param>
	/// <param name="useBidAsk">Whether to use bid/ask from order book.</param>
	/// <param name="useTicks">Whether to use last trade price.</param>
	public QuotingProcessorAlgo(
		IQuotingBehavior behavior,
		Security security, Portfolio portfolio,
		Sides side, decimal volume, decimal maxOrderVolume,
		TimeSpan timeOut,
		ISubscriptionProvider subProvider,
		IMarketRuleContainer container,
		ITransactionProvider transProvider,
		ITimeProvider timeProvider,
		IMarketDataProvider mdProvider,
		Func<StrategyTradingModes, bool> isAllowed,
		bool useBidAsk, bool useTicks)
	{
		_remainingVolume = volume;

		_processor = new(
			behavior, security, portfolio,
			side, volume, maxOrderVolume,
			timeOut, subProvider, container,
			transProvider, timeProvider, mdProvider,
			isAllowed, useBidAsk, useTicks);

		_processor.Finished += success =>
		{
			_finished = true;
			_remainingVolume = _processor.LeftVolume;
		};

		_processor.OwnTrade += trade =>
		{
			_remainingVolume = _processor.LeftVolume;
		};
	}

	/// <inheritdoc />
	public decimal RemainingVolume => _remainingVolume;

	/// <inheritdoc />
	public bool IsFinished => _finished;

	/// <inheritdoc />
	public void UpdateMarketData(DateTime time, decimal? price, decimal? volume)
	{
		if (!_started)
		{
			_started = true;
			_processor.Start();
		}
	}

	/// <inheritdoc />
	public void UpdateOrderBook(IOrderBookMessage depth)
	{
		// QuotingProcessor manages order book subscription internally
	}

	/// <inheritdoc />
	public PositionModifyAction GetNextAction()
	{
		// QuotingProcessor manages its own orders internally
		if (_finished)
			return PositionModifyAction.Finished();

		return PositionModifyAction.None();
	}

	/// <inheritdoc />
	public void OnOrderMatched(decimal matchedVolume)
	{
		// managed internally by QuotingProcessor
	}

	/// <inheritdoc />
	public void OnOrderFailed()
	{
		// managed internally by QuotingProcessor
	}

	/// <inheritdoc />
	public void OnOrderCanceled(decimal matchedVolume)
	{
		// managed internally by QuotingProcessor
	}

	/// <inheritdoc />
	public void Cancel()
	{
		_processor.Stop();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_processor.Dispose();
	}
}
