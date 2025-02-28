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
	internal class SmaStrategyMartingaleStrategy : Strategy
	{
		private readonly Subscription _subscription;

		public SimpleMovingAverage LongSma { get; set; }
		public SimpleMovingAverage ShortSma { get; set; }

		public SmaStrategyMartingaleStrategy(CandleSeries series)
		{
			_subscription = new(series);
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			this.WhenCandlesFinished(_subscription).Do(ProcessCandle).Apply(this);
			Subscribe(_subscription);
			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			LongSma.Process(candle);
			ShortSma.Process(candle);

			if (!IsFormedAndOnlineAndAllowTrading()) return;

			var isShortLessThenLongCurrent = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();
			var isShortLessThenLongPrevios = ShortSma.GetValue(1) < LongSma.GetValue(1);

			if (isShortLessThenLongPrevios == isShortLessThenLongCurrent) return;

			CancelActiveOrders();

			var direction = isShortLessThenLongCurrent ? Sides.Sell : Sides.Buy;

			var volume = Volume + Math.Abs(Position);

			var price = Security.ShrinkPrice(ShortSma.GetCurrentValue());
			RegisterOrder(this.CreateOrder(direction, price, volume));
		}
	}
}