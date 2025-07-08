namespace StockSharp.Algo.Strategies.Quoting;

using StockSharp.Algo.Derivatives;
using StockSharp.BusinessEntities;

/// <summary>
/// Defines the behavior for calculating the best price and determining if quoting is needed.
/// </summary>
public interface IQuotingBehavior
{
	/// <summary>
	/// Calculates the best price for quoting based on market data.
	/// </summary>
	/// <param name="security"><see cref="Security"/></param>
	/// <param name="provider"><see cref="IMarketDataProvider"/></param>
	/// <param name="quotingDirection">The direction of quoting (Buy or Sell).</param>
	/// <param name="bestBidPrice">The best bid price in the order book.</param>
	/// <param name="bestAskPrice">The best ask price in the order book.</param>
	/// <param name="lastTradePrice">The price of the last trade.</param>
	/// <param name="bids">Array of bid quotes from the order book.</param>
	/// <param name="asks">Array of ask quotes from the order book.</param>
	/// <returns>The calculated best price for quoting, or null if unavailable.</returns>
	decimal? CalculateBestPrice(
		Security security,
		IMarketDataProvider provider,
		Sides quotingDirection,
		decimal? bestBidPrice,
		decimal? bestAskPrice,
		decimal? lastTradePrice,
		QuoteChange[] bids,
		QuoteChange[] asks);

	/// <summary>
	/// Determines if quoting is required based on the current order state and market conditions.
	/// </summary>
	/// <param name="security"><see cref="Security"/></param>
	/// <param name="provider"><see cref="IMarketDataProvider"/></param>
	/// <param name="currentTime">The current time.</param>
	/// <param name="currentPrice">The current price of the order, or null if not registered.</param>
	/// <param name="currentVolume">The current volume of the order, or null if not registered.</param>
	/// <param name="newVolume">The new volume to be quoted.</param>
	/// <param name="bestPrice">The calculated best price for quoting.</param>
	/// <returns>The price to quote at, or null if quoting is not needed.</returns>
	decimal? NeedQuoting(
		Security security,
		IMarketDataProvider provider,
		DateTimeOffset currentTime,
		decimal? currentPrice,
		decimal? currentVolume,
		decimal newVolume,
		decimal? bestPrice);
}

/// <summary>
/// Quoting behavior based on market price with configurable offset and type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MarketQuotingBehavior"/> class.
/// </remarks>
/// <param name="priceOffset">The price offset from the best quote.</param>
/// <param name="bestPriceOffset">The minimum deviation triggering order adjustment.</param>
/// <param name="priceType">The type of market price to use.</param>
public class MarketQuotingBehavior(Unit priceOffset, Unit bestPriceOffset, MarketPriceTypes priceType = MarketPriceTypes.Following) : IQuotingBehavior
{
	private readonly Unit _priceOffset = priceOffset ?? throw new ArgumentNullException(nameof(priceOffset));
	private readonly Unit _bestPriceOffset = bestPriceOffset ?? throw new ArgumentNullException(nameof(bestPriceOffset));

	decimal? IQuotingBehavior.CalculateBestPrice(Security security, IMarketDataProvider provider, Sides quotingDirection, decimal? bestBidPrice, decimal? bestAskPrice,
		decimal? lastTradePrice, QuoteChange[] bids, QuoteChange[] asks)
	{
		decimal? basePrice;

		switch (priceType)
		{
			case MarketPriceTypes.Following:
				basePrice = quotingDirection == Sides.Buy ? bestBidPrice : bestAskPrice;
				break;
			case MarketPriceTypes.Opposite:
				basePrice = quotingDirection == Sides.Buy ? bestAskPrice : bestBidPrice;
				break;
			case MarketPriceTypes.Middle:
				if (bestBidPrice != null && bestAskPrice != null)
					basePrice = bestBidPrice + (bestAskPrice - bestBidPrice) / 2m;
				else
					basePrice = null;
				break;
			default:
				throw new InvalidOperationException(priceType.ToString());
		}

		basePrice ??= lastTradePrice;

		if (basePrice == null)
			return null;

		// Apply price offset based on direction
		return quotingDirection == Sides.Buy
			? basePrice + (decimal)_priceOffset
			: basePrice - (decimal)_priceOffset;
	}

	decimal? IQuotingBehavior.NeedQuoting(Security security, IMarketDataProvider provider, DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume, decimal? bestPrice)
	{
		if (bestPrice == null)
			return null;

		if (currentPrice == null)
			return bestPrice;

		var diff = Math.Abs(currentPrice.Value - bestPrice.Value);
		if (diff >= _bestPriceOffset || currentVolume != newVolume)
			return bestPrice;

		return null;
	}
}

/// <summary>
/// Quoting behavior based on the best price with a configurable offset.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BestByPriceQuotingBehavior"/> class.
/// </remarks>
/// <param name="bestPriceOffset">The minimum deviation triggering order adjustment.</param>
public class BestByPriceQuotingBehavior(Unit bestPriceOffset) : IQuotingBehavior
{
	decimal? IQuotingBehavior.CalculateBestPrice(Security security, IMarketDataProvider provider, Sides quotingDirection, decimal? bestBidPrice, decimal? bestAskPrice,
		decimal? lastTradePrice, QuoteChange[] bids, QuoteChange[] asks)
	{
		// Use the best price based on direction
		return (quotingDirection == Sides.Buy ? bestBidPrice : bestAskPrice) ?? lastTradePrice;
	}

	decimal? IQuotingBehavior.NeedQuoting(Security security, IMarketDataProvider provider, DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume, decimal? bestPrice)
	{
		if (bestPrice == null)
			return null;

		if (currentPrice == null)
			return bestPrice;

		var diff = Math.Abs(currentPrice.Value - bestPrice.Value);
		if (diff >= bestPriceOffset || currentVolume != newVolume)
			return bestPrice;

		return null;
	}
}

/// <summary>
/// Quoting behavior based on a fixed limit price.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LimitQuotingBehavior"/> class.
/// </remarks>
/// <param name="limitPrice">The fixed price for quoting.</param>
public class LimitQuotingBehavior(decimal limitPrice) : IQuotingBehavior
{
	decimal? IQuotingBehavior.CalculateBestPrice(Security security, IMarketDataProvider provider, Sides quotingDirection, decimal? bestBidPrice, decimal? bestAskPrice,
		decimal? lastTradePrice, QuoteChange[] bids, QuoteChange[] asks)
	{
		// Always return the fixed limit price
		return limitPrice;
	}

	decimal? IQuotingBehavior.NeedQuoting(Security security, IMarketDataProvider provider, DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume, decimal? bestPrice)
	{
		if (bestPrice == null)
			return null;

		if (currentPrice == null || currentPrice != bestPrice || currentVolume != newVolume)
			return bestPrice;

		return null;
	}
}

/// <summary>
/// Quoting behavior based on the best price by volume, allowing a specified volume delta ahead of the quoted order.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BestByVolumeQuotingBehavior"/> class.
/// </remarks>
/// <param name="volumeExchange">The volume delta that can stand in front of the quoted order.</param>
public class BestByVolumeQuotingBehavior(Unit volumeExchange) : IQuotingBehavior
{
	private readonly Unit _volumeExchange = volumeExchange ?? new Unit();

	decimal? IQuotingBehavior.CalculateBestPrice(Security security, IMarketDataProvider provider, Sides quotingDirection, decimal? bestBidPrice, decimal? bestAskPrice,
		decimal? lastTradePrice, QuoteChange[] bids, QuoteChange[] asks)
	{
		var quotes = quotingDirection == Sides.Buy ? bids : asks;

		if (quotes.Length == 0)
			return lastTradePrice;

		var volume = 0m;

		foreach (var quote in quotes)
		{
			volume += quote.Volume;

			if (volume > _volumeExchange)
				return quote.Price;
		}

		return quotes.Last().Price; // Default to the last quote if volume threshold not exceeded
	}

	decimal? IQuotingBehavior.NeedQuoting(Security security, IMarketDataProvider provider, DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume, decimal? bestPrice)
	{
		if (bestPrice == null)
			return null;

		if (currentPrice == null)
			return bestPrice;

		if (currentPrice != bestPrice || currentVolume != newVolume)
			return bestPrice;

		return null;
	}
}

/// <summary>
/// Quoting behavior based on a specified level in the order book.
/// </summary>
public class LevelQuotingBehavior : IQuotingBehavior
{
	private readonly Range<int> _level;
	private readonly bool _ownLevel;

	/// <summary>
	/// Initializes a new instance of the <see cref="LevelQuotingBehavior"/> class.
	/// </summary>
	/// <param name="level">The range of levels in the order book (min and max depth from the best quote).</param>
	/// <param name="ownLevel">Whether to create a custom price level if the desired quote is not present.</param>
	public LevelQuotingBehavior(Range<int> level, bool ownLevel)
	{
		if (level == null)
			throw new ArgumentNullException(nameof(level));

		_level = level ?? throw new ArgumentNullException(nameof(level));
		_ownLevel = ownLevel;
	}

	decimal? IQuotingBehavior.CalculateBestPrice(Security security, IMarketDataProvider provider, Sides quotingDirection, decimal? bestBidPrice, decimal? bestAskPrice,
		decimal? lastTradePrice, QuoteChange[] bids, QuoteChange[] asks)
	{
		var quotes = quotingDirection == Sides.Buy ? bids : asks;

		if (quotes.Length == 0)
			return lastTradePrice;

		if (quotes.ElementAtOr(_level.Min) is not QuoteChange minQuote)
			return null;

		var fromPrice = minQuote.Price;
		var pip = security.PriceStep ?? 0.01m; // Default to 0.01 if PriceStep is null

		decimal toPrice;
		if (quotes.ElementAtOr(_level.Max) is not QuoteChange maxQuote)
		{
			if (_ownLevel)
			{
				toPrice = fromPrice + (quotingDirection == Sides.Sell ? 1 : -1) * _level.Length * pip;
			}
			else
			{
				toPrice = quotes.Last().Price;
			}
		}
		else
		{
			toPrice = maxQuote.Price;
		}

		return ((fromPrice + toPrice) / 2m).Round(pip, null); // Return the midpoint between min and max levels
	}

	decimal? IQuotingBehavior.NeedQuoting(Security security, IMarketDataProvider provider, DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume, decimal? bestPrice)
	{
		if (bestPrice == null)
			return null;

		if (currentPrice == null)
			return bestPrice;

		// For simplicity, assume quoting is needed if price or volume differs
		if (currentPrice != bestPrice || currentVolume != newVolume)
			return bestPrice;

		return null;
	}
}

/// <summary>
/// Quoting behavior based on the last trade price with a configurable offset.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LastTradeQuotingBehavior"/> class.
/// </remarks>
/// <param name="bestPriceOffset">The minimum deviation from the last trade price that triggers order adjustment.</param>
public class LastTradeQuotingBehavior(Unit bestPriceOffset) : IQuotingBehavior
{
	private readonly Unit _bestPriceOffset = bestPriceOffset ?? new Unit();

	decimal? IQuotingBehavior.CalculateBestPrice(Security security, IMarketDataProvider provider, Sides quotingDirection, decimal? bestBidPrice, decimal? bestAskPrice,
		decimal? lastTradePrice, QuoteChange[] bids, QuoteChange[] asks)
	{
		// Always use the last trade price, regardless of useLastTradePrice flag
		return lastTradePrice;
	}

	decimal? IQuotingBehavior.NeedQuoting(Security security, IMarketDataProvider provider, DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume, decimal? bestPrice)
	{
		if (bestPrice == null)
			return null;

		if (currentPrice == null)
			return bestPrice;

		var diff = Math.Abs(currentPrice.Value - bestPrice.Value);
		if (diff >= _bestPriceOffset || currentVolume != newVolume)
			return bestPrice;

		return null;
	}
}

/// <summary>
/// Quoting behavior for options based on theoretical price with an offset range.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TheorPriceQuotingBehavior"/> class.
/// </remarks>
/// <param name="theorPriceOffset">Theoretical price offset range.</param>
public class TheorPriceQuotingBehavior(Range<Unit> theorPriceOffset) : IQuotingBehavior
{
	private readonly Range<Unit> _theorPriceOffset = theorPriceOffset ?? throw new ArgumentNullException(nameof(theorPriceOffset));

	decimal? IQuotingBehavior.CalculateBestPrice(Security security, IMarketDataProvider provider, Sides quotingDirection, decimal? bestBidPrice, decimal? bestAskPrice,
		decimal? lastTradePrice, QuoteChange[] bids, QuoteChange[] asks)
	{
		// Use the best price from the order book, as in BestByPriceQuotingStrategy
		return (quotingDirection == Sides.Buy ? bestBidPrice : bestAskPrice) ?? lastTradePrice;
	}

	decimal? IQuotingBehavior.NeedQuoting(Security security, IMarketDataProvider provider, DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume, decimal? bestPrice)
	{
		var tp = provider.GetSecurityValue<decimal?>(security, Level1Fields.TheorPrice);
		if (tp == null)
			return null;

		var minOffset = (decimal)_theorPriceOffset.Min;
		var maxOffset = (decimal)_theorPriceOffset.Max;

		if (currentPrice == null || currentPrice < (tp.Value + minOffset) || currentPrice > (tp.Value + maxOffset))
			return tp.Value + (decimal)(minOffset + _theorPriceOffset.Length / 2);

		if (currentVolume != newVolume)
			return currentPrice;

		return null;
	}
}

/// <summary>
/// Quoting behavior for options based on volatility range using the Black-Scholes model.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VolatilityQuotingBehavior"/> class.
/// </remarks>
/// <param name="ivRange">Volatility range (in percentage).</param>
/// <param name="model">Black-Scholes model for option pricing.</param>
public class VolatilityQuotingBehavior(Range<decimal> ivRange, IBlackScholes model) : IQuotingBehavior
{
	private readonly Range<decimal> _ivRange = ivRange ?? throw new ArgumentNullException(nameof(ivRange));
	private readonly IBlackScholes _model = model ?? throw new ArgumentNullException(nameof(model));

	decimal? IQuotingBehavior.CalculateBestPrice(Security security, IMarketDataProvider provider, Sides quotingDirection, decimal? bestBidPrice, decimal? bestAskPrice,
		decimal? lastTradePrice, QuoteChange[] bids, QuoteChange[] asks)
	{
		// Use the best price from the order book, as in BestByPriceQuotingStrategy
		return (quotingDirection == Sides.Buy ? bestBidPrice : bestAskPrice) ?? lastTradePrice;
	}

	decimal? IQuotingBehavior.NeedQuoting(Security security, IMarketDataProvider provider, DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume, decimal? bestPrice)
	{
		var minPrice = _model.Premium(currentTime, _ivRange.Min / 100);
		if (minPrice == null)
			return null;

		var maxPrice = _model.Premium(currentTime, _ivRange.Max / 100);
		if (maxPrice == null)
			return null;

		if (currentPrice == null || currentPrice < minPrice || currentPrice > maxPrice)
			return (minPrice + maxPrice) / 2;

		if (currentVolume != newVolume)
			return currentPrice;

		return null;
	}
}