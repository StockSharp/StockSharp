using System;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies.HistorySMA
{
	public class SmaStrategyClassicStrategy : Strategy
	{
		private readonly Subscription _subscription;

		public SimpleMovingAverage LongSma { get; set; }
		public SimpleMovingAverage ShortSma { get; set; }

		public SmaStrategyClassicStrategy(CandleSeries candleSeries)
		{
			_subscription = new(candleSeries);
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			this
				.WhenCandlesFinished(_subscription)
				.Do(CandleManager_Processing)
				.Apply(this);

			Subscribe(_subscription);
			base.OnStarted(time);
		}

		private void CandleManager_Processing(ICandleMessage candle)
		{
			LongSma.Process(candle);
			ShortSma.Process(candle);

			if (!IsFormedAndOnlineAndAllowTrading()) return;

			var isShortLessCurrent = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();
			var isShortLessPrev = ShortSma.GetValue(1) < LongSma.GetValue(1);

			if (isShortLessCurrent == isShortLessPrev) return;

			var volume = Volume + Math.Abs(Position);

			if (isShortLessCurrent)
				SellMarket(volume);
			else
				BuyMarket(volume);
		}
	}
}