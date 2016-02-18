using System;
using System.Collections.Generic;
using System.Linq;
using Ecng.Collections;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Terminal.Services {
	class TerminalCandleBuilder {
		private readonly CachedSynchronizedList<TimeFrameCandle> _candles = new CachedSynchronizedList<TimeFrameCandle>();

		public event Action<TimeFrameCandle> Candle;

		public IEnumerable<TimeFrameCandle> AllCandles => _candles.Cache;

		readonly TimeSpan _timeframe;
		readonly Security _security;

		public TerminalCandleBuilder(Security sec, TimeSpan tf)
		{
			_security = sec;
			_timeframe = tf;
		}

		public void ProcessMessage(TimeFrameCandleMessage message)
		{
			if(message.SecurityId.ToStringId() != _security.Id || (TimeSpan)message.Arg != _timeframe)
				return;

			var candle = _candles.LastOrDefault();

			if(candle == null)
				_candles.Add(candle = NewCandleFromMessage(message));
			else if (candle.OpenTime > message.OpenTime)
				throw new InvalidOperationException("received unordered candle");
			else
			{
				if (!CheckSameCandle(candle, message))
				{
					if (candle.State != CandleStates.Finished)
					{
						candle.State = CandleStates.Finished;
						Candle?.Invoke(candle);
					}

					_candles.Add(candle = NewCandleFromMessage(message));
				}
			}

			candle.CloseTime = message.CloseTime;
			candle.OpenPrice = message.OpenPrice;
			candle.HighPrice = message.HighPrice;
			candle.LowPrice = message.LowPrice;
			candle.ClosePrice = message.ClosePrice;
			candle.TotalVolume = message.TotalVolume;

			Candle?.Invoke(candle);
		}

		private bool CheckSameCandle(TimeFrameCandle candle, TimeFrameCandleMessage msg)
		{
			return candle.Arg == msg.Arg && candle.OpenTime == msg.OpenTime;
		}

		private TimeFrameCandle NewCandleFromMessage(TimeFrameCandleMessage msg)
		{
			return new TimeFrameCandle
			{
				Security = _security,
				OpenTime = msg.OpenTime,
				CloseTime = msg.OpenTime + _timeframe,
				TimeFrame = _timeframe,
				State = CandleStates.Active
			};
		}
	}
}
