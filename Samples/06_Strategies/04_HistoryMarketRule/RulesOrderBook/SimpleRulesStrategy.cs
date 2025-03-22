using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using System;

namespace StockSharp.Samples.Strategies.HistoryMarketRule
{
	public class SimpleRulesStrategy : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			var tickSub = new Subscription(DataType.Ticks, Security);
			var mdSub = new Subscription(DataType.MarketDepth, Security);

			//-----------------------Create a rule. Method №1-----------------------------------
			mdSub.WhenOrderBookReceived(this).Do((depth) =>
			{
				LogInfo($"The rule WhenOrderBookReceived №1 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");
			}).Once().Apply(this);

			//-----------------------Create a rule. Method №2-----------------------------------
			var whenMarketDepthChanged = mdSub.WhenOrderBookReceived(this);

			whenMarketDepthChanged.Do((depth) =>
			{
				LogInfo($"The rule WhenOrderBookReceived №2 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");
			}).Once().Apply(this);

			//----------------------Rule inside rule-----------------------------------
			mdSub.WhenOrderBookReceived(this).Do((depth) =>
			{
				LogInfo($"The rule WhenOrderBookReceived №3 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");

				//----------------------not a Once rule-----------------------------------
				mdSub.WhenOrderBookReceived(this).Do((depth1) =>
				{
					LogInfo($"The rule WhenOrderBookReceived №4 BestBid={depth1.GetBestBid()}, BestAsk={depth1.GetBestAsk()}");
				}).Apply(this);
			}).Once().Apply(this);

			// Sending requests for subscribe to market data.
			Subscribe(tickSub);
			Subscribe(mdSub);

			base.OnStarted(time);
		}
	}
}