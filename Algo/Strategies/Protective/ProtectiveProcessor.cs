namespace StockSharp.Algo.Strategies.Protective;

using System;

using StockSharp.Localization;
using StockSharp.Logging;
using StockSharp.Messages;

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

	private DateTimeOffset? _startedTime;
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
	/// <param name="logs">The log source.</param>
	public ProtectiveProcessor(Sides protectiveSide, decimal protectivePrice, bool isUpTrend, bool isTrailing, Unit protectiveLevel, bool useMarketOrders, Unit priceOffset, TimeSpan timeout, ILogReceiver logs)
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

		_prevBestPrice = protectivePrice;
	}

	/// <summary>
	/// The absolute value of the price when the one is reached the protective strategy is activated.
	/// </summary>
	/// <remarks>If the price is equal to <see langword="null" /> then the activation is not required.</remarks>
	public decimal? GetActivationPrice(decimal? currentPrice, DateTimeOffset currentTime)
	{
		if (currentPrice is decimal currPriceDec2)
			_prevCurrPrice = currPriceDec2;

		decimal? getClosePosPrice(decimal closePrice)
		{
			if (_useMarketOrders)
				return 0;

			return (decimal)(closePrice + (_protectiveSide == Sides.Buy ? -_priceOffset : _priceOffset));
		}

		bool isTimeOut()
		{
			if (_timeout == default)
				return false;
			else if (_startedTime is null)
			{
				_startedTime = currentTime;
				return false;
			}
			else
				return (currentTime - _startedTime.Value) >= _timeout;
		}

		if (isTimeOut())
		{
			_logs.AddDebugLog("Timeout.");

			if ((currentPrice ?? _prevCurrPrice) is not decimal closePrice)
			{
				_logs.AddWarningLog("No price for close position.");
				return null;
			}

			return getClosePosPrice(closePrice);
		}

		if (currentPrice is not decimal currPriceDec)
		{
			_logs.AddDebugLog("Current price is null.");
			return null;
		}

		decimal? isActivation()
		{
			var activationPrice = _protectiveLevel.Type == UnitTypes.Limit
				? _protectiveLevel.Value
				: (_isUpTrend ? _prevBestPrice + _protectiveLevel.Value : _prevBestPrice - _protectiveLevel.Value);

			// protectiveLevel may has extra big value.
			// In that case activationPrice may less that zero.
			if (activationPrice <= 0)
				activationPrice = 0.01m;

			if ((_isUpTrend && currPriceDec < activationPrice) || (!_isUpTrend && currPriceDec > activationPrice))
				return null;

			_logs.AddDebugLog("ActivationPrice={0} CurrPrice={1} ProtectLvl={2}", activationPrice, currPriceDec, _protectiveLevel);

			return getClosePosPrice(currPriceDec);
		}

		if (_isTrailing)
		{
			//_logs.AddDebugLog("PrevPrice={0} CurrPrice={1}", _prevBestPrice, currPriceDec);

			if (_isUpTrend)
			{
				if (_prevBestPrice > currPriceDec)
					_prevBestPrice = currPriceDec;
				else
				{
					if (isActivation() is decimal closePrice)
						return closePrice;
				}
			}
			else
			{
				if (_prevBestPrice < currPriceDec)
					_prevBestPrice = currPriceDec;
				else
				{
					if (isActivation() is decimal closePrice)
						return closePrice;
				}
			}
		}
		else
		{
			if (isActivation() is decimal closePrice)
				return closePrice;
		}

		return null;
	}
}