namespace StockSharp.Algo.Strategies.Protective;

using System;

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
	private bool _isTrailingActivated;
	private decimal? _prevBestPrice;
	private readonly decimal _protectivePrice;
	private readonly Sides _protectiveSide;

	private DateTimeOffset? _startedTime;

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
		_protectiveSide = protectiveSide;
		_protectivePrice = protectivePrice;
		_isUpTrend = isUpTrend;
		_isTrailing = isTrailing;
		_protectiveLevel = protectiveLevel;
		_useMarketOrders = useMarketOrders;
		_priceOffset = priceOffset;
		_timeout = timeout;
	}

	/// <summary>
	/// Protected position side
	/// </summary>
	public Sides ProtectiveSide => _protectiveSide;

	/// <summary>
	/// The absolute value of the price when the one is reached the protective strategy is activated.
	/// </summary>
	/// <remarks>If the price is equal to <see langword="null" /> then the activation is not required.</remarks>
	public virtual decimal? GetActivationPrice(decimal? currentPrice, DateTimeOffset currentTime)
	{
		_prevBestPrice ??= _protectivePrice;

		if (currentPrice is null)
		{
			this.AddDebugLog("Current price is null.");
			return null;
		}

		decimal? getClosePosPrice()
		{
			if (_useMarketOrders)
				return 0;

			//if (!Security.Board.IsSupportMarketOrders)
			//	return this.GetMarketPrice(QuotingDirection);

			if (currentPrice is not null)
				return (decimal)(currentPrice.Value + (_protectiveSide == Sides.Buy ? -_priceOffset : _priceOffset));

			this.AddWarningLog("No best price.");
			return null;
		}

		bool isTimeOut()
		{
			if (_startedTime is null)
			{
				_startedTime = currentTime;
				return false;
			}

			return _timeout > TimeSpan.Zero && (currentTime - _startedTime.Value) >= _timeout;
		}

		if (isTimeOut() && (_isUpTrend ? currentPrice > _protectivePrice : currentPrice < _protectivePrice))
		{
			this.AddDebugLog("Timeout.");
			return getClosePosPrice();
		}

		this.AddDebugLog("PrevBest={0} CurrBest={1}", _prevBestPrice, currentPrice);

		if (_isTrailing)
		{
			if (_isUpTrend)
			{
				if (_prevBestPrice < currentPrice)
				{
					_prevBestPrice = currentPrice;
				}
				else if (_prevBestPrice > currentPrice)
				{
					_isTrailingActivated = true;
				}
			}
			else
			{
				if (_prevBestPrice > currentPrice)
				{
					_prevBestPrice = currentPrice;
				}
				else if (_prevBestPrice < currentPrice)
				{
					_isTrailingActivated = true;
				}
			}

			if (!_isTrailingActivated)
				return null;

			var activationPrice = _isUpTrend
				? _prevBestPrice.Value - _protectiveLevel
				: _prevBestPrice.Value + _protectiveLevel;

			this.AddDebugLog("ActivationPrice={0} level={1}", activationPrice, _protectiveLevel);

			if (_isUpTrend)
			{
				if (currentPrice <= activationPrice)
					return getClosePosPrice();
			}
			else
			{
				if (currentPrice >= activationPrice)
					return getClosePosPrice();
			}

			return null;
		}
		else
		{
			var activationPrice = (_protectiveLevel.Type == UnitTypes.Limit)
				? _protectiveLevel
				: (_isUpTrend ? _prevBestPrice + _protectiveLevel : _prevBestPrice - _protectiveLevel);

			this.AddDebugLog("ActivationPrice={0} level={1}", activationPrice, _protectiveLevel);

			// protectiveLevel may has extra big value.
			// In that case activationPrice may less that zero.
			if (activationPrice <= 0)
				activationPrice = 0.01m;

			if (_isUpTrend)
			{
				if (currentPrice >= activationPrice)
					return getClosePosPrice();
			}
			else
			{
				if (currentPrice <= activationPrice)
					return getClosePosPrice();
			}

			return null;
		}
	}
}