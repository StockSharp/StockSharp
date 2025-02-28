namespace StockSharp.Samples.Strategies.HistoryBollingerBands
{
	using System;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Testing;
	using StockSharp.Messages;
	using StockSharp.BusinessEntities;

	internal class BollingerStrategyClassicStrategy : Strategy
	{
		private readonly Subscription _subscription;

		public BollingerBands BollingerBands { get; set; }
		public BollingerStrategyClassicStrategy(CandleSeries series)
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
			BollingerBands.Process(candle);

			if (!IsFormedAndOnlineAndAllowTrading()) return;

			if (candle.ClosePrice >= BollingerBands.UpBand.GetCurrentValue() && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}

			else
			if (candle.ClosePrice <= BollingerBands.LowBand.GetCurrentValue() && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
	}
}