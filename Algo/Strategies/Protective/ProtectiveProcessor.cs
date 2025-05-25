namespace StockSharp.Algo.Strategies.Protective;

/// <summary>
/// Protective strategy processor.
/// </summary>
public class ProtectiveProcessor
{
	private readonly bool _isUpTrend;
	private readonly bool _isTrailing;
	private readonly Unit _protectiveLevel;
	private readonly bool _useMarketOrders;
	private readonly Unit _priceOffset;
	private readonly TimeSpan _timeout;
	private readonly ILogReceiver _logs;
	private readonly Sides _protectiveSide;

	private readonly DateTimeOffset _startedTime;
	private decimal? _prevCurrPrice;
	private decimal _prevBestPrice;

	/// <summary>
	/// Initialize <see cref="ProtectiveProcessor"/>.
	/// </summary>
	/// <param name="protectiveSide">Protected position side.</param>
	/// <param name="protectivePrice">Protected position price.</param>
	/// <param name="isUpTrend">To track price increase or falling.</param>
	/// <param name="isTrailing">Trailing mode.</param>
	/// <param name="protectiveLevel">The protective level. If the <see cref="Unit.Type"/> type is equal to <see cref="UnitTypes.Limit"/>, then the given price is specified. Otherwise, the shift value from the protected trade <see cref="DataType.Ticks"/> is specified.</param>
	/// <param name="useMarketOrders">Whether to use <see cref="OrderTypes.Market"/> for protection.</param>
	/// <param name="priceOffset">The price shift for the registering order. It determines the amount of shift from the best quote (for the buy it is added to the price, for the sell it is subtracted).</param>
	/// <param name="timeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="startedTime">The time when the protective strategy was started.</param>
	/// <param name="logs">The log source.</param>
	public ProtectiveProcessor(Sides protectiveSide, decimal protectivePrice, bool isUpTrend, bool isTrailing, Unit protectiveLevel, bool useMarketOrders, Unit priceOffset, TimeSpan timeout, DateTimeOffset startedTime, ILogReceiver logs)
	{
		if (protectivePrice <= 0)
			throw new ArgumentOutOfRangeException(nameof(protectivePrice), protectivePrice, LocalizedStrings.InvalidValue);

		if (timeout < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout), timeout, LocalizedStrings.InvalidValue);

		_protectiveSide = protectiveSide;
		_isUpTrend = isUpTrend;
		_isTrailing = isTrailing;
		_protectiveLevel = protectiveLevel ?? throw new ArgumentNullException(nameof(protectiveLevel));
		_useMarketOrders = useMarketOrders;
		_priceOffset = priceOffset ?? throw new ArgumentNullException(nameof(priceOffset));
		_timeout = timeout;
		_logs = logs ?? throw new ArgumentNullException(nameof(logs));

		if (_isTrailing && _protectiveLevel.Type == UnitTypes.Limit)
			throw new ArgumentException(LocalizedStrings.TrailingNotSupportLimitProtectiveLevel, nameof(protectiveLevel));

		_startedTime = startedTime;
		_prevBestPrice = protectivePrice;
	}

	/// <summary>
	/// The absolute value of the price when the one is reached the protective strategy is activated.
	/// </summary>
	/// <param name="currentPrice">The current price of the security. If the price is equal to <see langword="null" />, then the activation is not required.</param>
	/// <param name="currentTime">The current time.</param>
	/// <returns>If the price is equal to <see langword="null" /> then the activation is not required.</returns>
	public decimal? GetActivationPrice(decimal? currentPrice, DateTimeOffset currentTime)
	{
		if (currentPrice is decimal currPriceDec2)
			_prevCurrPrice = currPriceDec2;

		decimal getClosePosPrice(decimal closePrice)
		{
			if (_useMarketOrders)
				return 0;

			return (decimal)(closePrice + (_protectiveSide == Sides.Buy ? -_priceOffset : _priceOffset));
		}

		bool isTimeOut()
		{
			if (_timeout == default)
				return false;
			else
				return (currentTime - _startedTime) >= _timeout;
		}

		if (isTimeOut())
		{
			_logs.LogDebug("Timeout.");

			if ((currentPrice ?? _prevCurrPrice) is not decimal closePrice)
			{
				_logs.AddWarningLog("No price for close position.");
				return null;
			}

			return getClosePosPrice(closePrice);
		}

		if (currentPrice is not decimal currPriceDec)
		{
			_logs.LogDebug("Current price is null.");
			return null;
		}

		if (_prevBestPrice == currPriceDec)
			return null;

		decimal? tryActivate()
		{
			var activationPrice = _protectiveLevel.Type == UnitTypes.Limit
				? _protectiveLevel.Value
				: (_isUpTrend ? _prevBestPrice + _protectiveLevel : _prevBestPrice - _protectiveLevel);

			// protectiveLevel may has extra big value.
			// In that case activationPrice may less that zero.
			if (activationPrice <= 0)
				activationPrice = 0.01m;

			if ((_isUpTrend && currPriceDec < activationPrice) || (!_isUpTrend && currPriceDec > activationPrice))
				return null;

			_logs.LogDebug("ActivationPrice={0} CurrPrice={1} ProtectLvl={2}", activationPrice, currPriceDec, _protectiveLevel);

			return getClosePosPrice(currPriceDec);
		}

		if (_isTrailing)
		{
			//_logs.AddDebugLog("PrevPrice={0} CurrPrice={1}", _prevBestPrice, currPriceDec);

			var isLong = _protectiveSide == Sides.Buy;

			if	(
					(isLong && _prevBestPrice < currPriceDec) ||
					(!isLong && _prevBestPrice > currPriceDec)
				)
			{
				_prevBestPrice = currPriceDec;
				return null;
			}
		}

		return tryActivate();
	}
}