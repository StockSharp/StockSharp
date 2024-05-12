using System;
using System.Linq;
using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;

namespace TakeProfit_and_StopLoss_strategy
{
	public class StairsCountertrendAndProtectRuleStrategy : Strategy
	{
		private readonly Subscription _subscription;
		public StairsCountertrendAndProtectRuleStrategy(CandleSeries candleSeries)
		{
			_subscription = new(candleSeries);
		}

		private int _bullLength;
		private int _bearLength;
		public int Length { get; set; } = 3;
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
				var order = this.SellAtMarket(Volume + Math.Abs(Position));
				order.WhenNewTrade(this).Protect(0.1, 0.2).Until(() => order.State == OrderStates.Done).Apply(this);
				ChildStrategies.ToList().ForEach(s => s.Stop());
				RegisterOrder(order);
			}

			else
			if (_bearLength >= Length && Position <= 0)
			{
				var order = this.BuyAtMarket(Volume + Math.Abs(Position));
				order.WhenNewTrade(this).Protect(0.1, 0.2).Until(() => order.State == OrderStates.Done).Apply(this);
				ChildStrategies.ToList().ForEach(s => s.Stop());
				RegisterOrder(order);
			}
		}
	}
}