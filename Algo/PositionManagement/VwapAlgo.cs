namespace StockSharp.Algo.PositionManagement;

/// <summary>
/// Volume-Weighted Average Price (VWAP). Tries to keep cumulative
/// child-order participation in proportion to the cumulative market
/// traded volume - when the market trades X% of its expected day
/// volume, the algo aims to have sent X% of <see cref="TotalVolume"/>.
///
/// Without an a-priori intraday volume profile the algo uses the live
/// volume observed since <see cref="StartAt"/>. Two policy parameters
/// shape the behaviour:
///
/// - <see cref="ParticipationRate"/> caps the fraction of the live
///   market volume the algo will take (0.10 = 10%). Even when behind
///   schedule the algo will not exceed this share.
/// - <see cref="MinSliceVolume"/> sets a floor - fragments smaller than
///   this aren't worth a round-trip and accumulate until they cross
///   the threshold.
/// </summary>
public sealed class VwapAlgo : IPositionModifyAlgo
{
	private readonly Sides _side;
	private readonly decimal _totalVolume;
	private readonly DateTime _startAt;
	private readonly DateTime _endAt;
	private readonly decimal _participationRate;
	private readonly decimal _minSliceVolume;
	private readonly decimal? _limitPrice;

	private decimal _marketVolumeSinceStart;
	private decimal _filledVolume;
	private decimal _sentVolume;
	private bool _orderInFlight;
	private bool _cancelled;
	private DateTime _lastTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="VwapAlgo"/>.
	/// </summary>
	/// <param name="side">Order side.</param>
	/// <param name="totalVolume">Total volume to execute.</param>
	/// <param name="startAt">When the algo may start.</param>
	/// <param name="endAt">When the algo must be done.</param>
	/// <param name="participationRate">Fraction of the live market volume the algo will take, within (0, 1].</param>
	/// <param name="minSliceVolume">Smallest slice worth a round-trip.</param>
	/// <param name="limitPrice">Price limit, or <see langword="null"/> to send market orders.</param>
	public VwapAlgo(Sides side, decimal totalVolume, DateTime startAt, DateTime endAt,
		decimal participationRate = 0.10m, decimal minSliceVolume = 1m, decimal? limitPrice = null)
	{
		if (totalVolume <= 0)
			throw new ArgumentOutOfRangeException(nameof(totalVolume), totalVolume, LocalizedStrings.InvalidValue);
		if (endAt <= startAt)
			throw new ArgumentOutOfRangeException(nameof(endAt), endAt, LocalizedStrings.InvalidValue);
		if (participationRate <= 0 || participationRate > 1)
			throw new ArgumentOutOfRangeException(nameof(participationRate), participationRate, LocalizedStrings.InvalidValue);
		if (minSliceVolume <= 0)
			throw new ArgumentOutOfRangeException(nameof(minSliceVolume), minSliceVolume, LocalizedStrings.InvalidValue);

		_side = side;
		_totalVolume = totalVolume;
		_startAt = startAt;
		_endAt = endAt;
		_participationRate = participationRate;
		_minSliceVolume = minSliceVolume;
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
	/// Fraction of the live market volume the algo will take.
	/// </summary>
	public decimal ParticipationRate => _participationRate;

	/// <summary>
	/// Smallest slice worth a round-trip.
	/// </summary>
	public decimal MinSliceVolume => _minSliceVolume;

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
	public bool IsFinished => _cancelled || _filledVolume >= _totalVolume || _lastTime >= _endAt;

	/// <inheritdoc />
	public void UpdateMarketData(DateTime time, decimal? price, decimal? volume)
	{
		// The observed clock only moves forward: once it reached EndAt the algo is finished, and an
		// out-of-order tick dated inside the window must not put it back to work.
		if (time > _lastTime)
			_lastTime = time;

		if (time >= _startAt && volume is { } v && v > 0)
			_marketVolumeSinceStart += v;
	}

	/// <inheritdoc />
	public void UpdateOrderBook(IOrderBookMessage depth) { }

	/// <inheritdoc />
	public PositionModifyAction GetNextAction()
	{
		if (IsFinished) return PositionModifyAction.Finished();
		if (_orderInFlight) return PositionModifyAction.None();
		if (_lastTime == default || _lastTime < _startAt) return PositionModifyAction.None();

		// Target sent volume = participation rate x market volume since start,
		// capped at the total. Slice = target - already sent.
		var target = Math.Min(_totalVolume, _participationRate * _marketVolumeSinceStart);
		var slice = target - _sentVolume;

		if (slice < _minSliceVolume) return PositionModifyAction.None();

		slice = Math.Min(slice, RemainingVolume);
		_sentVolume += slice;
		_orderInFlight = true;

		var orderType = _limitPrice is null ? OrderTypes.Market : OrderTypes.Limit;
		return PositionModifyAction.Register(_side, slice, _limitPrice, orderType);
	}

	/// <inheritdoc />
	public void OnOrderMatched(decimal matchedVolume)
	{
		_filledVolume += matchedVolume;
		_orderInFlight = false;
	}

	/// <inheritdoc />
	public void OnOrderFailed()
	{
		// Recover sent counter - the failed in-flight slice never actually consumed market
		// participation. Only that slice is rolled back, not the whole remaining order.
		_sentVolume = SentVolumeAfterFailedSlice(_sentVolume, _filledVolume);
		_orderInFlight = false;
	}

	/// <summary>
	/// The sent-volume counter after the current in-flight slice fails. Only the in-flight slice
	/// (sent minus filled) is rolled back - subtracting the whole remaining order would erase
	/// earlier sends and make the algorithm re-send them, breaching the participation cap.
	/// </summary>
	/// <param name="sentVolume">Volume sent so far.</param>
	/// <param name="filledVolume">Volume filled so far.</param>
	/// <returns>The sent-volume counter to carry on with.</returns>
	public static decimal SentVolumeAfterFailedSlice(decimal sentVolume, decimal filledVolume)
	{
		// The in-flight slice is everything sent but not yet filled; only it failed.
		var inFlight = Math.Max(0m, sentVolume - filledVolume);
		return Math.Max(0m, sentVolume - inFlight);
	}

	/// <inheritdoc />
	public void OnOrderCanceled(decimal matchedVolume)
	{
		_filledVolume += matchedVolume;

		// Only the part that traded consumed market participation; the unfilled remainder of the
		// cancelled slice is rolled back off the counter so it can go out again.
		_sentVolume = SentVolumeAfterFailedSlice(_sentVolume, _filledVolume);
		_orderInFlight = false;
	}

	/// <inheritdoc />
	public void Cancel() => _cancelled = true;

	/// <inheritdoc />
	public void Dispose() { }
}
