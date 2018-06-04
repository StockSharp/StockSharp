namespace StockSharp.Algo.Candles.Compression
{
	using System;

	using Ecng.Collections;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The interface that describes data transformation of the <see cref="ICandleBuilder"/> source.
	/// </summary>
	public interface ICandleBuilderValueTransform
	{
		/// <summary>
		/// Which market-data type is used as a source value.
		/// </summary>
		MarketDataTypes BuildFrom { get; }

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
		protected BaseCandleBuilderValueTransform(MarketDataTypes buildFrom)
		{
			_buildFrom = buildFrom;
		}

		private readonly MarketDataTypes _buildFrom;

		MarketDataTypes ICandleBuilderValueTransform.BuildFrom => _buildFrom;

		/// <inheritdoc />
		public virtual bool Process(Message message)
		{
			if (message is ResetMessage)
			{
				_time = default(DateTimeOffset);
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
		protected void Update(DateTimeOffset time, decimal price, decimal? volume, Sides? side, decimal? openInterest)
		{
			_time = time;
			_price = price;
			_volume = volume;
			_side = side;
			_openInterest = openInterest;
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
			: base(MarketDataTypes.Trades)
		{
		}

		/// <inheritdoc />
		public override bool Process(Message message)
		{
			if (!(message is ExecutionMessage tick) || tick.ExecutionType != ExecutionTypes.Tick)
				return base.Process(message);

			Update(tick.ServerTime, tick.TradePrice.Value, tick.TradeVolume, tick.OriginSide, tick.OpenInterest);

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
			: base(MarketDataTypes.MarketDepth)
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

					Update(md.ServerTime, quote.Price, quote.Volume, quote.Side, null);
					return true;
				}

				case Level1Fields.BestAskPrice:
				{
					var quote = md.GetBestAsk();

					if (quote == null)
						return false;

					Update(md.ServerTime, quote.Price, quote.Volume, quote.Side, null);
					return true;
				}

				case Level1Fields.SpreadMiddle:
				{
					var price = md.GetSpreadMiddle();

					if (price == null)
						return false;

					Update(md.ServerTime, price.Value, null, null, null);
					return true;
				}

				default:
					throw new ArgumentOutOfRangeException(nameof(Type), Type, LocalizedStrings.Str1219);
			}
		}
	}

	/// <summary>
	/// The level1 based data source transformation for <see cref="ICandleBuilder"/>.
	/// </summary>
	public class Level1CandleBuilderValueTransform : BaseCandleBuilderValueTransform
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Level1CandleBuilderValueTransform"/>.
		/// </summary>
		public Level1CandleBuilderValueTransform()
			: base(MarketDataTypes.Level1)
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
				return base.Process(message);

			switch (Type)
			{
				case Level1Fields.BestBidPrice:
				{
					var price = (decimal?)l1.Changes.TryGetValue(Type);

					if (price == null)
						return false;

					Update(l1.ServerTime, price.Value, (decimal?)l1.Changes.TryGetValue(Level1Fields.BestBidVolume), Sides.Buy, null);
					return true;
				}
				case Level1Fields.BestAskPrice:
				{
					var price = (decimal?)l1.Changes.TryGetValue(Type);

					if (price == null)
						return false;

					Update(l1.ServerTime, price.Value, (decimal?)l1.Changes.TryGetValue(Level1Fields.BestAskVolume), Sides.Sell, null);
					return true;
				}
				case Level1Fields.LastTradePrice:
				{
					var price = l1.GetLastTradePrice();

					if (price == null)
						return false;

					Update(l1.ServerTime, price.Value,
						(decimal?)l1.Changes.TryGetValue(Level1Fields.LastTradeVolume),
						(Sides?)l1.Changes.TryGetValue(Level1Fields.LastTradeOrigin),
						(decimal?)l1.Changes.TryGetValue(Level1Fields.OpenInterest));
					return true;
				}

				case Level1Fields.SpreadMiddle:
				{
					var price = l1.GetSpreadMiddle();
					if (price == null)
						return false;

					Update(l1.ServerTime, price.Value, null, null, null);
					return true;
				}

				default:
					throw new ArgumentOutOfRangeException(nameof(Type), Type, LocalizedStrings.Str1219);
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
			: base(MarketDataTypes.OrderLog)
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
					Update(ol.ServerTime, ol.OrderPrice, ol.OrderVolume, ol.Side, ol.OpenInterest);
					return true;
				}
				case Level1Fields.LastTradePrice:
				{
					var price = ol.TradePrice;

					if (price == null)
						return false;

					Update(ol.ServerTime, price.Value, ol.TradeVolume, ol.OriginSide, ol.OpenInterest);
					return true;
				}

				default:
					throw new ArgumentOutOfRangeException(nameof(Type), Type, LocalizedStrings.Str1219);	
			}
		}
	}
}