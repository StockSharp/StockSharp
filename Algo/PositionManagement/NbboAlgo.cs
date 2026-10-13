namespace StockSharp.Algo.PositionManagement;

/// <summary>
/// NBBO peg - keep one resting limit order at the National Best Bid
/// (for buys) or National Best Ask (for sells). On every top-of-book
/// move the algo cancels the working order and re-pegs at the new
/// best.
///
/// Used to provide passive liquidity at the touch without crossing
/// the spread. The slice size <see cref="SliceVolume"/> is constant
/// per round-trip; after a fill (or partial fill on cancel) the algo
/// resumes with whatever volume is left until it reaches
/// <see cref="TotalVolume"/>.
///
/// "NBBO" historically refers to the consolidated SIP feed in US
/// markets; here it is the best price the configured market-data feed
/// reports, so the caller is responsible for pointing the feed at the
/// right consolidator (or single venue for non-US instruments).
///
/// A crossed book (best bid above best ask) is quoted like any other:
/// the slice goes to the best price on the side the algo works, which
/// is the only touch it has. While the side it works carries no quote
/// at all no new slice is sent, and an order already resting keeps its
/// place until a best shows up again.
/// </summary>
public sealed class NbboAlgo : IPositionModifyAlgo
{
	private readonly Sides _side;
	private readonly decimal _totalVolume;
	private readonly decimal _sliceVolume;

	private decimal _filledVolume;
	private decimal? _currentBestPrice;
	private decimal? _restingPrice;
	private bool _peggedSideQuoted;
	private bool _orderInFlight;
	private bool _cancelled;
	private bool _cancelSent;

	/// <summary>
	/// Initializes a new instance of the <see cref="NbboAlgo"/>.
	/// </summary>
	/// <param name="side">Order side.</param>
	/// <param name="totalVolume">Total volume to execute.</param>
	/// <param name="sliceVolume">Size of a single resting slice.</param>
	public NbboAlgo(Sides side, decimal totalVolume, decimal sliceVolume)
	{
		if (totalVolume <= 0)
			throw new ArgumentOutOfRangeException(nameof(totalVolume), totalVolume, LocalizedStrings.InvalidValue);
		if (sliceVolume <= 0 || sliceVolume > totalVolume)
			throw new ArgumentOutOfRangeException(nameof(sliceVolume), sliceVolume, LocalizedStrings.InvalidValue);

		_side = side;
		_totalVolume = totalVolume;
		_sliceVolume = sliceVolume;
	}

	/// <summary>
	/// Order side.
	/// </summary>
	public Sides Side => _side;

	/// <summary>
	/// Total volume to execute.
	/// </summary>
	public decimal TotalVolume => _totalVolume;

	/// <summary>
	/// Size of a single resting slice.
	/// </summary>
	public decimal SliceVolume => _sliceVolume;

	/// <summary>
	/// Best price last seen on the side the algo pegs to, or <see langword="null"/> before any book arrived.
	/// </summary>
	public decimal? CurrentBestPrice => _currentBestPrice;

	/// <inheritdoc />
	public decimal RemainingVolume => Math.Max(0, _totalVolume - _filledVolume);

	/// <inheritdoc />
	public bool IsFinished => _cancelled || _filledVolume >= _totalVolume;

	/// <inheritdoc />
	public void UpdateMarketData(DateTime time, decimal? price, decimal? volume) { }

	/// <inheritdoc />
	public void UpdateOrderBook(IOrderBookMessage depth)
	{
		if (depth is null) return;

		decimal? newBest = _side == Sides.Buy
			? depth.GetBestBid()?.Price
			: depth.GetBestAsk()?.Price;

		if (newBest is null)
		{
			// A side that went empty is not a price move: the resting order keeps its place, but
			// there is no touch to send a new slice to until a quote comes back.
			_peggedSideQuoted = false;
			return;
		}

		_peggedSideQuoted = true;
		_currentBestPrice = newBest;
	}

	/// <inheritdoc />
	public PositionModifyAction GetNextAction()
	{
		if (IsFinished) return PositionModifyAction.Finished();

		// Reprice path: cancel first, register again once the cancel is acknowledged. Staleness
		// is measured against the current best on every call, so an order sitting at the best
		// keeps its queue position however the book got there.
		if (_orderInFlight && !_cancelSent && _restingPrice is not null && _restingPrice != _currentBestPrice)
		{
			_cancelSent = true;
			return PositionModifyAction.CancelOrder();
		}

		if (_orderInFlight) return PositionModifyAction.None();
		if (!_peggedSideQuoted) return PositionModifyAction.None();

		var slice = Math.Min(_sliceVolume, RemainingVolume);
		if (slice <= 0) return PositionModifyAction.Finished();

		_restingPrice = _currentBestPrice;
		_orderInFlight = true;
		return PositionModifyAction.Register(_side, slice, _currentBestPrice, OrderTypes.Limit);
	}

	/// <inheritdoc />
	public void OnOrderMatched(decimal matchedVolume)
	{
		_filledVolume += matchedVolume;
		_orderInFlight = false;
		_restingPrice = null;
		_cancelSent = false;
	}

	/// <inheritdoc />
	public void OnOrderFailed()
	{
		_orderInFlight = false;
		_restingPrice = null;
		_cancelSent = false;
	}

	/// <inheritdoc />
	public void OnOrderCanceled(decimal matchedVolume)
	{
		_filledVolume += matchedVolume;
		_orderInFlight = false;
		_restingPrice = null;
		_cancelSent = false;
	}

	/// <inheritdoc />
	public void Cancel() => _cancelled = true;

	/// <inheritdoc />
	public void Dispose() { }
}
