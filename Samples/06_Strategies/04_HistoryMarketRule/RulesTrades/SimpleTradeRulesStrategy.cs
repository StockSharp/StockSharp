using StockSharp.Algo;
using StockSharp.Algo.Strategies;

using Ecng.Logging;

using System;

namespace StockSharp.Samples.Strategies.HistoryMarketRule
{
	public class SimpleTradeRulesStrategy : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			var sub = this.SubscribeTrades(Security);

			sub.WhenTickTradeReceived(this).Do(t =>
			{
				sub
					.WhenLastTradePriceMore(this, t.Price + 2)
					.Or(sub.WhenLastTradePriceLess(this, t.Price - 2))
					.Do(t =>
					{
						this.AddInfoLog($"The rule WhenLastTradePriceMore Or WhenLastTradePriceLess tick={t}");
					})
					.Apply(this);
			})
			.Once() // call this rule only once
			.Apply(this);

			base.OnStarted(time);
		}
	}
}