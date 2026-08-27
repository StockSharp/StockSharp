namespace StockSharp.Samples.Strategies.HistoryMarketRule.Avalonia;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class SimpleRulesStrategy : Strategy
{
	protected override void OnStarted2(DateTime time)
	{
		var tickSubscription = new Subscription(DataType.Ticks, Security);
		var depthSubscription = new Subscription(DataType.MarketDepth, Security);

		depthSubscription.WhenOrderBookReceived(this)
			.Do(depth => LogInfo($"The rule WhenOrderBookReceived №1 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}"))
			.Once()
			.Apply(this);

		var secondRule = depthSubscription.WhenOrderBookReceived(this);
		secondRule
			.Do(depth => LogInfo($"The rule WhenOrderBookReceived №2 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}"))
			.Once()
			.Apply(this);

		depthSubscription.WhenOrderBookReceived(this)
			.Do(depth =>
			{
				LogInfo($"The rule WhenOrderBookReceived №3 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");
				depthSubscription.WhenOrderBookReceived(this)
					.Do(nextDepth => LogInfo($"The rule WhenOrderBookReceived №4 BestBid={nextDepth.GetBestBid()}, BestAsk={nextDepth.GetBestAsk()}"))
					.Apply(this);
			})
			.Once()
			.Apply(this);

		Subscribe(tickSubscription);
		Subscribe(depthSubscription);
		base.OnStarted2(time);
	}
}
