using System;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies.HistoryTrend
{
	public class OneCandleCountertrendStrategy : Strategy
	{
		public DataType CandleDataType { get; set; }

		protected override void OnStarted(DateTimeOffset time)
		{
			var subscription = new Subscription(CandleDataType, Security);

			this
				.WhenCandlesFinished(subscription)
				.Do(OnCandleReceived)
				.Apply(this);

			Subscribe(subscription);

			base.OnStarted(time);
		}

		private void OnCandleReceived(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished) return;

			if (candle.OpenPrice < candle.ClosePrice && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}

			else
			if (candle.OpenPrice > candle.ClosePrice && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
	}
}