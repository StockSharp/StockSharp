namespace StockSharp.Algo.PositionManagement;

/// <summary>
/// Time-Weighted Average Price (TWAP). Splits the total volume into
/// <see cref="SliceCount"/> equal slices and sends one child order per
/// slice at the configured interval - slice 1 at start, slice 2 at
/// start + interval, ... - until the time window <see cref="StartAt"/>
/// .. <see cref="EndAt"/> elapses.
///
/// Each slice is a Market order by default (immediate execution).
/// Setting <see cref="LimitPrice"/> sends Limit orders instead, capped
/// at the supplied price for buys / floored for sells.
///
/// Outstanding child orders are tracked by the caller; this algo only
/// emits Register actions when the current slice is due and reports
/// finished once the total volume is exhausted or EndAt has passed.
/// </summary>
public sealed class TwapAlgo : IPositionModifyAlgo
{
	private readonly Sides _side;
	private readonly decimal _totalVolume;
	private readonly int _sliceCount;
	private readonly DateTime _startAt;
	private readonly DateTime _endAt;
	private readonly decimal? _limitPrice;

	private decimal _filledVolume;
	private int _slicesSent;
	private bool _orderInFlight;
	private bool _cancelled;
	private DateTime _lastTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="TwapAlgo"/>.
	/// </summary>
	/// <param name="side">Order side.</param>
	/// <param name="totalVolume">Total volume to execute.</param>
	/// <param name="sliceCount">How many slices the volume is cut into.</param>
	/// <param name="startAt">When the algo may start.</param>
	/// <param name="endAt">When the algo must be done.</param>
	/// <param name="limitPrice">Price limit, or <see langword="null"/> to send market orders.</param>
	public TwapAlgo(Sides side, decimal totalVolume, int sliceCount, DateTime startAt, DateTime endAt, decimal? limitPrice = null)
	{
		if (totalVolume <= 0)
			throw new ArgumentOutOfRangeException(nameof(totalVolume), totalVolume, LocalizedStrings.InvalidValue);
		if (sliceCount <= 0)
			throw new ArgumentOutOfRangeException(nameof(sliceCount), sliceCount, LocalizedStrings.InvalidValue);
		if (endAt <= startAt)
			throw new ArgumentOutOfRangeException(nameof(endAt), endAt, LocalizedStrings.InvalidValue);

		_side = side;
		_totalVolume = totalVolume;
		_sliceCount = sliceCount;
		_startAt = startAt;
		_endAt = endAt;
		_limitPrice = limitPrice;
	}

	/// <summary>
	/// Order side.
	/// </summary>
	public Sides Side => _side;

	/// <summary>
	/// How many slices the volume is cut into.
	/// </summary>
	public int SliceCount => _sliceCount;

	/// <summary>
	/// When the algo may start.
	/// </summary>
	public DateTime StartAt => _startAt;

	/// <summary>
	/// When the algo must be done.
	/// </summary>
	public DateTime EndAt => _endAt;

	/// <summary>
	/// Price limit, or <see langword="null"/> when slices go out as market orders.
	/// </summary>
	public decimal? LimitPrice => _limitPrice;

	/// <inheritdoc />
	public decimal RemainingVolume => Math.Max(0, _totalVolume - _filledVolume);

	/// <inheritdoc />
	public bool IsFinished => _cancelled || _filledVolume >= _totalVolume || _slicesSent >= _sliceCount;

	/// <inheritdoc />
	public void UpdateMarketData(DateTime time, decimal? price, decimal? volume) => _lastTime = time;

	/// <inheritdoc />
	public void UpdateOrderBook(IOrderBookMessage depth) { /* TWAP ignores book depth */ }

	/// <inheritdoc />
	public PositionModifyAction GetNextAction()
	{
		if (IsFinished) return PositionModifyAction.Finished();
		if (_orderInFlight) return PositionModifyAction.None();
		if (_lastTime == default || _lastTime < _startAt) return PositionModifyAction.None();

		// Slice schedule: slice k fires at StartAt + k * step, k = 0..N-1
		// where step = (EndAt - StartAt) / N.
		var totalSpan = (_endAt - _startAt).TotalSeconds;
		if (totalSpan <= 0) return PositionModifyAction.Finished();

		var step = totalSpan / _sliceCount;
		var elapsed = (_lastTime - _startAt).TotalSeconds;
		var dueSlices = (int)Math.Floor(elapsed / step) + 1;
		if (dueSlices <= _slicesSent) return PositionModifyAction.None();

		var sliceVolume = RemainingVolume / Math.Max(1, _sliceCount - _slicesSent);
		_slicesSent++;
		_orderInFlight = true;

		var orderType = _limitPrice is null ? OrderTypes.Market : OrderTypes.Limit;
		return PositionModifyAction.Register(_side, sliceVolume, _limitPrice, orderType);
	}

	/// <inheritdoc />
	public void OnOrderMatched(decimal matchedVolume)
	{
		_filledVolume += matchedVolume;
		_orderInFlight = false;
	}

	/// <inheritdoc />
	public void OnOrderFailed() => _orderInFlight = false;

	/// <inheritdoc />
	public void OnOrderCanceled(decimal matchedVolume)
	{
		_filledVolume += matchedVolume;
		_orderInFlight = false;
	}

	/// <inheritdoc />
	public void Cancel() => _cancelled = true;

	/// <inheritdoc />
	public void Dispose() { }
}
