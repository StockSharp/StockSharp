namespace StockSharp.Algo.Strategies.Protective;

using System;

using StockSharp.Localization;
using StockSharp.Logging;
using StockSharp.Messages;

/// <summary>
/// Protective strategy processor.
/// </summary>
public class ProtectiveProcessor : BaseLogReceiver
{
	private readonly bool _isUpTrend;
	private readonly bool _isTrailing;
	private readonly Unit _protectiveLevel;
	private readonly bool _useMarketOrders;
	private readonly Unit _priceOffset;
	private readonly TimeSpan _timeout;
	private decimal? _prevBestPrice;
	private readonly decimal _protectivePrice;
	private readonly Sides _protectiveSide;

	private DateTimeOffset? _startedTime;
	private decimal? _prevCurrPrice;

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
	/// <param name="timeout"></param>
	public ProtectiveProcessor(Sides protectiveSide, decimal protectivePrice, bool isUpTrend, bool isTrailing, Unit protectiveLevel, bool useMarketOrders, Unit priceOffset, TimeSpan timeout)
	{
		if (protectivePrice <= 0)
			throw new ArgumentOutOfRangeException(nameof(protectivePrice), protectivePrice, LocalizedStrings.InvalidValue);

		if (timeout < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout), timeout, LocalizedStrings.InvalidValue);

		_protectiveSide = protectiveSide;
		_protectivePrice = protectivePrice;
		_isUpTrend = isUpTrend;
		_isTrailing = isTrailing;
		_protectiveLevel = protectiveLevel ?? throw new ArgumentNullException(nameof(protectiveLevel));
		_useMarketOrders = useMarketOrders;
		_priceOffset = priceOffset ?? throw new ArgumentNullException(nameof(priceOffset));
		_timeout = timeout;
	}

	/// <summary>
	/// The absolute value of the price when the one is reached the protective strategy is activated.
	/// </summary>
	/// <remarks>If the price is equal to <see langword="null" /> then the activation is not required.</remarks>
	public decimal? GetActivationPrice(decimal? currentPrice, DateTimeOffset currentTime)
	{
		_prevCurrPrice ??= currentPrice;

		decimal? getClosePosPrice()
		{
			if (_useMarketOrders)
				return 0;

			if ((currentPrice ?? _prevCurrPrice) is not decimal closePrice)
			{
				this.AddWarningLog("No price for close position.");
				return null;
			}

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
			this.AddDebugLog("Timeout.");
			return getClosePosPrice();
		}

		if (currentPrice is not decimal currPriceDec)
		{
			this.AddDebugLog("Current price is null.");
			return null;
		}

		if (_isTrailing)
		{
			this.AddDebugLog("PrevBest={0} CurrBest={1}", _prevBestPrice, currPriceDec);

			if (_isUpTrend)
			{
				if (_prevBestPrice < currPriceDec)
					_prevBestPrice = currPriceDec;
				else if (_prevBestPrice > currPriceDec)
					return getClosePosPrice();
			}
			else
			{
				if (_prevBestPrice > currPriceDec)
					_prevBestPrice = currPriceDec;
				else if (_prevBestPrice < currPriceDec)
					return getClosePosPrice();
			}
		}
		else
		{
			var activationPrice = (_protectiveLevel.Type == UnitTypes.Limit)
				? _protectiveLevel
				: (_isUpTrend ? _protectivePrice + _protectiveLevel : _protectivePrice - _protectiveLevel);

			this.AddDebugLog("ActivationPrice={0} level={1}", activationPrice, _protectiveLevel);

			// protectiveLevel may has extra big value.
			// In that case activationPrice may less that zero.
			if (activationPrice <= 0)
				activationPrice = 0.01m;

			if (_isUpTrend)
			{
				if (currPriceDec >= activationPrice)
					return getClosePosPrice();
			}
			else
			{
				if (currPriceDec <= activationPrice)
					return getClosePosPrice();
			}
		}

		return null;
	}
}