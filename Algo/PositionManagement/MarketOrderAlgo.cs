namespace StockSharp.Algo.PositionManagement;

/// <summary>
/// Market order execution algorithm.
/// </summary>
public class MarketOrderAlgo : IPositionModifyAlgo
{
	private readonly Sides _side;
	private readonly decimal _volume;
	private bool _orderSent;
	private bool _finished;
	private bool _canceled;

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketOrderAlgo"/>.
	/// </summary>
	/// <param name="side">Order side.</param>
	/// <param name="volume">Total volume to execute.</param>
	public MarketOrderAlgo(Sides side, decimal volume)
	{
		if (volume <= 0)
			throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.InvalidValue);

		_side = side;
		_volume = volume;
	}

	/// <inheritdoc />
	public decimal RemainingVolume => _finished ? 0 : _volume;

	/// <inheritdoc />
	public bool IsFinished => _finished;

	/// <inheritdoc />
	public void UpdateMarketData(DateTime time, decimal? price, decimal? volume)
	{
	}

	/// <inheritdoc />
	public void UpdateOrderBook(IOrderBookMessage depth)
	{
	}

	/// <inheritdoc />
	public PositionModifyAction GetNextAction()
	{
		if (_finished || _canceled)
			return PositionModifyAction.Finished();

		if (_orderSent)
			return PositionModifyAction.None();

		_orderSent = true;
		return PositionModifyAction.Register(_side, _volume, null, OrderTypes.Market);
	}

	/// <inheritdoc />
	public void OnOrderMatched(decimal matchedVolume)
	{
		_finished = true;
	}

	/// <inheritdoc />
	public void OnOrderFailed()
	{
		_orderSent = false;
		_finished = true;
	}

	/// <inheritdoc />
	public void OnOrderCanceled(decimal matchedVolume)
	{
		_finished = true;
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
}
