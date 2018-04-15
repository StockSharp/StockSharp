namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Messages;

	/// <summary>
	/// Compressor of candles from smaller time-frames to bigger.
	/// </summary>
	public class BiggerTimeFrameCandleCompressor
	{
		private class PartCandleBuilderValueTransform : BaseCandleBuilderValueTransform
		{
			public PartCandleBuilderValueTransform()
				: base(MarketDataTypes.Trades)
			{
			}

			public Level1Fields Part { get; set; }

			public override bool Process(Message message)
			{
				if (!(message is CandleMessage candle))
					return base.Process(message);

				decimal price;
				decimal? volume = null;
				decimal? oi = null;

				switch (Part)
				{
					case Level1Fields.OpenPrice:
						price = candle.OpenPrice;
						volume = candle.TotalVolume;
						oi = candle.OpenInterest;
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
						throw new ArgumentOutOfRangeException();
				}

				Update(candle.OpenTime, price, volume, null, oi);

				return true;
			}
		}

		private readonly PartCandleBuilderValueTransform _transform;
		private readonly TimeFrameCandleBuilder _builder;
		private readonly MarketDataMessage _mdMsg;
		private CandleMessage _currentCandle;

		/// <summary>
		/// Initializes a new instance of the <see cref="BiggerTimeFrameCandleCompressor"/>.
		/// </summary>
		/// <param name="mdMsg">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		public BiggerTimeFrameCandleCompressor(MarketDataMessage mdMsg)
		{
			if (mdMsg == null)
				throw new ArgumentNullException(nameof(mdMsg));

			_mdMsg = mdMsg;
			_transform = new PartCandleBuilderValueTransform();
			_builder = new TimeFrameCandleBuilder();
		}

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="message">The message contains information about the time-frame candle.</param>
		/// <returns>A new candles changes.</returns>
		public IEnumerable<CandleMessage> Process(CandleMessage message)
		{
			foreach (var builtCandle in ProcessCandlePart(Level1Fields.OpenPrice, message))
				yield return (TimeFrameCandleMessage)builtCandle;

			foreach (var builtCandle in ProcessCandlePart(Level1Fields.HighPrice, message))
				yield return (TimeFrameCandleMessage)builtCandle;

			foreach (var builtCandle in ProcessCandlePart(Level1Fields.LowPrice, message))
				yield return (TimeFrameCandleMessage)builtCandle;

			foreach (var builtCandle in ProcessCandlePart(Level1Fields.ClosePrice, message))
				yield return (TimeFrameCandleMessage)builtCandle;
		}

		private IEnumerable<CandleMessage> ProcessCandlePart(Level1Fields part, CandleMessage message)
		{
			_transform.Part = part;
			_transform.Process(message);

			foreach (var builtCandle in _builder.Process(_mdMsg, _currentCandle, _transform))
			{
				_currentCandle = builtCandle;
				yield return builtCandle;
			}
		}
	}
}