using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Logging;
using System;

namespace StockSharp.Samples.Strategies.HistoryMarketRule
{
	public class SimpleOrderRulesStrategy : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			var sub = this.SubscribeTrades(Security);

			sub.WhenTickTradeReceived(this).Do(() =>
			{
				var order = this.BuyAtMarket(1);
				var ruleReg = order.WhenRegistered(this);
				var ruleRegFailed = order.WhenRegisterFailed(this);

				ruleReg
					.Do(() => this.AddInfoLog("Order №1 Registered"))
					.Once()
					.Apply(this)
					.Exclusive(ruleRegFailed);

				ruleRegFailed
					.Do(() => this.AddInfoLog("Order №1 RegisterFailed"))
					.Once()
					.Apply(this)
					.Exclusive(ruleReg);

				RegisterOrder(order);
			}).Once().Apply(this);

			sub.WhenTickTradeReceived(this).Do(() =>
			{
				var order = this.BuyAtMarket(10000000);
				var ruleReg = order.WhenRegistered(this);
				var ruleRegFailed = order.WhenRegisterFailed(this);

				ruleReg
					.Do(() => this.AddInfoLog("Order №2 Registered"))
					.Once()
					.Apply(this)
					.Exclusive(ruleRegFailed);

				ruleRegFailed
					.Do(() => this.AddInfoLog("Order №2 RegisterFailed"))
					.Once()
					.Apply(this)
					.Exclusive(ruleReg);

				RegisterOrder(order);
			}).Once().Apply(this);

			base.OnStarted(time);
		}
	}
}