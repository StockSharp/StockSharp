namespace StockSharp.Samples.Strategies.HistoryMarketRule.Avalonia;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class SimpleRulesUntilStrategy : Strategy
{
	protected override void OnStarted2(DateTime time)
	{
		var tickSubscription = new Subscription(DataType.Ticks, Security);
		var depthSubscription = new Subscription(DataType.MarketDepth, Security);
		var count = 0;

		depthSubscription.WhenOrderBookReceived(this)
			.Do(depth =>
			{
				count++;
				LogInfo($"The rule WhenOrderBookReceived BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");
				LogInfo($"The rule WhenOrderBookReceived i={count}");
			})
			.Until(() => count >= 10)
			.Apply(this);

		Subscribe(tickSubscription);
		Subscribe(depthSubscription);
		base.OnStarted2(time);
	}
}
