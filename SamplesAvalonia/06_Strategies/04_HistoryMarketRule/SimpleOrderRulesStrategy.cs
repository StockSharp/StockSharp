namespace StockSharp.Samples.Strategies.HistoryMarketRule.Avalonia;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class SimpleOrderRulesStrategy : Strategy
{
	protected override void OnStarted2(DateTime time)
	{
		var subscription = new Subscription(DataType.Ticks, Security);

		RegisterOrderOnFirstTick(subscription, 1, "1");
		RegisterOrderOnFirstTick(subscription, 10_000_000, "2");
		Subscribe(subscription);

		base.OnStarted2(time);
	}

	private void RegisterOrderOnFirstTick(Subscription subscription, decimal volume, string number)
	{
		subscription.WhenTickTradeReceived(this)
			.Do(() =>
			{
				var order = CreateOrder(Sides.Buy, default, volume);
				var registered = order.WhenRegistered(this);
				var failed = order.WhenRegisterFailed(this);

				registered
					.Do(() => LogInfo($"Order №{number} Registered"))
					.Once()
					.Apply(this)
					.Exclusive(failed);

				failed
					.Do(() => LogInfo($"Order №{number} RegisterFailed"))
					.Once()
					.Apply(this)
					.Exclusive(registered);

				RegisterOrder(order);
			})
			.Once()
			.Apply(this);
	}
}
