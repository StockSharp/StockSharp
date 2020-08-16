namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Messages;

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
	public abstract class BaseCandleBuilderValueTransform : ICandleBuilderValueTransform
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseCandleBuilderValueTransform"/>.
		/// </summary>
		/// <param name="buildFrom">Which market-data type is used as a source value.</param>
		protected BaseCandleBuilderValueTransform(DataType buildFrom)
		{
			_buildFrom = buildFrom;
		}

		private readonly DataType _buildFrom;

		DataType ICandleBuilderValueTransform.BuildFrom => _buildFrom;

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
			if (!(message is ExecutionMessage tick) || tick.ExecutionType != ExecutionTypes.Tick)
				return base.Process(message);

			Update(tick.ServerTime, tick.TradePrice.Value, tick.TradeVolume, tick.OriginSide, tick.OpenInterest, null);

			return true;
		}
	}

	/// <summary>
	/// The order book based data source transformation for <see cref="ICandleBuilder"/>.
	/// </summary>
	public class QuoteCandleBuilderValueTransform : BaseCandleBuilderValueTransform
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QuoteCandleBuilderValueTransform"/>.
		/// </summary>
		public QuoteCandleBuilderValueTransform()
			: base(DataType.MarketDepth)
		{
		}

		/// <summary>
		/// Type of candle based data.
		/// </summary>
		public Level1Fields Type { get; set; } = Level1Fields.SpreadMiddle;

		/// <inheritdoc />
		public override bool Process(Message message)
		{
			if (!(message is QuoteChangeMessage md))
				return base.Process(message);

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
					var price = md.GetSpreadMiddle();

					if (price == null)
						return false;

					Update(md.ServerTime, price.Value, null, null, null, null);
					return true;
				}

				//default:
				//	throw new ArgumentOutOfRangeException(nameof(Type), Type, LocalizedStrings.Str1219);
			}
		}
	}

	/// <summary>
	/// The level1 based data source transformation for <see cref="ICandleBuilder"/>.
	/// </summary>
	public class Level1CandleBuilderValueTransform : BaseCandleBuilderValueTransform
	{
		private decimal? _prevBestBid;
		private decimal? _prevBestAsk;

		/// <summary>
		/// Initializes a new instance of the <see cref="Level1CandleBuilderValueTransform"/>.
		/// </summary>
		public Level1CandleBuilderValueTransform()
			: base(DataType.Level1)
		{
		}

		/// <summary>
		/// Type of candle based data.
		/// </summary>
		public Level1Fields Type { get; set; } = Level1Fields.LastTradePrice;

		/// <inheritdoc />
		public override bool Process(Message message)
		{
			if (!(message is Level1ChangeMessage l1))
			{
				if (message.Type == MessageTypes.Reset)
				{
					_prevBestBid = _prevBestAsk = null;
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

					_prevBestBid = currBidPrice ?? _prevBestBid;
					_prevBestAsk = currAskPrice ?? _prevBestAsk;

					var spreadMiddle = l1.TryGetDecimal(Level1Fields.SpreadMiddle);

					if (spreadMiddle == null)
					{
						if (currBidPrice == null && currAskPrice == null)
							return false;

						if (_prevBestBid == null || _prevBestAsk == null)
							return false;

						spreadMiddle = _prevBestBid.Value.GetSpreadMiddle(_prevBestAsk.Value);
					}

					Update(time, spreadMiddle.Value, null, null, null, null);
					return true;
				}

				//default:
				//	throw new ArgumentOutOfRangeException(nameof(Type), Type, LocalizedStrings.Str1219);
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
			if (!(message is ExecutionMessage ol) || ol.ExecutionType != ExecutionTypes.OrderLog)
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
				//	throw new ArgumentOutOfRangeException(nameof(Type), Type, LocalizedStrings.Str1219);	
			}
		}
	}
}