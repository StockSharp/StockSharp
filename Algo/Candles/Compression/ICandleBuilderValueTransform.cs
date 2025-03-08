namespace StockSharp.Algo.Candles.Compression;

/// <summary>
/// The interface that describes data transformation of the <see cref="ICandleBuilder"/> source.
/// </summary>
public interface ICandleBuilderValueTransform
{
	/// <summary>
	/// Which market-data type is used as a source value.
	/// </summary>
	DataType BuildFrom { get; }

	/// <summary>
	/// Process message to update current state.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns><see langword="true" />, if the message was processed, otherwise, <see langword="false" />.</returns>
	bool Process(Message message);

	/// <summary>
	/// The time of new data occurrence.
	/// </summary>
	DateTimeOffset Time { get; }

	/// <summary>
	/// Price.
	/// </summary>
	decimal Price { get; }

	/// <summary>
	/// Volume.
	/// </summary>
	decimal? Volume { get; }

	/// <summary>
	/// Side.
	/// </summary>
	Sides? Side { get; }

	/// <summary>
	/// Open interest.
	/// </summary>
	decimal? OpenInterest { get; }

	/// <summary>
	/// Price levels.
	/// </summary>
	IEnumerable<CandlePriceLevel> PriceLevels { get; }
}

/// <summary>
/// The base data source transformation for <see cref="ICandleBuilder"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BaseCandleBuilderValueTransform"/>.
/// </remarks>
/// <param name="buildFrom">Which market-data type is used as a source value.</param>
public abstract class BaseCandleBuilderValueTransform(DataType buildFrom) : ICandleBuilderValueTransform
{
	DataType ICandleBuilderValueTransform.BuildFrom => buildFrom;

	/// <inheritdoc />
	public virtual bool Process(Message message)
	{
		if (message is ResetMessage)
		{
			_time = default;
			_price = 0;
			_volume = null;
			_side = null;
		}

		return false;
	}

	/// <summary>
	/// Update latest values.
	/// </summary>
	/// <param name="time">Time.</param>
	/// <param name="price">Price.</param>
	/// <param name="volume">Volume.</param>
	/// <param name="side">Side.</param>
	/// <param name="openInterest">Open interest.</param>
	/// <param name="priceLevels">Price levels.</param>
	protected void Update(DateTimeOffset time, decimal price, decimal? volume, Sides? side, decimal? openInterest, IEnumerable<CandlePriceLevel> priceLevels)
	{
		_time = time;
		_price = price;
		_volume = volume;
		_side = side;
		_openInterest = openInterest;
		_priceLevels = priceLevels;
	}

	private DateTimeOffset _time;
	
	DateTimeOffset ICandleBuilderValueTransform.Time => _time;

	private decimal _price;

	decimal ICandleBuilderValueTransform.Price => _price;

	private decimal? _volume;

	decimal? ICandleBuilderValueTransform.Volume => _volume;

	private Sides? _side;

	Sides? ICandleBuilderValueTransform.Side => _side;

	private decimal? _openInterest;

	decimal? ICandleBuilderValueTransform.OpenInterest => _openInterest;

	private IEnumerable<CandlePriceLevel> _priceLevels;

	IEnumerable<CandlePriceLevel> ICandleBuilderValueTransform.PriceLevels => _priceLevels;
}

/// <summary>
/// The tick based data source transformation for <see cref="ICandleBuilder"/>.
/// </summary>
public class TickCandleBuilderValueTransform : BaseCandleBuilderValueTransform
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TickCandleBuilderValueTransform"/>.
	/// </summary>
	public TickCandleBuilderValueTransform()
		: base(DataType.Ticks)
	{
	}

	/// <inheritdoc />
	public override bool Process(Message message)
	{
		if (message is not ExecutionMessage tick || tick.DataType != DataType.Ticks)
			return base.Process(message);

		Update(tick.ServerTime, tick.TradePrice.Value, tick.TradeVolume, tick.OriginSide, tick.OpenInterest, null);

		return true;
	}
}

/// <summary>
/// The order book based data source transformation for <see cref="ICandleBuilder"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QuoteCandleBuilderValueTransform"/>.
/// </remarks>
/// <param name="priceStep"><see cref="SecurityMessage.PriceStep"/></param>
/// <param name="volStep"><see cref="SecurityMessage.VolumeStep"/></param>
public class QuoteCandleBuilderValueTransform(decimal? priceStep, decimal? volStep) : BaseCandleBuilderValueTransform(DataType.MarketDepth)
{
	private decimal? _prevBidVol;
	private decimal? _prevAskVol;

	/// <summary>
	/// Type of candle based data.
	/// </summary>
	public Level1Fields Type { get; set; } = Level1Fields.SpreadMiddle;

	/// <inheritdoc />
	public override bool Process(Message message)
	{
		if (message is not QuoteChangeMessage md)
		{
			if (message.Type == MessageTypes.Reset)
			{
				_prevBidVol = _prevAskVol = null;
			}

			return base.Process(message);
		}

		switch (Type)
		{
			case Level1Fields.BestBidPrice:
			{
				var quote = md.GetBestBid();

				if (quote == null)
					return false;

				Update(md.ServerTime, quote.Value.Price, quote.Value.Volume, Sides.Buy, null, null);
				return true;
			}

			case Level1Fields.BestAskPrice:
			{
				var quote = md.GetBestAsk();

				if (quote == null)
					return false;

				Update(md.ServerTime, quote.Value.Price, quote.Value.Volume, Sides.Sell, null, null);
				return true;
			}

			//case Level1Fields.SpreadMiddle:
			default:
			{
				var bestBid = md.GetBestBid();
				var bestAsk = md.GetBestAsk();

				var price = (bestBid?.Price).GetSpreadMiddle(bestAsk?.Price, priceStep);

				if (price is null)
					return false;

				_prevBidVol = bestBid?.Volume ?? _prevBidVol;
				_prevAskVol = bestAsk?.Volume ?? _prevAskVol;

				decimal? spreadVol = null;

				if (_prevBidVol is not null && _prevAskVol is not null)
				{
					spreadVol = _prevBidVol.Value.GetSpreadMiddle(_prevAskVol.Value, volStep);
				}

				Update(md.ServerTime, price.Value, spreadVol, null, null, null);
				return true;
			}

			//default:
			//	throw new ArgumentOutOfRangeException(nameof(Type), Type, LocalizedStrings.InvalidValue);
		}
	}
}

/// <summary>
/// The level1 based data source transformation for <see cref="ICandleBuilder"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Level1CandleBuilderValueTransform"/>.
/// </remarks>
/// <param name="priceStep"><see cref="SecurityMessage.PriceStep"/></param>
/// <param name="volStep"><see cref="SecurityMessage.VolumeStep"/></param>
public class Level1CandleBuilderValueTransform(decimal? priceStep, decimal? volStep) : BaseCandleBuilderValueTransform(DataType.Level1)
{
	private decimal? _prevBidPrice;
	private decimal? _prevAskPrice;
	private decimal? _prevBidVol;
	private decimal? _prevAskVol;

	/// <summary>
	/// Type of candle based data.
	/// </summary>
	public Level1Fields Type { get; set; } = Level1Fields.LastTradePrice;

	/// <inheritdoc />
	public override bool Process(Message message)
	{
		if (message is not Level1ChangeMessage l1)
		{
			if (message.Type == MessageTypes.Reset)
			{
				_prevBidPrice = _prevAskPrice = null;
				_prevBidVol = _prevAskVol = null;
			}

			return base.Process(message);
		}

		var time = l1.ServerTime;

		switch (Type)
		{
			case Level1Fields.BestBidPrice:
			{
				var price = l1.TryGetDecimal(Type);

				if (price == null)
					return false;

				Update(time, price.Value, l1.TryGetDecimal(Level1Fields.BestBidVolume), Sides.Buy, null, null);
				return true;
			}
			case Level1Fields.BestAskPrice:
			{
				var price = l1.TryGetDecimal(Type);

				if (price == null)
					return false;

				Update(time, price.Value, l1.TryGetDecimal(Level1Fields.BestAskVolume), Sides.Sell, null, null);
				return true;
			}
			case Level1Fields.LastTradePrice:
			{
				var price = l1.GetLastTradePrice();

				if (price == null)
					return false;

				Update(time, price.Value,
					l1.TryGetDecimal(Level1Fields.LastTradeVolume),
					(Sides?)l1.TryGet(Level1Fields.LastTradeOrigin),
					l1.TryGetDecimal(Level1Fields.OpenInterest),
					null);

				return true;
			}

			//case Level1Fields.SpreadMiddle:
			default:
			{
				var currBidPrice = l1.TryGetDecimal(Level1Fields.BestBidPrice);
				var currAskPrice = l1.TryGetDecimal(Level1Fields.BestAskPrice);

				_prevBidPrice = currBidPrice ?? _prevBidPrice;
				_prevAskPrice = currAskPrice ?? _prevAskPrice;

				var spreadMiddle = l1.TryGetDecimal(Level1Fields.SpreadMiddle);

				if (spreadMiddle is null)
				{
					if (currBidPrice is null && currAskPrice is null)
						return false;

					if (_prevBidPrice is null || _prevAskPrice is null)
						return false;

					spreadMiddle = _prevBidPrice.Value.GetSpreadMiddle(_prevAskPrice.Value, priceStep);
				}

				_prevBidVol = l1.TryGetDecimal(Level1Fields.BestBidVolume) ?? _prevBidVol;
				_prevAskVol = l1.TryGetDecimal(Level1Fields.BestAskVolume) ?? _prevAskVol;

				decimal? spreadVol = null;

				if (_prevBidVol is not null && _prevAskVol is not null)
				{
					spreadVol = _prevBidVol.Value.GetSpreadMiddle(_prevAskVol.Value, volStep);
				}

				Update(time, spreadMiddle.Value, spreadVol, null, null, null);
				return true;
			}

			//default:
			//	throw new ArgumentOutOfRangeException(nameof(Type), Type, LocalizedStrings.InvalidValue);
		}
	}
}

/// <summary>
/// The order log based data source transformation for <see cref="ICandleBuilder"/>.
/// </summary>
public class OrderLogCandleBuilderValueTransform : BaseCandleBuilderValueTransform
{
	/// <summary>
	/// Initializes a new instance of the <see cref="OrderLogCandleBuilderValueTransform"/>.
	/// </summary>
	public OrderLogCandleBuilderValueTransform()
		: base(DataType.OrderLog)
	{
	}

	/// <summary>
	/// Type of candle based data.
	/// </summary>
	public Level1Fields Type { get; set; } = Level1Fields.LastTradePrice;

	/// <inheritdoc />
	public override bool Process(Message message)
	{
		if (message is not ExecutionMessage ol || ol.DataType != DataType.OrderLog)
			return base.Process(message);

		switch (Type)
		{
			case Level1Fields.PriceBook:
			{
				Update(ol.ServerTime, ol.OrderPrice, ol.OrderVolume, ol.Side, ol.OpenInterest, null);
				return true;
			}

			//case Level1Fields.LastTradePrice:
			default:
			{
				var price = ol.TradePrice;

				if (price == null)
					return false;

				Update(ol.ServerTime, price.Value, ol.TradeVolume, ol.OriginSide, ol.OpenInterest, null);
				return true;
			}

			//default:
			//	throw new ArgumentOutOfRangeException(nameof(Type), Type, LocalizedStrings.InvalidValue);	
		}
	}
}