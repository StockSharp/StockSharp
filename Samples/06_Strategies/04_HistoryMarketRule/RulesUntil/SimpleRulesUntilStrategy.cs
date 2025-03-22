using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using System;

namespace StockSharp.Samples.Strategies.HistoryMarketRule
{
	public class SimpleRulesUntilStrategy : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			var tickSub = new Subscription(DataType.Ticks, Security);
			var mdSub = new Subscription(DataType.MarketDepth, Security);

			var i = 0;
			mdSub.WhenOrderBookReceived(this).Do(depth =>
			{
				i++;
				LogInfo($"The rule WhenOrderBookReceived BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");
				LogInfo($"The rule WhenOrderBookReceived i={i}");
			})
			.Until(() => i >= 10)
			.Apply(this);

			// Sending requests for subscribe to market data.
			Subscribe(tickSub);
			Subscribe(mdSub);

			base.OnStarted(time);
		}
	}
}