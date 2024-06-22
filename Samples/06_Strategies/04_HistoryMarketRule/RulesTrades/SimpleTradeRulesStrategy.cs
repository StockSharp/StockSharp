using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Logging;
using System;

namespace StockSharp.Samples.Strategies.HistoryMarketRule
{
	public class SimpleTradeRulesStrategy : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			var sub = this.SubscribeTrades(Security);

			sub.WhenTickTradeReceived(this).Do(() =>
			{
				new IMarketRule[] { Security.WhenLastTradePriceMore(this, 2), Security.WhenLastTradePriceLess(this, 2) }
					.Or() // or conditions (WhenLastTradePriceMore or WhenLastTradePriceLess)
					.Do(() =>
					{
						this.AddInfoLog($"The rule WhenLastTradePriceMore Or WhenLastTradePriceLess candle={Security.LastTick}");
					})
					.Apply(this);
			})
			.Once() // call this rule only once
			.Apply(this);

			base.OnStarted(time);
		}
	}
}