namespace StockSharp.Algo.PositionManagement;

/// <summary>
/// Iceberg - keep one resting limit order with only
/// <see cref="DisplayVolume"/> visible. When the displayed slice fills,
/// the algo emits the next slice at the same price (until total volume
/// is exhausted or it is cancelled).
///
/// Pure venue-agnostic implementation: the algo doesn't ask the venue
/// for native iceberg support; it manages the slicing itself by
/// re-registering after each fill. Native-iceberg venues stay
/// compatible (the algo simply runs one slice end-to-end and then is
/// finished if the venue accepted the bulk size - but most regulated
/// venues now ban naked icebergs, so client-side slicing is the
/// portable path).
/// </summary>
public sealed class IcebergAlgo : IPositionModifyAlgo
{
	private readonly Sides _side;
	private readonly decimal _totalVolume;
	private readonly decimal _displayVolume;
	private readonly decimal _limitPrice;

	private decimal _filledVolume;
	private bool _orderInFlight;
	private bool _cancelled;

	/// <summary>
	/// Initializes a new instance of the <see cref="IcebergAlgo"/>.
	/// </summary>
	/// <param name="side">Order side.</param>
	/// <param name="totalVolume">Total volume to execute.</param>
	/// <param name="displayVolume">How much of the total is shown at a time.</param>
	/// <param name="limitPrice">Price every slice rests at.</param>
	public IcebergAlgo(Sides side, decimal totalVolume, decimal displayVolume, decimal limitPrice)
	{
		if (totalVolume <= 0)
			throw new ArgumentOutOfRangeException(nameof(totalVolume), totalVolume, LocalizedStrings.InvalidValue);
		if (displayVolume <= 0 || displayVolume > totalVolume)
			throw new ArgumentOutOfRangeException(nameof(displayVolume), displayVolume, LocalizedStrings.InvalidValue);
		if (limitPrice <= 0)
			throw new ArgumentOutOfRangeException(nameof(limitPrice), limitPrice, LocalizedStrings.InvalidValue);

		_side = side;
		_totalVolume = totalVolume;
		_displayVolume = displayVolume;
		_limitPrice = limitPrice;
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
	/// How much of the total is shown at a time.
	/// </summary>
	public decimal DisplayVolume => _displayVolume;

	/// <summary>
	/// Price every slice rests at.
	/// </summary>
	public decimal LimitPrice => _limitPrice;

	/// <inheritdoc />
	public decimal RemainingVolume => Math.Max(0, _totalVolume - _filledVolume);

	/// <inheritdoc />
	public bool IsFinished => _cancelled || _filledVolume >= _totalVolume;

	/// <inheritdoc />
	public void UpdateMarketData(DateTime time, decimal? price, decimal? volume) { }

	/// <inheritdoc />
	public void UpdateOrderBook(IOrderBookMessage depth) { }

	/// <inheritdoc />
	public PositionModifyAction GetNextAction()
	{
		if (IsFinished) return PositionModifyAction.Finished();
		if (_orderInFlight) return PositionModifyAction.None();

		// Next slice = min(DisplayVolume, remaining).
		var slice = Math.Min(_displayVolume, RemainingVolume);
		if (slice <= 0) return PositionModifyAction.Finished();

		_orderInFlight = true;
		return PositionModifyAction.Register(_side, slice, _limitPrice, OrderTypes.Limit);
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
