using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using System;

namespace StockSharp.Samples.Strategies.HistoryMarketRule
{
	public class SimpleTradeRulesStrategy : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			var sub = new Subscription(DataType.Ticks, Security);

			sub.WhenTickTradeReceived(this).Do(t =>
			{
				sub
					.WhenLastTradePriceMore(this, t.Price + 2)
					.Or(sub.WhenLastTradePriceLess(this, t.Price - 2))
					.Do(t =>
					{
						LogInfo($"The rule WhenLastTradePriceMore Or WhenLastTradePriceLess tick={t}");
					})
					.Apply(this);
			})
			.Once() // call this rule only once
			.Apply(this);

			// Sending request for subscribe to market data.
			Subscribe(sub);

			base.OnStarted(time);
		}
	}
}