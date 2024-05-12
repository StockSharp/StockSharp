using System;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace Parallel_testing_terminal
{
	public class StairsStrategyCountertrendStrategy : Strategy
	{
		private readonly CandleSeries _candleSeries;
		private readonly StrategyParam<int> _length;

		public StairsStrategyCountertrendStrategy(CandleSeries candleSeries)
		{
			_candleSeries = candleSeries;
			_length = this.Param(nameof(Length), 3);
		}

		private int _bullLength;
		private int _bearLength;

		public int Length
		{
			get => _length.Value;
			set => _length.Value = value;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			Connector.CandleProcessing += CandleManager_Processing;
			this.SubscribeCandles(_candleSeries);
			base.OnStarted(time);
		}

		private void CandleManager_Processing(CandleSeries candleSeries, ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished) return;

			if (candle.OpenPrice < candle.ClosePrice)
			{
				_bullLength++;
				_bearLength = 0;
			}
			else
			if (candle.OpenPrice > candle.ClosePrice)
			{
				_bullLength = 0;
				_bearLength++;
			}

			if (_bullLength >= Length && Position >= 0)
			{
				RegisterOrder(this.SellAtMarket(Volume + Math.Abs(Position)));
			}

			else
			if (_bearLength >= Length && Position <= 0)
			{
				RegisterOrder(this.BuyAtMarket(Volume + Math.Abs(Position)));
			}
		}
	}
}