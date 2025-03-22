using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using System;

namespace StockSharp.Samples.Strategies.HistoryMarketRule
{
	public class SimpleOrderRulesStrategy : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			var sub = new Subscription(DataType.Ticks, Security);

			sub.WhenTickTradeReceived(this).Do(() =>
			{
				var order = CreateOrder(Sides.Buy, default, 1);

				var ruleReg = order.WhenRegistered(this);
				var ruleRegFailed = order.WhenRegisterFailed(this);

				ruleReg
					.Do(() => LogInfo("Order №1 Registered"))
					.Once()
					.Apply(this)
					.Exclusive(ruleRegFailed);

				ruleRegFailed
					.Do(() => LogInfo("Order №1 RegisterFailed"))
					.Once()
					.Apply(this)
					.Exclusive(ruleReg);

				RegisterOrder(order);
			}).Once().Apply(this);

			sub.WhenTickTradeReceived(this).Do(() =>
			{
				var order = CreateOrder(Sides.Buy, default, 10000000);

				var ruleReg = order.WhenRegistered(this);
				var ruleRegFailed = order.WhenRegisterFailed(this);

				ruleReg
					.Do(() => LogInfo("Order №2 Registered"))
					.Once()
					.Apply(this)
					.Exclusive(ruleRegFailed);

				ruleRegFailed
					.Do(() => LogInfo("Order №2 RegisterFailed"))
					.Once()
					.Apply(this)
					.Exclusive(ruleReg);

				RegisterOrder(order);
			}).Once().Apply(this);

			// Sending request for subscribe to market data.
			Subscribe(sub);

			base.OnStarted(time);
		}
	}
}