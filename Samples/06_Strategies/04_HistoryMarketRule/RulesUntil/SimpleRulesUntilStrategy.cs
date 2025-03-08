using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

using System;

namespace StockSharp.Samples.Strategies.HistoryMarketRule
{
	public class SimpleRulesUntilStrategy : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			var tickSub = this.SubscribeTrades(Security);

			var mdSub = this.SubscribeMarketDepth(Security);

			var i = 0;
			mdSub.WhenOrderBookReceived(this).Do(depth =>
			{
				i++;
				LogInfo($"The rule WhenOrderBookReceived BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");
				LogInfo($"The rule WhenOrderBookReceived i={i}");
			})
			.Until(() => i >= 10)
			.Apply(this);

			base.OnStarted(time);
		}
	}
}