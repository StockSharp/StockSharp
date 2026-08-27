namespace StockSharp.Samples.Strategies.HistoryMarketRule.Avalonia;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class SimpleTradeRulesStrategy : Strategy
{
	protected override void OnStarted2(DateTime time)
	{
		var subscription = new Subscription(DataType.Ticks, Security);

		subscription.WhenTickTradeReceived(this)
			.Do(firstTrade =>
			{
				subscription.WhenLastTradePriceMore(this, firstTrade.Price + 2)
					.Or(subscription.WhenLastTradePriceLess(this, firstTrade.Price - 2))
					.Do(trade => LogInfo($"The rule WhenLastTradePriceMore Or WhenLastTradePriceLess tick={trade}"))
					.Apply(this);
			})
			.Once()
			.Apply(this);

		Subscribe(subscription);
		base.OnStarted2(time);
	}
}
