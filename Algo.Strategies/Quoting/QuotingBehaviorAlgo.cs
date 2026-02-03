namespace StockSharp.Algo.Strategies.Quoting;

using StockSharp.Algo.PositionManagement;

/// <summary>
/// Position modify algorithm that delegates price calculation to <see cref="IQuotingBehavior"/>.
/// </summary>
public class QuotingBehaviorAlgo : IPositionModifyAlgo
{
	private readonly IQuotingBehavior _behavior;
	private readonly Sides _side;
	private readonly Unit _volumePart;
	private readonly Security _security;

	private decimal? _lastTradePrice;
	private decimal? _lastTradeVolume;
	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;
	private QuoteChange[] _bids = [];
	private QuoteChange[] _asks = [];
	private DateTime _lastUpdateTime;

	private decimal? _cachedBestPrice;
	private bool _hasActiveOrder;
	private bool _canceled;

	/// <summary>
	/// Initializes a new instance of the <see cref="QuotingBehaviorAlgo"/>.
	/// </summary>
	/// <param name="behavior">Quoting behavior for price calculation.</param>
	/// <param name="side">Order side.</param>
	/// <param name="volume">Total volume to execute.</param>
	/// <param name="volumePart">Volume part for each slice.</param>
	/// <param name="security">Security (optional, for behaviors that need PriceStep).</param>
	public QuotingBehaviorAlgo(
		IQuotingBehavior behavior,
		Sides side,
		decimal volume,
		Unit volumePart,
		Security security = null)
	{
		if (volume <= 0)
			throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.InvalidValue);

		_behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
		_side = side;
		_volumePart = volumePart ?? throw new ArgumentNullException(nameof(volumePart));
		_security = security;
		RemainingVolume = volume;
	}

	/// <inheritdoc />
	public decimal RemainingVolume { get; private set; }

	/// <inheritdoc />
	public bool IsFinished => RemainingVolume <= 0 || _canceled;

	/// <inheritdoc />
	public void UpdateMarketData(DateTime time, decimal? price, decimal? volume)
	{
		_lastTradePrice = price;
		_lastTradeVolume = volume;
		_lastUpdateTime = time;

		// Call CalculateBestPrice to allow stateful behaviors (like VWAP) to accumulate data
		// and cache the result to avoid double-accumulation in GetNextAction
		_cachedBestPrice = _behavior.CalculateBestPrice(
			_security,
			null,
			_side,
			_bestBidPrice,
			_bestAskPrice,
			_lastTradePrice,
			_lastTradeVolume,
			_bids,
			_asks);
	}

	/// <inheritdoc />
	public void UpdateOrderBook(IOrderBookMessage depth)
	{
		if (depth is null)
			return;

		var bestBid = depth.GetBestBid();
		var bestAsk = depth.GetBestAsk();

		_bestBidPrice = bestBid?.Price;
		_bestAskPrice = bestAsk?.Price;
		_bids = depth.Bids?.ToArray() ?? [];
		_asks = depth.Asks?.ToArray() ?? [];

		// Recalculate best price with new order book data
		_cachedBestPrice = _behavior.CalculateBestPrice(
			_security,
			null,
			_side,
			_bestBidPrice,
			_bestAskPrice,
			_lastTradePrice,
			_lastTradeVolume,
			_bids,
			_asks);
	}

	/// <inheritdoc />
	public PositionModifyAction GetNextAction()
	{
		if (IsFinished)
			return PositionModifyAction.Finished();

		if (_hasActiveOrder)
			return PositionModifyAction.None();

		// Use cached price from UpdateMarketData/UpdateOrderBook
		var price = _cachedBestPrice;

		if (price is null)
			return PositionModifyAction.None();

		// Check if quoting is needed (for behaviors with timing logic like TWAP)
		var quotingPrice = _behavior.NeedQuoting(
			_security,
			null,
			_lastUpdateTime,
			null, // no current order price
			null, // no current order volume
			CalcOrderVolume(),
			price);

		if (quotingPrice is null)
			return PositionModifyAction.None();

		var orderVolume = CalcOrderVolume();
		if (orderVolume <= 0)
			return PositionModifyAction.Finished();

		_hasActiveOrder = true;
		return PositionModifyAction.Register(_side, orderVolume, quotingPrice.Value, OrderTypes.Limit);
	}

	/// <inheritdoc />
	public void OnOrderMatched(decimal matchedVolume)
	{
		_hasActiveOrder = false;
		RemainingVolume -= matchedVolume;

		if (RemainingVolume < 0)
			RemainingVolume = 0;
	}

	/// <inheritdoc />
	public void OnOrderFailed()
	{
		_hasActiveOrder = false;
	}

	/// <inheritdoc />
	public void OnOrderCanceled(decimal matchedVolume)
	{
		_hasActiveOrder = false;
		RemainingVolume -= matchedVolume;

		if (RemainingVolume < 0)
			RemainingVolume = 0;
	}

	/// <inheritdoc />
	public void Cancel()
	{
		_canceled = true;
	}

	/// <inheritdoc />
	public void Dispose()
	{
	}

	private decimal CalcOrderVolume()
	{
		return _volumePart.Type switch
		{
			UnitTypes.Absolute => RemainingVolume.Min((decimal)_volumePart),
			UnitTypes.Percent => RemainingVolume.Min(RemainingVolume * _volumePart.Value / 100m),
			_ => throw new InvalidOperationException(_volumePart.Type.To<string>())
		};
	}
}
