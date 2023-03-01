namespace StockSharp.Algo.Candles
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Messages;

	/// <summary>
	/// Compressor of candles from smaller time-frames to bigger.
	/// </summary>
	public class BiggerTimeFrameCandleCompressor : ICandleBuilderSubscription
	{
		private class PartCandleBuilderValueTransform : BaseCandleBuilderValueTransform
		{
			public PartCandleBuilderValueTransform()
				: base(DataType.Ticks)
			{
			}

			public Level1Fields Part { get; set; }

			public override bool Process(Message message)
			{
				if (message is not CandleMessage candle)
					return base.Process(message);

				decimal price;
				decimal? volume = null;
				decimal? oi = null;
				IEnumerable<CandlePriceLevel> priceLevels = null;

				switch (Part)
				{
					case Level1Fields.OpenPrice:
						price = candle.OpenPrice;
						volume = candle.TotalVolume;
						oi = candle.OpenInterest;
						priceLevels = candle.PriceLevels;
						break;

					case Level1Fields.HighPrice:
						price = candle.HighPrice;
						break;

					case Level1Fields.LowPrice:
						price = candle.LowPrice;
						break;

					case Level1Fields.ClosePrice:
						price = candle.ClosePrice;
						break;

					default:
						throw new InvalidOperationException(Part.To<string>());
				}

				Update(candle.OpenTime, price, volume, null, oi, priceLevels);

				return true;
			}
		}

		private readonly PartCandleBuilderValueTransform _transform;
		private readonly ICandleBuilder _builder;

		/// <summary>
		/// Initializes a new instance of the <see cref="BiggerTimeFrameCandleCompressor"/>.
		/// </summary>
		/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <param name="builder">The builder of candles of <see cref="TimeFrameCandleMessage"/> type.</param>
		public BiggerTimeFrameCandleCompressor(MarketDataMessage message, ICandleBuilder builder)
		{
			Message = message ?? throw new ArgumentNullException(nameof(message));
			_transform = new PartCandleBuilderValueTransform();
			_builder = builder ?? throw new ArgumentNullException(nameof(builder));
		}

		/// <inheritdoc />
		public MarketDataMessage Message { get; private set; }

		/// <inheritdoc />
		public VolumeProfileBuilder VolumeProfile { get; set; }

		/// <inheritdoc />
		public CandleMessage CurrentCandle { get; set; }

		/// <inheritdoc />
		public CandleMessage PrevCandle { get; set; }

		/// <summary>
		/// Reset state.
		/// </summary>
		public void Reset()
		{
			CurrentCandle = null;
			PrevCandle = null;
		}

		private static readonly Level1Fields[] _processParts =
		{
			Level1Fields.OpenPrice,
			Level1Fields.HighPrice,
			Level1Fields.LowPrice,
			Level1Fields.ClosePrice,
		};

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="message">The message contains information about the time-frame candle.</param>
		/// <returns>A new candles changes.</returns>
		public IEnumerable<CandleMessage> Process(CandleMessage message)
		{
			TimeFrameCandleMessage lastCandle = null;

			foreach (var candle in _processParts.SelectMany(p => ProcessCandlePart(p, message)).ToArray().Cast<TimeFrameCandleMessage>())
			{
				if (candle != lastCandle)
				{
					lastCandle = candle;
					yield return candle;
				}
			}
		}

		private IEnumerable<CandleMessage> ProcessCandlePart(Level1Fields part, CandleMessage message)
		{
			_transform.Part = part;
			_transform.Process(message);

			return _builder.Process(this, _transform);
		}
	}
}