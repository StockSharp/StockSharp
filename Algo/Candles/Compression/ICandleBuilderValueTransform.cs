namespace StockSharp.Algo.Candles.Compression
{
	using System;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The interface that describes data transformation of the <see cref="ICandleBuilder"/> source.
	/// </summary>
	public interface ICandleBuilderValueTransform
	{
		/// <summary>
		/// Which market-data type is used as an candle source value.
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
	}

	/// <summary>
	/// The base data source transformation for <see cref="ICandleBuilder"/>.
	/// </summary>
	public abstract class BaseCandleBuilderValueTransform : ICandleBuilderValueTransform
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseCandleBuilderValueTransform"/>.
		/// </summary>
		/// <param name="buildFrom">Which market-data type is used as an candle source value.</param>
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
		protected void Update(DateTimeOffset time, decimal price, decimal? volume, Sides? side)
		{
			_time = time;
			_price = price;
			_volume = volume;
			_side = side;
		}

		private DateTimeOffset _time;
		
		DateTimeOffset ICandleBuilderValueTransform.Time => _time;

		private decimal _price;

		decimal ICandleBuilderValueTransform.Price => _price;

		private decimal? _volume;

		decimal? ICandleBuilderValueTransform.Volume => _volume;

		private Sides? _side;

		Sides? ICandleBuilderValueTransform.Side => _side;
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

			Update(tick.ServerTime, tick.TradePrice.Value, tick.TradeVolume, tick.OriginSide);

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
		/// Type of candle depth based data.
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

					Update(md.ServerTime, quote.Price, quote.Volume, quote.Side);
					return true;
				}

				case Level1Fields.BestAskPrice:
				{
					var quote = md.GetBestAsk();

					if (quote == null)
						return false;

					Update(md.ServerTime, quote.Price, quote.Volume, quote.Side);
					return true;
				}

				case Level1Fields.SpreadMiddle:
				{
					var bid = md.GetBestBid();
					var ask = md.GetBestAsk();

					if (bid == null || ask == null)
						return false;

					Update(md.ServerTime, (ask.Price + bid.Price) / 2, null, null);
					return true;
				}

				default:
					throw new ArgumentOutOfRangeException();
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
		/// Type of candle depth based data.
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

					Update(l1.ServerTime, price.Value, (decimal?)l1.Changes.TryGetValue(Level1Fields.BestBidVolume), Sides.Buy);
					return true;
				}
				case Level1Fields.BestAskPrice:
				{
					var price = (decimal?)l1.Changes.TryGetValue(Type);

					if (price == null)
						return false;

					Update(l1.ServerTime, price.Value, (decimal?)l1.Changes.TryGetValue(Level1Fields.BestAskVolume), Sides.Sell);
					return true;
				}
				case Level1Fields.LastTradePrice:
				{
					var price = (decimal?)l1.Changes.TryGetValue(Type);

					if (price == null)
						return false;

					Update(l1.ServerTime, price.Value, (decimal?)l1.Changes.TryGetValue(Level1Fields.LastTradeVolume), (Sides?)l1.Changes.TryGetValue(Level1Fields.LastTradeOrigin));
					return true;
				}

				case Level1Fields.SpreadMiddle:
				{
					var bid = (decimal?)l1.Changes.TryGetValue(Level1Fields.BestBidPrice);
					var ask = (decimal?)l1.Changes.TryGetValue(Level1Fields.BestAskPrice);

					if (bid == null || ask == null)
						return false;

					Update(l1.ServerTime, (ask.Value + bid.Value) / 2, null, null);
					return true;
				}

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}