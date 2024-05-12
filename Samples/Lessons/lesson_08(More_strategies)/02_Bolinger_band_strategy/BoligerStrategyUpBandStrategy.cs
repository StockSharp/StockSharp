namespace bolinger_band_strategy
{
	using System;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Testing;
	using StockSharp.Messages;

	internal class BoligerStrategyUpBandStrategy : Strategy
	{
		private readonly Subscription _subscription;

		public BollingerBands BollingerBands { get; set; }
		public BoligerStrategyUpBandStrategy(CandleSeries series)
		{
			_subscription = new(series);
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			this.WhenCandlesFinished(_subscription).Do(ProcessCandle).Apply(this);
			Subscribe(_subscription);
			base.OnStarted(time);
		}

		private bool IsRealTime(ICandleMessage candle)
		{
			return (CurrentTime - candle.CloseTime).TotalSeconds < 10;
		}

		private bool IsHistoryEmulationConnector => Connector is HistoryEmulationConnector;

		private void ProcessCandle(ICandleMessage candle)
		{
			BollingerBands.Process(candle);

			if (!BollingerBands.IsFormed) return;
			if (!IsHistoryEmulationConnector && !IsRealTime(candle)) return;

			if (candle.ClosePrice >= BollingerBands.UpBand.GetCurrentValue() && Position == 0)
			{
				RegisterOrder(this.BuyAtMarket(Volume));
			}

			else if (candle.ClosePrice <= BollingerBands.MovingAverage.GetCurrentValue() && Position > 0)
			{
				RegisterOrder(this.SellAtMarket(Math.Abs(Position)));
			}
		}
	}
}